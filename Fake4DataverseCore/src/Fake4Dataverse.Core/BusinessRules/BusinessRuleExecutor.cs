using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;

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
    /// </summary>
    public class BusinessRuleExecutor
    {
        private readonly List<BusinessRuleDefinition> _rules;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleExecutor"/> class.
        /// </summary>
        public BusinessRuleExecutor()
        {
            _rules = new List<BusinessRuleDefinition>();
        }
        
        /// <summary>
        /// Registers a business rule for execution.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#activate-or-deactivate
        /// "Business rules must be activated before they can be executed"
        /// 
        /// The rule will be validated before being added to ensure it's properly configured.
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
            
            _rules.Add(rule);
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
            _rules.Clear();
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
            
            return _rules
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
            
            // Find applicable rules for this entity
            var applicableRules = _rules
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
    }
}
