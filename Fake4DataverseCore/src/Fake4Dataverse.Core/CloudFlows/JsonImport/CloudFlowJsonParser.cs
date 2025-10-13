using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;

namespace Fake4Dataverse.CloudFlows.JsonImport
{
    /// <summary>
    /// Parses exported Cloud Flow JSON definitions and converts them to ICloudFlowDefinition objects.
    /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
    /// 
    /// This parser handles the standard JSON format exported from Power Automate, which follows
    /// the Logic Apps workflow definition language schema.
    /// </summary>
    internal class CloudFlowJsonParser
    {
        /// <summary>
        /// Parses a Cloud Flow JSON string and returns a CloudFlowDefinition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language-schema-reference
        /// 
        /// The JSON format includes:
        /// - Flow metadata (name, displayName, state)
        /// - Workflow definition with schema version
        /// - Triggers dictionary (typically one trigger)
        /// - Actions dictionary (can have multiple actions with dependencies)
        /// </summary>
        public ICloudFlowDefinition Parse(string flowJson)
        {
            if (string.IsNullOrWhiteSpace(flowJson))
                throw new ArgumentException("Flow JSON cannot be null or empty", nameof(flowJson));

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true
                };

                var root = JsonSerializer.Deserialize<CloudFlowJsonRoot>(flowJson, options);

                if (root == null)
                    throw new InvalidOperationException("Failed to deserialize flow JSON");

                return MapToFlowDefinition(root);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid Cloud Flow JSON format: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maps the parsed JSON root to a CloudFlowDefinition.
        /// </summary>
        private ICloudFlowDefinition MapToFlowDefinition(CloudFlowJsonRoot root)
        {
            if (root.Properties?.Definition == null)
                throw new InvalidOperationException("Flow JSON must contain properties.definition");

            var definition = new CloudFlowDefinition
            {
                Name = root.Name,
                DisplayName = root.Properties.DisplayName ?? root.Name,
                IsEnabled = root.Properties.State?.Equals("Started", StringComparison.OrdinalIgnoreCase) ?? true,
                Metadata = new Dictionary<string, object>
                {
                    ["schema"] = root.Properties.Definition.Schema,
                    ["contentVersion"] = root.Properties.Definition.ContentVersion
                }
            };

            // Parse trigger (typically only one)
            definition.Trigger = ParseTrigger(root.Properties.Definition.Triggers);

            // Parse actions
            definition.Actions = ParseActions(root.Properties.Definition.Actions);

            return definition;
        }

        /// <summary>
        /// Parses the triggers dictionary and returns the first trigger.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
        /// 
        /// In most flows, there is only one trigger. If multiple triggers exist,
        /// we take the first one (this is consistent with how Power Automate works).
        /// </summary>
        private IFlowTrigger ParseTrigger(Dictionary<string, TriggerDefinition> triggers)
        {
            if (triggers == null || !triggers.Any())
                throw new InvalidOperationException("Flow must have at least one trigger");

            var triggerEntry = triggers.First();
            var triggerName = triggerEntry.Key;
            var triggerDef = triggerEntry.Value;

            // Check if this is a Dataverse trigger
            // Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
            if (IsDataverseTrigger(triggerDef))
            {
                return ParseDataverseTrigger(triggerName, triggerDef);
            }

            // For unsupported trigger types, throw an exception with guidance
            throw new NotSupportedException(
                $"Trigger type '{triggerDef.Type}' is not yet supported. " +
                "Currently supported: Dataverse triggers (OpenApiConnectionWebhook with commondataserviceforapps). " +
                "Please use RegisterFlow() with a programmatic flow definition for unsupported trigger types.");
        }

        /// <summary>
        /// Checks if the trigger is a Dataverse trigger.
        /// Dataverse triggers use type "OpenApiConnectionWebhook" with connectionName containing "commondataserviceforapps".
        /// </summary>
        private bool IsDataverseTrigger(TriggerDefinition triggerDef)
        {
            if (triggerDef.Type != "OpenApiConnectionWebhook")
                return false;

            var connectionName = triggerDef.Inputs?.Host?.ConnectionName;
            return connectionName?.IndexOf("commondataserviceforapps", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Parses a Dataverse trigger from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
        /// 
        /// Dataverse triggers contain:
        /// - subscriptionRequest/message: 1=Create, 2=Update, 3=Delete, 4=CreateOrUpdate
        /// - subscriptionRequest/entityname: The entity logical name
        /// - subscriptionRequest/scope: 1=Organization, 2=BusinessUnit, 3=ParentChildBusinessUnits, 4=User
        /// - subscriptionRequest/filteringattributes: Comma-separated list of attributes (for Update triggers)
        /// </summary>
        private DataverseTrigger ParseDataverseTrigger(string triggerName, TriggerDefinition triggerDef)
        {
            var parameters = triggerDef.Inputs?.Parameters;
            if (parameters == null)
                throw new InvalidOperationException("Dataverse trigger must have parameters");

            var trigger = new DataverseTrigger
            {
                Name = triggerName
            };

            // Parse message type
            if (parameters.TryGetValue("subscriptionRequest/message", out var messageObj))
            {
                var messageCode = GetIntValue(messageObj);
                trigger.Message = MapMessageCode(messageCode);
            }

            // Parse entity name
            if (parameters.TryGetValue("subscriptionRequest/entityname", out var entityNameObj))
            {
                trigger.EntityLogicalName = GetStringValue(entityNameObj);
            }

            // Parse scope
            if (parameters.TryGetValue("subscriptionRequest/scope", out var scopeObj))
            {
                var scopeCode = GetIntValue(scopeObj);
                trigger.Scope = (TriggerScope)scopeCode;
            }

            // Parse filtered attributes (for Update triggers)
            if (parameters.TryGetValue("subscriptionRequest/filteringattributes", out var filteringAttrObj))
            {
                var filteringAttrString = GetStringValue(filteringAttrObj);
                if (!string.IsNullOrWhiteSpace(filteringAttrString))
                {
                    trigger.FilteredAttributes = filteringAttrString
                        .Split(',')
                        .Select(a => a.Trim())
                        .Where(a => !string.IsNullOrWhiteSpace(a))
                        .ToList();
                }
            }

            return trigger;
        }

        /// <summary>
        /// Maps the message code to the message name.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
        /// 
        /// Message codes:
        /// - 1: Create
        /// - 2: Update
        /// - 3: Delete
        /// - 4: CreateOrUpdate
        /// </summary>
        private string MapMessageCode(int messageCode)
        {
            switch (messageCode)
            {
                case 1:
                    return "Create";
                case 2:
                    return "Update";
                case 3:
                    return "Delete";
                case 4:
                    return "CreateOrUpdate";
                default:
                    throw new InvalidOperationException($"Unknown message code: {messageCode}");
            }
        }

        /// <summary>
        /// Parses the actions dictionary and returns a list of flow actions.
        /// Actions are sorted by their dependencies (runAfter) to ensure correct execution order.
        /// </summary>
        private IList<IFlowAction> ParseActions(Dictionary<string, ActionDefinition> actions)
        {
            if (actions == null || !actions.Any())
                return new List<IFlowAction>();

            var actionList = new List<IFlowAction>();

            // Sort actions by dependencies (runAfter)
            // Actions with no dependencies run first, then actions that depend on them, etc.
            var sortedActions = TopologicalSort(actions);

            foreach (var actionEntry in sortedActions)
            {
                var actionName = actionEntry.Key;
                var actionDef = actionEntry.Value;

                try
                {
                    var action = ParseAction(actionName, actionDef);
                    if (action != null)
                    {
                        actionList.Add(action);
                    }
                }
                catch (NotSupportedException ex)
                {
                    // Log warning but continue with other actions
                    // In a real implementation, you might want to collect these warnings and return them
                    Console.WriteLine($"Warning: Skipping unsupported action '{actionName}': {ex.Message}");
                }
            }

            return actionList;
        }

        /// <summary>
        /// Topological sort of actions based on their dependencies (runAfter).
        /// This ensures actions execute in the correct order.
        /// </summary>
        private List<KeyValuePair<string, ActionDefinition>> TopologicalSort(Dictionary<string, ActionDefinition> actions)
        {
            var sorted = new List<KeyValuePair<string, ActionDefinition>>();
            var visited = new HashSet<string>();

            void Visit(string actionName)
            {
                if (visited.Contains(actionName))
                    return;

                visited.Add(actionName);

                if (actions.TryGetValue(actionName, out var actionDef))
                {
                    // Visit dependencies first
                    if (actionDef.RunAfter != null)
                    {
                        foreach (var dependency in actionDef.RunAfter.Keys)
                        {
                            Visit(dependency);
                        }
                    }

                    sorted.Add(new KeyValuePair<string, ActionDefinition>(actionName, actionDef));
                }
            }

            foreach (var actionName in actions.Keys)
            {
                Visit(actionName);
            }

            return sorted;
        }

        /// <summary>
        /// Parses a single action from the JSON definition.
        /// Supports Dataverse actions, control flow actions (If, Switch, Foreach, Until), and data operations (Compose).
        /// </summary>
        private IFlowAction ParseAction(string actionName, ActionDefinition actionDef)
        {
            // Check action type and parse accordingly
            switch (actionDef.Type?.ToLowerInvariant())
            {
                case "if":
                    return ParseConditionAction(actionName, actionDef);
                
                case "switch":
                    return ParseSwitchAction(actionName, actionDef);
                
                case "foreach":
                    return ParseApplyToEachAction(actionName, actionDef);
                
                case "until":
                    return ParseDoUntilAction(actionName, actionDef);
                
                case "compose":
                    return ParseComposeAction(actionName, actionDef);
                
                case "openapiconnection":
                    // Check if this is a Dataverse action
                    if (IsDataverseAction(actionDef))
                    {
                        return ParseDataverseAction(actionName, actionDef);
                    }
                    break;
            }

            // For unsupported action types, throw an exception
            throw new NotSupportedException(
                $"Action type '{actionDef.Type}' is not yet supported. " +
                "Currently supported: If, Switch, Foreach, Until, Compose, and Dataverse actions (OpenApiConnection with commondataserviceforapps). " +
                "Use RegisterConnectorActionHandler() to handle custom connector actions in tests.");
        }

        /// <summary>
        /// Checks if the action is a Dataverse action.
        /// Dataverse actions use type "OpenApiConnection" with connectionName containing "commondataserviceforapps".
        /// </summary>
        private bool IsDataverseAction(ActionDefinition actionDef)
        {
            if (actionDef.Type != "OpenApiConnection")
                return false;

            // Get ActionInputs from object
            ActionInputs inputs = null;
            if (actionDef.Inputs is JsonElement jsonElement)
            {
                try
                {
                    inputs = JsonSerializer.Deserialize<ActionInputs>(jsonElement.GetRawText());
                }
                catch
                {
                    return false;
                }
            }

            var connectionName = inputs?.Host?.ConnectionName;
            return connectionName?.IndexOf("commondataserviceforapps", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>
        /// Parses a Dataverse action from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
        /// 
        /// Supported operations:
        /// - CreateRecord: Create a new record
        /// - UpdateRecord: Update an existing record
        /// - DeleteRecord: Delete a record
        /// - GetItem: Retrieve a single record
        /// - ListRecords: Query records
        /// </summary>
        private DataverseAction ParseDataverseAction(string actionName, ActionDefinition actionDef)
        {
            // Get ActionInputs from object
            ActionInputs inputs = null;
            if (actionDef.Inputs is JsonElement jsonElement)
            {
                inputs = JsonSerializer.Deserialize<ActionInputs>(jsonElement.GetRawText());
            }
            
            var operationId = inputs?.Host?.OperationId;
            if (string.IsNullOrEmpty(operationId))
                throw new InvalidOperationException($"Action '{actionName}' must have an operationId");

            var parameters = inputs?.Parameters ?? new Dictionary<string, object>();

            var action = new DataverseAction
            {
                Name = actionName,
                DataverseActionType = MapOperationId(operationId)
            };

            // Parse entity name
            if (parameters.TryGetValue("entityName", out var entityNameObj))
            {
                action.EntityLogicalName = GetStringValue(entityNameObj);
            }

            // Parse record ID (for Update, Delete, Retrieve)
            if (parameters.TryGetValue("recordId", out var recordIdObj))
            {
                var recordIdStr = GetStringValue(recordIdObj);
                if (!string.IsNullOrWhiteSpace(recordIdStr))
                {
                    // Handle expressions like @triggerOutputs()?['body/opportunityid']
                    // For now, we store the expression as-is and don't evaluate it
                    // In a real flow execution, expressions would be evaluated
                    action.Parameters["recordIdExpression"] = recordIdStr;
                }
            }

            // Parse attributes (for Create, Update)
            // Attributes are prefixed with "item/" in the parameters
            var attributes = new Dictionary<string, object>();
            foreach (var param in parameters)
            {
                if (param.Key.StartsWith("item/", StringComparison.OrdinalIgnoreCase))
                {
                    var attributeName = param.Key.Substring(5); // Remove "item/" prefix
                    
                    // Extract the actual value, handling JsonElement
                    var attrValue = param.Value;
                    if (attrValue is JsonElement attrJsonElement)
                    {
                        attrValue = GetJsonElementValue(attrJsonElement);
                    }
                    
                    attributes[attributeName] = attrValue;
                }
            }
            if (attributes.Any())
            {
                action.Attributes = attributes;
            }

            // Parse filter (for ListRecords)
            if (parameters.TryGetValue("$filter", out var filterObj))
            {
                action.Filter = GetStringValue(filterObj);
            }

            // Parse orderBy (for ListRecords)
            if (parameters.TryGetValue("$orderby", out var orderByObj))
            {
                action.OrderBy = GetStringValue(orderByObj);
            }

            // Parse top (for ListRecords)
            if (parameters.TryGetValue("$top", out var topObj))
            {
                action.Top = GetIntValue(topObj);
            }

            // Parse skip (for ListRecords - advanced paging)
            if (parameters.TryGetValue("$skip", out var skipObj))
            {
                action.Skip = GetIntValue(skipObj);
            }

            // Parse includeTotalCount (for ListRecords - returns @odata.count)
            if (parameters.TryGetValue("$count", out var countObj))
            {
                action.IncludeTotalCount = GetBoolValue(countObj);
            }

            // Parse expand (for ListRecords - navigation properties)
            if (parameters.TryGetValue("$expand", out var expandObj))
            {
                action.Expand = GetStringValue(expandObj);
            }

            // Parse column name (for UploadFile/DownloadFile)
            if (parameters.TryGetValue("columnName", out var columnNameObj))
            {
                action.ColumnName = GetStringValue(columnNameObj);
            }

            // Parse file name (for UploadFile/DownloadFile)
            if (parameters.TryGetValue("fileName", out var fileNameObj))
            {
                action.FileName = GetStringValue(fileNameObj);
            }

            // Parse file content (for UploadFile - base64 encoded in JSON)
            if (parameters.TryGetValue("fileContent", out var fileContentObj))
            {
                var fileContentStr = GetStringValue(fileContentObj);
                if (!string.IsNullOrWhiteSpace(fileContentStr))
                {
                    // In JSON, file content is typically base64-encoded
                    try
                    {
                        action.FileContent = Convert.FromBase64String(fileContentStr);
                    }
                    catch (FormatException)
                    {
                        // If not base64, treat as expression and store in Parameters
                        action.Parameters["fileContentExpression"] = fileContentStr;
                    }
                }
            }

            return action;
        }

        /// <summary>
        /// Maps the Dataverse operation ID to DataverseActionType.
        /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
        /// 
        /// Common operation IDs:
        /// - CreateRecord: Create a new record
        /// - UpdateRecord: Update an existing record  
        /// - DeleteRecord: Delete a record
        /// - GetItem: Retrieve a single record by ID
        /// - ListRecords: Query records (with $filter, $orderby, $top, $skip)
        /// - AssociateEntities: Relate two records (Associate)
        /// - UnassociateEntities: Unrelate two records (Disassociate)
        /// - PerformBoundAction: Execute a bound custom action
        /// - PerformUnboundAction: Execute an unbound custom action
        /// - UploadFile: Upload a file or image to a column
        /// - DownloadFile: Download a file or image from a column
        /// </summary>
        private DataverseActionType MapOperationId(string operationId)
        {
            switch (operationId)
            {
                case "CreateRecord":
                    return DataverseActionType.Create;
                case "UpdateRecord":
                    return DataverseActionType.Update;
                case "DeleteRecord":
                    return DataverseActionType.Delete;
                case "GetItem":
                    return DataverseActionType.Retrieve;
                case "ListRecords":
                    return DataverseActionType.ListRecords;
                case "AssociateEntities":
                case "RelateEntities":
                    return DataverseActionType.Relate;
                case "UnassociateEntities":
                case "UnrelateEntities":
                    return DataverseActionType.Unrelate;
                case "PerformBoundAction":
                    return DataverseActionType.ExecuteAction;
                case "PerformUnboundAction":
                    return DataverseActionType.PerformUnboundAction;
                case "UploadFile":
                    return DataverseActionType.UploadFile;
                case "DownloadFile":
                    return DataverseActionType.DownloadFile;
                default:
                    throw new NotSupportedException(
                        $"Operation ID '{operationId}' is not yet supported. " +
                        "Currently supported: CreateRecord, UpdateRecord, DeleteRecord, GetItem, ListRecords, " +
                        "AssociateEntities, UnassociateEntities, PerformBoundAction, PerformUnboundAction, " +
                        "UploadFile, DownloadFile.");
            }
        }

        /// <summary>
        /// Helper method to safely extract an integer value from a parameter object.
        /// Handles both direct integers and JsonElement objects from JSON deserialization.
        /// </summary>
        private int GetIntValue(object value)
        {
            if (value == null)
                return 0;

            if (value is int intValue)
                return intValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetInt32();
                
                if (jsonElement.ValueKind == JsonValueKind.String && 
                    int.TryParse(jsonElement.GetString(), out var parsed))
                    return parsed;
            }

            if (int.TryParse(value.ToString(), out var result))
                return result;

            throw new InvalidOperationException($"Cannot convert value '{value}' to integer");
        }

        /// <summary>
        /// Helper method to safely extract a string value from a parameter object.
        /// Handles both direct strings and JsonElement objects from JSON deserialization.
        /// </summary>
        private string GetStringValue(object value)
        {
            if (value == null)
                return null;

            if (value is string strValue)
                return strValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();
                
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.ToString();
            }

            return value.ToString();
        }

        /// <summary>
        /// Helper method to safely extract a boolean value from a parameter object.
        /// Handles both direct booleans and JsonElement objects from JSON deserialization.
        /// </summary>
        private bool GetBoolValue(object value)
        {
            if (value == null)
                return false;

            if (value is bool boolValue)
                return boolValue;

            if (value is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True)
                    return true;
                if (jsonElement.ValueKind == JsonValueKind.False)
                    return false;
                
                if (jsonElement.ValueKind == JsonValueKind.String && 
                    bool.TryParse(jsonElement.GetString(), out var parsed))
                    return parsed;
            }

            if (bool.TryParse(value.ToString(), out var result))
                return result;

            // Handle common string representations
            var strValue = value.ToString().ToLowerInvariant();
            return strValue == "true" || strValue == "1" || strValue == "yes";
        }

        /// <summary>
        /// Helper method to extract the appropriate value from a JsonElement.
        /// Converts JsonElement to the appropriate .NET type based on its ValueKind.
        /// </summary>
        private object GetJsonElementValue(JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.Number:
                    int intVal;
                    if (jsonElement.TryGetInt32(out intVal))
                        return intVal;
                    return jsonElement.GetDouble();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return jsonElement.ToString();
            }
        }

        /// <summary>
        /// Parses a Condition (If) action from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-conditional-statement
        /// 
        /// If actions contain an expression, actions to run when true, and optional else actions.
        /// </summary>
        private ConditionAction ParseConditionAction(string actionName, ActionDefinition actionDef)
        {
            var condition = new ConditionAction
            {
                Name = actionName,
                Expression = actionDef.Expression
            };

            // Parse true actions
            if (actionDef.Actions != null && actionDef.Actions.Any())
            {
                condition.TrueActions = ParseActions(actionDef.Actions);
            }

            // Parse false actions (else block)
            if (actionDef.Else?.Actions != null && actionDef.Else.Actions.Any())
            {
                condition.FalseActions = ParseActions(actionDef.Else.Actions);
            }

            return condition;
        }

        /// <summary>
        /// Parses a Switch action from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-switch-statement
        /// 
        /// Switch actions contain an expression, multiple cases with their actions, and a default case.
        /// </summary>
        private SwitchAction ParseSwitchAction(string actionName, ActionDefinition actionDef)
        {
            var switchAction = new SwitchAction
            {
                Name = actionName,
                Expression = actionDef.Expression
            };

            // Parse cases
            if (actionDef.Cases != null)
            {
                foreach (var caseEntry in actionDef.Cases)
                {
                    var caseName = caseEntry.Key;
                    var caseBlock = caseEntry.Value;
                    
                    if (caseBlock.Actions != null && caseBlock.Actions.Any())
                    {
                        var caseActions = ParseActions(caseBlock.Actions);
                        // Use the case value from the block, or fall back to the key
                        var caseValue = caseBlock.Case ?? caseName;
                        switchAction.Cases[caseValue] = caseActions;
                    }
                }
            }

            // Parse default case
            if (actionDef.Default?.Actions != null && actionDef.Default.Actions.Any())
            {
                switchAction.DefaultActions = ParseActions(actionDef.Default.Actions);
            }

            return switchAction;
        }

        /// <summary>
        /// Parses an Apply to Each (Foreach) action from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-loops#foreach-loop
        /// 
        /// Foreach actions contain an expression that evaluates to a collection and actions to run for each item.
        /// </summary>
        private ApplyToEachAction ParseApplyToEachAction(string actionName, ActionDefinition actionDef)
        {
            var applyToEach = new ApplyToEachAction
            {
                Name = actionName,
                Collection = actionDef.Expression
            };

            // Parse actions to execute for each item
            if (actionDef.Actions != null && actionDef.Actions.Any())
            {
                applyToEach.Actions = ParseActions(actionDef.Actions);
            }

            return applyToEach;
        }

        /// <summary>
        /// Parses a Do Until action from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-control-flow-loops#until-loop
        /// 
        /// Until actions contain an expression (exit condition), actions to repeat, and limits.
        /// </summary>
        private DoUntilAction ParseDoUntilAction(string actionName, ActionDefinition actionDef)
        {
            var doUntil = new DoUntilAction
            {
                Name = actionName,
                Expression = actionDef.Expression
            };

            // Parse actions to execute in the loop
            if (actionDef.Actions != null && actionDef.Actions.Any())
            {
                doUntil.Actions = ParseActions(actionDef.Actions);
            }

            // Parse limits
            if (actionDef.Limit != null)
            {
                doUntil.MaxIterations = actionDef.Limit.Count > 0 ? actionDef.Limit.Count : 60;
                doUntil.Timeout = actionDef.Limit.Timeout;
            }

            return doUntil;
        }

        /// <summary>
        /// Parses a Compose action from the JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-perform-data-operations#compose-action
        /// 
        /// Compose actions transform data using expressions and return the result.
        /// The inputs can be a simple expression string or complex object.
        /// </summary>
        private ComposeAction ParseComposeAction(string actionName, ActionDefinition actionDef)
        {
            var compose = new ComposeAction
            {
                Name = actionName,
                Inputs = actionDef.Inputs // Inputs can be any object (string, dict, array, etc.)
            };

            return compose;
        }
    }
}
