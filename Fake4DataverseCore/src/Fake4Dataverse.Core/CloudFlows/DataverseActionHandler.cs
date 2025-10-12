using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;
using Fake4Dataverse.CloudFlows.Expressions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Built-in connector action handler for Dataverse operations.
    /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
    /// 
    /// Handles all Dataverse connector actions:
    /// - Create, Retrieve, Update, Delete operations
    /// - ListRecords with filtering
    /// - Associate/Disassociate relationships
    /// - ExecuteAction for custom actions/APIs
    /// 
    /// This handler supports expression evaluation using the Power Automate expression language.
    /// Expressions in action parameters and attributes are automatically evaluated.
    /// </summary>
    public class DataverseActionHandler : IConnectorActionHandler
    {
        public string ConnectorType => "Dataverse";

        public bool CanHandle(IFlowAction action)
        {
            return action != null && action.ActionType == "Dataverse";
        }

        public IDictionary<string, object> Execute(
            IFlowAction action, 
            IXrmFakedContext context, 
            IFlowExecutionContext flowContext)
        {
            if (!(action is DataverseAction dataverseAction))
            {
                throw new ArgumentException("Action must be of type DataverseAction", nameof(action));
            }

            // Create expression evaluator for this flow execution context
            var expressionEvaluator = new ExpressionEvaluator(flowContext);

            // Evaluate expressions in action attributes and parameters
            EvaluateExpressions(dataverseAction, expressionEvaluator);

            var service = context.GetOrganizationService();

            switch (dataverseAction.DataverseActionType)
            {
                case DataverseActionType.Create:
                    return HandleCreate(dataverseAction, service);

                case DataverseActionType.Retrieve:
                    return HandleRetrieve(dataverseAction, service);

                case DataverseActionType.Update:
                    return HandleUpdate(dataverseAction, service);

                case DataverseActionType.Delete:
                    return HandleDelete(dataverseAction, service);

                case DataverseActionType.ListRecords:
                    return HandleListRecords(dataverseAction, service);

                case DataverseActionType.Relate:
                    return HandleRelate(dataverseAction, service, context);

                case DataverseActionType.Unrelate:
                    return HandleUnrelate(dataverseAction, service, context);

                case DataverseActionType.ExecuteAction:
                case DataverseActionType.PerformUnboundAction:
                    return HandleExecuteAction(dataverseAction, service);

                default:
                    throw new NotImplementedException(
                        $"Dataverse action type '{dataverseAction.DataverseActionType}' is not yet implemented");
            }
        }

        /// <summary>
        /// Handle Create action
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-create
        /// 
        /// Attributes are converted from OData format (used by Power Automate/Web API) to SDK types.
        /// This includes converting integers to OptionSetValue, decimals to Money, etc.
        /// </summary>
        private IDictionary<string, object> HandleCreate(DataverseAction action, IOrganizationService service)
        {
            var entity = new Entity(action.EntityLogicalName);

            // Set attributes from action - convert OData values to SDK types
            if (action.Attributes != null)
            {
                var convertedAttributes = ODataValueConverter.ConvertODataAttributes(
                    action.Attributes as Dictionary<string, object>, 
                    action.EntityLogicalName);

                foreach (var attr in convertedAttributes)
                {
                    entity[attr.Key] = attr.Value;
                }
            }

            // Create the record
            var createdId = service.Create(entity);

            return new Dictionary<string, object>
            {
                ["id"] = createdId.ToString(),
                [action.EntityLogicalName + "id"] = createdId.ToString(),
                ["entityLogicalName"] = action.EntityLogicalName
            };
        }

        /// <summary>
        /// Handle Retrieve action
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-retrieve
        /// </summary>
        private IDictionary<string, object> HandleRetrieve(DataverseAction action, IOrganizationService service)
        {
            if (!action.EntityId.HasValue)
                throw new ArgumentException("EntityId is required for Retrieve action");

            var columns = new ColumnSet(true); // Retrieve all columns by default

            var entity = service.Retrieve(action.EntityLogicalName, action.EntityId.Value, columns);

            var result = new Dictionary<string, object>
            {
                ["id"] = entity.Id.ToString(),
                [action.EntityLogicalName + "id"] = entity.Id.ToString()
            };

            // Add all attributes to result
            foreach (var attr in entity.Attributes)
            {
                result[attr.Key] = attr.Value;
            }

            return result;
        }

        /// <summary>
        /// Handle Update action
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-update
        /// 
        /// Attributes are converted from OData format (used by Power Automate/Web API) to SDK types.
        /// </summary>
        private IDictionary<string, object> HandleUpdate(DataverseAction action, IOrganizationService service)
        {
            if (!action.EntityId.HasValue)
                throw new ArgumentException("EntityId is required for Update action");

            var entity = new Entity(action.EntityLogicalName)
            {
                Id = action.EntityId.Value
            };

            // Set attributes from action - convert OData values to SDK types
            if (action.Attributes != null)
            {
                var convertedAttributes = ODataValueConverter.ConvertODataAttributes(
                    action.Attributes as Dictionary<string, object>,
                    action.EntityLogicalName);

                foreach (var attr in convertedAttributes)
                {
                    entity[attr.Key] = attr.Value;
                }
            }

            // Update the record
            service.Update(entity);

            return new Dictionary<string, object>
            {
                ["id"] = action.EntityId.Value.ToString(),
                [action.EntityLogicalName + "id"] = action.EntityId.Value.ToString(),
                ["success"] = true
            };
        }

        /// <summary>
        /// Handle Delete action
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-delete
        /// </summary>
        private IDictionary<string, object> HandleDelete(DataverseAction action, IOrganizationService service)
        {
            if (!action.EntityId.HasValue)
                throw new ArgumentException("EntityId is required for Delete action");

            service.Delete(action.EntityLogicalName, action.EntityId.Value);

            return new Dictionary<string, object>
            {
                ["success"] = true,
                ["deletedId"] = action.EntityId.Value.ToString()
            };
        }

        /// <summary>
        /// Handle ListRecords action
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-query-data
        /// </summary>
        private IDictionary<string, object> HandleListRecords(DataverseAction action, IOrganizationService service)
        {
            var query = new QueryExpression(action.EntityLogicalName)
            {
                ColumnSet = new ColumnSet(true)
            };

            // Apply filter if specified
            if (!string.IsNullOrWhiteSpace(action.Filter))
            {
                // TODO: Parse OData-style filter to QueryExpression
                // For now, retrieve all records
                // Full filter parsing would be implemented in a future enhancement
            }

            // Apply ordering if specified
            if (!string.IsNullOrWhiteSpace(action.OrderBy))
            {
                var orderParts = action.OrderBy.Split(' ');
                var attributeName = orderParts[0];
                var orderType = orderParts.Length > 1 && orderParts[1].Equals("desc", StringComparison.OrdinalIgnoreCase)
                    ? OrderType.Descending
                    : OrderType.Ascending;

                query.Orders.Add(new OrderExpression(attributeName, orderType));
            }

            // Apply top limit if specified
            if (action.Top.HasValue)
            {
                query.TopCount = action.Top.Value;
            }

            var results = service.RetrieveMultiple(query);

            // Convert results to output format
            var records = new List<Dictionary<string, object>>();
            foreach (var entity in results.Entities)
            {
                var record = new Dictionary<string, object>
                {
                    ["id"] = entity.Id.ToString(),
                    [action.EntityLogicalName + "id"] = entity.Id.ToString()
                };

                foreach (var attr in entity.Attributes)
                {
                    record[attr.Key] = attr.Value;
                }

                records.Add(record);
            }

            return new Dictionary<string, object>
            {
                ["value"] = records,
                ["count"] = records.Count
            };
        }

        /// <summary>
        /// Handle Relate action (Associate)
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-associate-disassociate
        /// </summary>
        private IDictionary<string, object> HandleRelate(
            DataverseAction action, 
            IOrganizationService service,
            IXrmFakedContext context)
        {
            // For Relate action, we need relationship name and related entity references
            // These would typically be in Parameters
            if (action.Parameters == null || !action.Parameters.ContainsKey("RelationshipName"))
                throw new ArgumentException("RelationshipName parameter is required for Relate action");

            var relationshipName = action.Parameters["RelationshipName"].ToString();
            var entity1Ref = action.Parameters["Entity1"] as EntityReference;
            var entity2Ref = action.Parameters["Entity2"] as EntityReference;

            if (entity1Ref == null || entity2Ref == null)
                throw new ArgumentException("Entity1 and Entity2 parameters are required for Relate action");

            service.Associate(
                entity1Ref.LogicalName,
                entity1Ref.Id,
                new Relationship(relationshipName),
                new EntityReferenceCollection { entity2Ref });

            return new Dictionary<string, object>
            {
                ["success"] = true
            };
        }

        /// <summary>
        /// Handle Unrelate action (Disassociate)
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/entity-operations-associate-disassociate
        /// </summary>
        private IDictionary<string, object> HandleUnrelate(
            DataverseAction action,
            IOrganizationService service,
            IXrmFakedContext context)
        {
            // For Unrelate action, we need relationship name and related entity references
            if (action.Parameters == null || !action.Parameters.ContainsKey("RelationshipName"))
                throw new ArgumentException("RelationshipName parameter is required for Unrelate action");

            var relationshipName = action.Parameters["RelationshipName"].ToString();
            var entity1Ref = action.Parameters["Entity1"] as EntityReference;
            var entity2Ref = action.Parameters["Entity2"] as EntityReference;

            if (entity1Ref == null || entity2Ref == null)
                throw new ArgumentException("Entity1 and Entity2 parameters are required for Unrelate action");

            service.Disassociate(
                entity1Ref.LogicalName,
                entity1Ref.Id,
                new Relationship(relationshipName),
                new EntityReferenceCollection { entity2Ref });

            return new Dictionary<string, object>
            {
                ["success"] = true
            };
        }

        /// <summary>
        /// Handle ExecuteAction (custom actions/APIs)
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-actions
        /// </summary>
        private IDictionary<string, object> HandleExecuteAction(DataverseAction action, IOrganizationService service)
        {
            if (action.Parameters == null || !action.Parameters.ContainsKey("ActionName"))
                throw new ArgumentException("ActionName parameter is required for ExecuteAction");

            var actionName = action.Parameters["ActionName"].ToString();

            var request = new OrganizationRequest(actionName);

            // Add all parameters except ActionName
            foreach (var param in action.Parameters.Where(p => p.Key != "ActionName"))
            {
                request.Parameters[param.Key] = param.Value;
            }

            var response = service.Execute(request);

            // Convert response parameters to dictionary
            var result = new Dictionary<string, object>
            {
                ["success"] = true
            };

            foreach (var param in response.Results)
            {
                result[param.Key] = param.Value;
            }

            return result;
        }

        /// <summary>
        /// Evaluates expressions in action attributes and parameters using the ExpressionEvaluator.
        /// This enables dynamic values from trigger data and previous action outputs.
        /// </summary>
        private void EvaluateExpressions(DataverseAction action, ExpressionEvaluator evaluator)
        {
            // Evaluate expressions in Attributes
            if (action.Attributes != null)
            {
                var evaluatedAttributes = new Dictionary<string, object>();
                foreach (var attr in action.Attributes)
                {
                    evaluatedAttributes[attr.Key] = evaluator.Evaluate(attr.Value);
                }
                action.Attributes = evaluatedAttributes;
            }

            // Evaluate expressions in Parameters
            if (action.Parameters != null)
            {
                var evaluatedParameters = new Dictionary<string, object>();
                foreach (var param in action.Parameters)
                {
                    evaluatedParameters[param.Key] = evaluator.Evaluate(param.Value);
                }
                action.Parameters = evaluatedParameters;
            }

            // Evaluate expression in EntityId if it's stored as string in Parameters
            if (action.Parameters != null && action.Parameters.ContainsKey("recordIdExpression"))
            {
                var evaluated = evaluator.Evaluate(action.Parameters["recordIdExpression"]);
                if (evaluated != null && Guid.TryParse(evaluated.ToString(), out var entityId))
                {
                    action.EntityId = entityId;
                }
            }
        }
    }
}
