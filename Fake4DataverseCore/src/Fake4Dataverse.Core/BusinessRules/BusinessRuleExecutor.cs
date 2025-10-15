using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fake4Dataverse.Abstractions;

namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Executes business rules against entities and produces execution results.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
    /// "Business rules are evaluated when records are created, updated, or when specific field values change.
    /// The rule engine evaluates all conditions and executes appropriate actions."
    /// 
    /// This class is the core execution engine for business rules in Fake4Dataverse.
    /// It evaluates rule conditions and executes actions when conditions are met.
    /// 
    /// Uses a hybrid storage approach:
    /// - In-memory cache for fast access and backwards compatibility
    /// - Workflow table persistence (when available) to mirror real Dataverse behavior
    /// </summary>
    public class BusinessRuleExecutor
    {
        private readonly IXrmFakedContext _context;
        private readonly Dictionary<string, BusinessRuleDefinition> _rulesCache;  // In-memory cache for backwards compatibility
        
        // Workflow table constants
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/workflow
        // The workflow entity (also known as process) stores process definitions including business rules
        private const string WorkflowEntityName = "workflow";
        private const int CategoryBusinessRule = 2; // Business Rule category
        private const int StateCodeActivated = 1;
        private const int TypeDefinition = 1;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleExecutor"/> class.
        /// </summary>
        [Obsolete("Use constructor with IXrmFakedContext parameter for table-based storage support")]
        public BusinessRuleExecutor()
        {
            _context = null;
            _rulesCache = new Dictionary<string, BusinessRuleDefinition>(StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleExecutor"/> class with context.
        /// This enables hybrid storage with workflow table persistence.
        /// </summary>
        /// <param name="context">The XrmFakedContext for table operations</param>
        public BusinessRuleExecutor(IXrmFakedContext context)
        {
            _context = context;
            _rulesCache = new Dictionary<string, BusinessRuleDefinition>(StringComparer.OrdinalIgnoreCase);
            
            // Load any existing business rules from workflow table into cache
            // This allows rules to be pre-populated via CRUD operations
            LoadRulesFromWorkflowTable();
        }
        
        /// <summary>
        /// Registers a business rule for execution.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#activate-or-deactivate
        /// "Business rules must be activated before they can be executed"
        /// 
        /// The rule will be validated before being added to ensure it's properly configured.
        /// 
        /// This method attempts to store the rule in the workflow table (process table),
        /// mirroring how real Dataverse stores business rules. Rules are also cached in memory for
        /// backwards compatibility and when the workflow entity is not available.
        /// </summary>
        /// <param name="rule">The business rule to register</param>
        /// <exception cref="ArgumentNullException">Thrown if rule is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if rule is not properly configured</exception>
        public void RegisterRule(BusinessRuleDefinition rule)
        {
            if (rule == null)
            {
                throw new ArgumentNullException(nameof(rule));
            }
            
            // Validate the rule before adding it
            rule.Validate();
            
            // Store in memory cache (always works, provides backwards compatibility)
            var key = $"{rule.EntityLogicalName}_{rule.Name}";
            _rulesCache[key] = rule;
            
            // Try to store rule in workflow table (this mirrors real Dataverse behavior)
            // But handle gracefully if the entity doesn't exist
            if (_context != null)
            {
                try
                {
                    StoreRuleInWorkflowTable(rule);
                }
                catch (Exception)
                {
                    // If workflow table is not available or not properly initialized,
                    // rule will still work from the in-memory cache
                }
            }
        }
        
        /// <summary>
        /// Registers multiple business rules at once.
        /// </summary>
        /// <param name="rules">The business rules to register</param>
        public void RegisterRules(IEnumerable<BusinessRuleDefinition> rules)
        {
            if (rules == null)
            {
                throw new ArgumentNullException(nameof(rules));
            }
            
            foreach (var rule in rules)
            {
                RegisterRule(rule);
            }
        }
        
        /// <summary>
        /// Clears all registered business rules.
        /// Useful for testing or resetting the executor.
        /// </summary>
        public void ClearRules()
        {
            // Clear cache
            _rulesCache.Clear();
            
            // Try to delete all business rules from workflow table
            if (_context != null)
            {
                try
                {
                    var service = _context.GetOrganizationService();
                    var query = new QueryExpression(WorkflowEntityName)
                    {
                        ColumnSet = new ColumnSet("workflowid"),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("category", ConditionOperator.Equal, CategoryBusinessRule)
                            }
                        }
                    };

                    var rules = service.RetrieveMultiple(query).Entities;
                    foreach (var rule in rules)
                    {
                        service.Delete(WorkflowEntityName, rule.Id);
                    }
                }
                catch (Exception)
                {
                    // If workflow table is not available, silently continue
                }
            }
        }
        
        /// <summary>
        /// Gets all registered rules for a specific entity.
        /// </summary>
        /// <param name="entityLogicalName">The entity logical name</param>
        /// <returns>List of rules for the entity</returns>
        public IReadOnlyList<BusinessRuleDefinition> GetRulesForEntity(string entityLogicalName)
        {
            if (string.IsNullOrEmpty(entityLogicalName))
            {
                return new List<BusinessRuleDefinition>().AsReadOnly();
            }
            
            return _rulesCache.Values
                .Where(r => r.EntityLogicalName.Equals(entityLogicalName, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }
        
        /// <summary>
        /// Executes all applicable business rules for an entity.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#execution-order
        /// "Business rules are executed in the order they are defined. If multiple rules affect the same field,
        /// the last rule to execute takes precedence."
        /// 
        /// This method finds all active rules for the entity, evaluates their conditions,
        /// and executes actions for rules where conditions are met.
        /// </summary>
        /// <param name="entity">The entity to execute rules against</param>
        /// <param name="trigger">The trigger that initiated this execution (Create, Update, etc.)</param>
        /// <param name="isServerSide">Whether this is server-side execution (true) or client-side (false)</param>
        /// <returns>Combined execution results from all rules</returns>
        /// <exception cref="ArgumentNullException">Thrown if entity is null</exception>
        public BusinessRuleExecutionResult ExecuteRules(Entity entity, BusinessRuleTrigger trigger, bool isServerSide = true)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            var result = new BusinessRuleExecutionResult();
            
            // Find applicable rules for this entity from cache
            var applicableRules = _rulesCache.Values
                .Where(r => r.EntityLogicalName.Equals(entity.LogicalName, StringComparison.OrdinalIgnoreCase))
                .Where(r => r.ShouldExecuteForTrigger(trigger))
                .Where(r => r.ShouldExecuteForContext(isServerSide))
                .ToList();
            
            // Execute each rule
            foreach (var rule in applicableRules)
            {
                ExecuteRule(rule, entity, result);
            }
            
            return result;
        }
        
        /// <summary>
        /// Executes a single business rule against an entity.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#rule-evaluation
        /// "The rule engine evaluates all conditions. If all conditions are true (AND logic) or any condition is true (OR logic),
        /// the actions are executed. Otherwise, the else actions are executed."
        /// 
        /// This internal method handles the logic of evaluating conditions and executing actions.
        /// </summary>
        /// <param name="rule">The rule to execute</param>
        /// <param name="entity">The entity to execute against</param>
        /// <param name="result">The result object to populate</param>
        private void ExecuteRule(BusinessRuleDefinition rule, Entity entity, BusinessRuleExecutionResult result)
        {
            try
            {
                // Evaluate all conditions
                bool conditionsMet = EvaluateConditions(rule, entity);
                
                // Execute appropriate actions based on condition results
                var actionsToExecute = conditionsMet ? rule.Actions : rule.ElseActions;
                
                foreach (var action in actionsToExecute)
                {
                    action.Execute(entity, result);
                }
            }
            catch (Exception ex)
            {
                // If rule execution fails, add it as an error
                result.AddError(null, $"Error executing business rule '{rule.Name}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Evaluates all conditions for a rule.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#condition-logic
        /// "Use AND logic when all conditions must be true. Use OR logic when any condition being true is sufficient."
        /// 
        /// By default, uses AND logic (all conditions must be true).
        /// If UseAndLogic is false, uses OR logic (any condition being true is sufficient).
        /// If there are no conditions, returns true (unconditional rule).
        /// </summary>
        /// <param name="rule">The rule to evaluate conditions for</param>
        /// <param name="entity">The entity to evaluate against</param>
        /// <returns>True if conditions are met, false otherwise</returns>
        private bool EvaluateConditions(BusinessRuleDefinition rule, Entity entity)
        {
            // No conditions means unconditional rule - always execute
            if (!rule.Conditions.Any())
            {
                return true;
            }
            
            if (rule.UseAndLogic)
            {
                // AND logic - all conditions must be true
                return rule.Conditions.All(c => c.Evaluate(entity));
            }
            else
            {
                // OR logic - at least one condition must be true
                return rule.Conditions.Any(c => c.Evaluate(entity));
            }
        }
        
        /// <summary>
        /// Internal method to store rule in workflow table
        /// </summary>
        private void StoreRuleInWorkflowTable(BusinessRuleDefinition rule)
        {
            if (_context == null)
                return;
                
            // Store rule in workflow table
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/workflow
            // Workflow entity stores process definitions including business rules (category=2)
            var service = _context.GetOrganizationService();
            
            var key = $"{rule.EntityLogicalName}_{rule.Name}";
            
            // Check if rule already exists by uniquename
            var query = new QueryExpression(WorkflowEntityName)
            {
                ColumnSet = new ColumnSet("workflowid"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("uniquename", ConditionOperator.Equal, key),
                        new ConditionExpression("category", ConditionOperator.Equal, CategoryBusinessRule)
                    }
                }
            };

            var existing = service.RetrieveMultiple(query).Entities.FirstOrDefault();
            
            var workflowEntity = new Entity(WorkflowEntityName);
            if (existing != null)
            {
                workflowEntity.Id = existing.Id;
            }
            else
            {
                workflowEntity.Id = Guid.NewGuid();
            }

            // Set workflow attributes
            workflowEntity["name"] = rule.Name;
            workflowEntity["uniquename"] = key;
            workflowEntity["category"] = CategoryBusinessRule; // Business Rule
            workflowEntity["type"] = TypeDefinition; // Definition (not activation/instance)
            workflowEntity["primaryentity"] = rule.EntityLogicalName;
            workflowEntity["description"] = rule.Description;
            
            // Set trigger flags based on trigger type
            workflowEntity["triggeroncreate"] = (rule.Trigger & BusinessRuleTrigger.OnCreate) == BusinessRuleTrigger.OnCreate;
            workflowEntity["triggeronupdate"] = (rule.Trigger & BusinessRuleTrigger.OnUpdate) == BusinessRuleTrigger.OnUpdate;
            
            // Set scope
            workflowEntity["scope"] = (int)rule.Scope;

            // Serialize rule definition to clientdata field
            // Note: We store a simplified representation
            var ruleData = new
            {
                rule.Name,
                rule.Description,
                rule.EntityLogicalName,
                rule.Scope,
                rule.Trigger,
                rule.IsActive,
                rule.UseAndLogic,
                Conditions = rule.Conditions.Select(c => new
                {
                    c.FieldName,
                    Operator = c.Operator.ToString(),
                    c.Value
                }).ToList(),
                Actions = rule.Actions.Select(a => new
                {
                    ActionType = a.ActionType.ToString(),
                    a.FieldName,
                    a.Value,
                    a.Message
                }).ToList(),
                ElseActions = rule.ElseActions.Select(a => new
                {
                    ActionType = a.ActionType.ToString(),
                    a.FieldName,
                    a.Value,
                    a.Message
                }).ToList()
            };
            workflowEntity["clientdata"] = JsonSerializer.Serialize(ruleData);

            if (existing != null)
            {
                service.Update(workflowEntity);
            }
            else
            {
                service.Create(workflowEntity);
            }

            // Set state after creation (statecode cannot be set during Create in Fake4Dataverse)
            var stateEntity = new Entity(WorkflowEntityName)
            {
                Id = workflowEntity.Id,
                ["statecode"] = rule.IsActive ? StateCodeActivated : 0,
                ["statuscode"] = rule.IsActive ? 2 : 1
            };
            service.Update(stateEntity);
        }
        
        /// <summary>
        /// Loads business rules from the workflow table during initialization.
        /// This allows rules to be pre-populated via CRUD operations on the workflow table.
        /// Loaded rules are added to the in-memory cache.
        /// </summary>
        private void LoadRulesFromWorkflowTable()
        {
            if (_context == null)
                return;
                
            try
            {
                var service = _context.GetOrganizationService();
                var query = new QueryExpression(WorkflowEntityName)
                {
                    ColumnSet = new ColumnSet(true),
                    Criteria = new FilterExpression
                    {
                        Conditions =
                        {
                            new ConditionExpression("category", ConditionOperator.Equal, CategoryBusinessRule)
                        }
                    }
                };

                var workflowEntities = service.RetrieveMultiple(query).Entities;
                
                // Load each rule into cache
                foreach (var workflowEntity in workflowEntities)
                {
                    var rule = DeserializeRuleFromWorkflowEntity(workflowEntity);
                    if (rule != null && !string.IsNullOrEmpty(rule.Name) && !string.IsNullOrEmpty(rule.EntityLogicalName))
                    {
                        var key = $"{rule.EntityLogicalName}_{rule.Name}";
                        _rulesCache[key] = rule;
                    }
                }
            }
            catch (Exception)
            {
                // If workflow table is not available, that's okay - rules can still be registered manually
            }
        }
        
        /// <summary>
        /// Deserializes a business rule from a workflow entity
        /// </summary>
        private BusinessRuleDefinition DeserializeRuleFromWorkflowEntity(Entity workflowEntity)
        {
            try
            {
                var clientData = workflowEntity.GetAttributeValue<string>("clientdata");
                if (string.IsNullOrWhiteSpace(clientData))
                {
                    // If no clientdata, create a minimal rule from workflow attributes
                    return CreateMinimalRuleFromWorkflowEntity(workflowEntity);
                }

                // Deserialize from JSON
                using (var doc = JsonDocument.Parse(clientData))
                {
                    var root = doc.RootElement;
                    
                    var rule = new BusinessRuleDefinition
                    {
                        Name = root.TryGetProperty("Name", out var name) ? name.GetString() : null,
                        Description = root.TryGetProperty("Description", out var desc) ? desc.GetString() : null,
                        EntityLogicalName = root.TryGetProperty("EntityLogicalName", out var entity) ? entity.GetString() : null,
                        IsActive = root.TryGetProperty("IsActive", out var active) && active.GetBoolean(),
                        UseAndLogic = !root.TryGetProperty("UseAndLogic", out var useAnd) || useAnd.GetBoolean()
                    };

                    // Deserialize scope
                    if (root.TryGetProperty("Scope", out var scope) && scope.ValueKind == JsonValueKind.Number)
                    {
                        rule.Scope = (BusinessRuleScope)scope.GetInt32();
                    }
                    
                    // Deserialize trigger
                    if (root.TryGetProperty("Trigger", out var trigger) && trigger.ValueKind == JsonValueKind.Number)
                    {
                        rule.Trigger = (BusinessRuleTrigger)trigger.GetInt32();
                    }

                    // Deserialize conditions
                    if (root.TryGetProperty("Conditions", out var conditions) && conditions.ValueKind == JsonValueKind.Array)
                    {
                        rule.Conditions = new List<BusinessRuleCondition>();
                        foreach (var condElement in conditions.EnumerateArray())
                        {
                            var condition = new BusinessRuleCondition
                            {
                                FieldName = condElement.TryGetProperty("FieldName", out var field) ? field.GetString() : null,
                                Value = condElement.TryGetProperty("Value", out var val) ? GetJsonValue(val) : null
                            };
                            
                            if (condElement.TryGetProperty("Operator", out var op) && op.ValueKind == JsonValueKind.String)
                            {
                                if (Enum.TryParse<ConditionOperator>(op.GetString(), out var opValue))
                                {
                                    condition.Operator = opValue;
                                }
                            }
                            
                            rule.Conditions.Add(condition);
                        }
                    }

                    // Deserialize actions
                    if (root.TryGetProperty("Actions", out var actions) && actions.ValueKind == JsonValueKind.Array)
                    {
                        rule.Actions = new List<BusinessRuleAction>();
                        foreach (var actionElement in actions.EnumerateArray())
                        {
                            var action = new BusinessRuleAction
                            {
                                FieldName = actionElement.TryGetProperty("FieldName", out var field) ? field.GetString() : null,
                                Value = actionElement.TryGetProperty("Value", out var val) ? GetJsonValue(val) : null,
                                Message = actionElement.TryGetProperty("Message", out var msg) ? msg.GetString() : null
                            };
                            
                            if (actionElement.TryGetProperty("ActionType", out var type) && type.ValueKind == JsonValueKind.String)
                            {
                                if (Enum.TryParse<BusinessRuleActionType>(type.GetString(), out var typeValue))
                                {
                                    action.ActionType = typeValue;
                                }
                            }
                            
                            rule.Actions.Add(action);
                        }
                    }

                    // Deserialize else actions
                    if (root.TryGetProperty("ElseActions", out var elseActions) && elseActions.ValueKind == JsonValueKind.Array)
                    {
                        rule.ElseActions = new List<BusinessRuleAction>();
                        foreach (var actionElement in elseActions.EnumerateArray())
                        {
                            var action = new BusinessRuleAction
                            {
                                FieldName = actionElement.TryGetProperty("FieldName", out var field) ? field.GetString() : null,
                                Value = actionElement.TryGetProperty("Value", out var val) ? GetJsonValue(val) : null,
                                Message = actionElement.TryGetProperty("Message", out var msg) ? msg.GetString() : null
                            };
                            
                            if (actionElement.TryGetProperty("ActionType", out var type) && type.ValueKind == JsonValueKind.String)
                            {
                                if (Enum.TryParse<BusinessRuleActionType>(type.GetString(), out var typeValue))
                                {
                                    action.ActionType = typeValue;
                                }
                            }
                            
                            rule.ElseActions.Add(action);
                        }
                    }

                    return rule;
                }
            }
            catch
            {
                // If deserialization fails, return null
                return null;
            }
        }
        
        /// <summary>
        /// Creates a minimal business rule when clientdata is not available
        /// </summary>
        private BusinessRuleDefinition CreateMinimalRuleFromWorkflowEntity(Entity workflowEntity)
        {
            var rule = new BusinessRuleDefinition
            {
                Name = workflowEntity.GetAttributeValue<string>("name"),
                Description = workflowEntity.GetAttributeValue<string>("description"),
                EntityLogicalName = workflowEntity.GetAttributeValue<string>("primaryentity"),
                IsActive = workflowEntity.GetAttributeValue<int>("statecode") == StateCodeActivated
            };

            // Set trigger based on flags
            var trigger = (BusinessRuleTrigger)0;
            if (workflowEntity.GetAttributeValue<bool>("triggeroncreate"))
                trigger |= BusinessRuleTrigger.OnCreate;
            if (workflowEntity.GetAttributeValue<bool>("triggeronupdate"))
                trigger |= BusinessRuleTrigger.OnUpdate;
            rule.Trigger = trigger;

            // Set scope
            if (workflowEntity.Contains("scope"))
            {
                rule.Scope = (BusinessRuleScope)workflowEntity.GetAttributeValue<int>("scope");
            }

            rule.Conditions = new List<BusinessRuleCondition>();
            rule.Actions = new List<BusinessRuleAction>();
            rule.ElseActions = new List<BusinessRuleAction>();

            return rule;
        }
        
        /// <summary>
        /// Extracts a value from a JsonElement
        /// </summary>
        private object GetJsonValue(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString();
                case JsonValueKind.Number:
                    if (element.TryGetInt32(out var intValue))
                        return intValue;
                    if (element.TryGetInt64(out var longValue))
                        return longValue;
                    return element.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return null;
            }
        }
    }
}
