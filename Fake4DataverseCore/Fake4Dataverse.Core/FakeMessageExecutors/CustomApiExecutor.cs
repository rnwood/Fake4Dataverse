using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Xrm.Sdk;
using System;
using System.Linq;

namespace Fake4Dataverse.FakeMessageExecutors
{
    /// <summary>
    /// Executor for Custom API requests in Dataverse.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
    /// 
    /// Custom APIs are the modern way to create custom messages in Dataverse, replacing Custom Actions.
    /// They provide a way to define custom business logic that can be called via the Web API or SDK.
    /// 
    /// Key features:
    /// - Defined via customapi entity metadata
    /// - Support both Function (read operations) and Action (write operations) binding types
    /// - Have strongly-typed request and response parameters
    /// - Can be entity-bound or global
    /// - Support both synchronous and asynchronous execution
    /// </summary>
    public class CustomApiExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            // Custom APIs are executed via OrganizationRequest with RequestName matching the Custom API's UniqueName
            // Check if this is a potential Custom API request by verifying:
            // 1. It's a generic OrganizationRequest (not a typed request like CreateRequest)
            // 2. It has a RequestName property set
            if (request == null || string.IsNullOrEmpty(request.RequestName))
            {
                return false;
            }

            // We need to check if this RequestName corresponds to a registered Custom API
            // For now, we'll use a simple convention: if the RequestName is not a standard SDK message,
            // it might be a Custom API
            return IsCustomApiRequest(request);
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
            // Custom API execution involves:
            // 1. Validating the Custom API exists and is enabled
            // 2. Validating required input parameters
            // 3. Executing the associated plugin (if configured)
            // 4. Returning the response with output parameters

            if (request == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Custom API request cannot be null.");
            }

            if (string.IsNullOrEmpty(request.RequestName))
            {
                throw FakeOrganizationServiceFaultFactory.New("Custom API RequestName cannot be null or empty.");
            }

            var service = ctx.GetOrganizationService();
            var customApiName = request.RequestName;

            // Retrieve the Custom API definition from the context
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables
            // The customapi table contains the definition of custom APIs
            var customApiQuery = ctx.CreateQuery("customapi")
                .Where(api => api.GetAttributeValue<string>("uniquename") == customApiName)
                .FirstOrDefault();

            if (customApiQuery == null)
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    $"Custom API with unique name '{customApiName}' is not registered in the system.");
            }

            // Check if the Custom API is enabled
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapi-table-columns
            // The isenabled column determines if the API is available for execution
            var isEnabled = customApiQuery.GetAttributeValue<bool>("isenabled");
            if (!isEnabled)
            {
                throw FakeOrganizationServiceFaultFactory.New(
                    $"Custom API '{customApiName}' is not enabled.");
            }

            // Validate required input parameters
            ValidateInputParameters(request, customApiName, ctx);

            // Execute plugins if pipeline simulation is enabled
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api#custom-api-and-plug-ins
            // Custom APIs can have plugins registered to run at different stages
            if (ctx.UsePipelineSimulation && ctx.PluginPipelineSimulator != null)
            {
                // Create a target entity for the custom action
                // For entity-bound custom APIs, this would be the bound entity
                // For global custom APIs, we use a placeholder
                Entity targetEntity = null;
                if (customApiQuery.Contains("boundentitylogicalname") &&
                    !string.IsNullOrEmpty(customApiQuery.GetAttributeValue<string>("boundentitylogicalname")))
                {
                    var boundEntityName = customApiQuery.GetAttributeValue<string>("boundentitylogicalname");
                    // Check if Target parameter contains the bound entity
                    if (request.Parameters.Contains("Target") && request.Parameters["Target"] is EntityReference targetRef)
                    {
                        targetEntity = new Entity(boundEntityName) { Id = targetRef.Id };
                    }
                    else if (request.Parameters.Contains("Target") && request.Parameters["Target"] is Entity target)
                    {
                        targetEntity = target;
                    }
                }

                // Execute PreValidation stage
                ctx.PluginPipelineSimulator.ExecutePipelineStage(
                    customApiName,
                    targetEntity?.LogicalName ?? string.Empty,
                    Abstractions.Plugins.Enums.ProcessingStepStage.Prevalidation,
                    targetEntity,
                    null, // No modified attributes for custom actions
                    null, // Pre-images
                    null, // Post-images
                    ctx.CallerProperties?.CallerId?.Id,
                    null, // Organization ID
                    1); // Initial depth

                // Execute PreOperation stage
                ctx.PluginPipelineSimulator.ExecutePipelineStage(
                    customApiName,
                    targetEntity?.LogicalName ?? string.Empty,
                    Abstractions.Plugins.Enums.ProcessingStepStage.Preoperation,
                    targetEntity,
                    null,
                    null,
                    null,
                    ctx.CallerProperties?.CallerId?.Id,
                    null,
                    1);
            }

            // Execute the Custom API logic
            // In a real implementation, this would invoke the associated plugin
            // For testing purposes, we'll create a mock response based on the defined output parameters
            var response = CreateCustomApiResponse(request, customApiQuery, ctx);

            // Execute plugins if pipeline simulation is enabled
            if (ctx.UsePipelineSimulation && ctx.PluginPipelineSimulator != null)
            {
                Entity targetEntity = null;
                if (customApiQuery.Contains("boundentitylogicalname") &&
                    !string.IsNullOrEmpty(customApiQuery.GetAttributeValue<string>("boundentitylogicalname")))
                {
                    var boundEntityName = customApiQuery.GetAttributeValue<string>("boundentitylogicalname");
                    if (request.Parameters.Contains("Target") && request.Parameters["Target"] is EntityReference targetRef)
                    {
                        targetEntity = new Entity(boundEntityName) { Id = targetRef.Id };
                    }
                    else if (request.Parameters.Contains("Target") && request.Parameters["Target"] is Entity target)
                    {
                        targetEntity = target;
                    }
                }

                // Execute PostOperation stage
                ctx.PluginPipelineSimulator.ExecutePipelineStage(
                    customApiName,
                    targetEntity?.LogicalName ?? string.Empty,
                    Abstractions.Plugins.Enums.ProcessingStepStage.Postoperation,
                    targetEntity,
                    null,
                    null,
                    null,
                    ctx.CallerProperties?.CallerId?.Id,
                    null,
                    1);
            }

            return response;
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(OrganizationRequest);
        }

        /// <summary>
        /// Determines if the request is a Custom API request.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api
        /// Custom APIs use OrganizationRequest with the UniqueName as the RequestName.
        /// </summary>
        private bool IsCustomApiRequest(OrganizationRequest request)
        {
            // Standard SDK messages that should not be treated as Custom APIs
            var standardMessages = new[]
            {
                "Create", "Update", "Delete", "Retrieve", "RetrieveMultiple",
                "Associate", "Disassociate", "Execute", "ExecuteMultiple",
                "Merge", "SetState", "Assign", "GrantAccess", "ModifyAccess", "RevokeAccess"
            };

            // If the RequestName matches a standard message, it's not a Custom API
            if (standardMessages.Contains(request.RequestName, StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }

            // If the request is a typed request (not OrganizationRequest), it's not a Custom API
            if (request.GetType() != typeof(OrganizationRequest))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validates that all required input parameters are provided.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapirequestparameter-table
        /// Request parameters are defined in the customapirequestparameter table.
        /// </summary>
        private void ValidateInputParameters(OrganizationRequest request, string customApiName, IXrmFakedContext ctx)
        {
            // Query for required input parameters
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapirequestparameter-table-columns
            var requiredParams = ctx.CreateQuery("customapirequestparameter")
                .Where(p => p.GetAttributeValue<EntityReference>("customapiid") != null &&
                           p.GetAttributeValue<bool>("isoptional") == false)
                .ToList();

            foreach (var param in requiredParams)
            {
                var paramName = param.GetAttributeValue<string>("uniquename");
                if (!request.Parameters.Contains(paramName))
                {
                    throw FakeOrganizationServiceFaultFactory.New(
                        $"Required parameter '{paramName}' is missing for Custom API '{customApiName}'.");
                }
            }
        }

        /// <summary>
        /// Creates the Custom API response with output parameters.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapiresponseparameter-table
        /// Response parameters are defined in the customapiresponseparameter table.
        /// </summary>
        private OrganizationResponse CreateCustomApiResponse(OrganizationRequest request, Entity customApi, IXrmFakedContext ctx)
        {
            var response = new OrganizationResponse
            {
                ResponseName = request.RequestName,
                Results = new ParameterCollection()
            };

            // Query for output parameters
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#customapiresponseparameter-table-columns
            var outputParams = ctx.CreateQuery("customapiresponseparameter")
                .Where(p => p.GetAttributeValue<EntityReference>("customapiid") != null &&
                           p.GetAttributeValue<EntityReference>("customapiid").Id == customApi.Id)
                .ToList();

            // For each output parameter, add a default value to the response
            // In a real scenario, these would be populated by the plugin execution
            foreach (var param in outputParams)
            {
                var paramName = param.GetAttributeValue<string>("uniquename");
                var paramType = param.GetAttributeValue<OptionSetValue>("type");

                // Add default values based on parameter type
                // In testing scenarios, plugins would populate these values
                object defaultValue = GetDefaultValueForType(paramType?.Value ?? 0);
                response.Results[paramName] = defaultValue;
            }

            return response;
        }

        /// <summary>
        /// Gets default values for Custom API parameter types.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#parameter-data-types
        /// </summary>
        private object GetDefaultValueForType(int typeValue)
        {
            // Custom API parameter types from Microsoft documentation:
            // 0 = Boolean, 1 = DateTime, 2 = Decimal, 3 = Entity, 4 = EntityCollection,
            // 5 = EntityReference, 6 = Float, 7 = Integer, 8 = Money, 9 = Picklist, 10 = String,
            // 11 = StringArray, 12 = Guid
            switch (typeValue)
            {
                case 0:
                    return false;                      // Boolean
                case 1:
                    return DateTime.UtcNow;           // DateTime
                case 2:
                    return 0m;                        // Decimal
                case 3:
                    return null;                      // Entity
                case 4:
                    return new EntityCollection();    // EntityCollection
                case 5:
                    return null;                      // EntityReference
                case 6:
                    return 0f;                        // Float
                case 7:
                    return 0;                         // Integer
                case 8:
                    return new Money(0m);             // Money
                case 9:
                    return new OptionSetValue(0);     // Picklist
                case 10:
                    return string.Empty;             // String
                case 11:
                    return new string[0];            // StringArray
                case 12:
                    return Guid.Empty;               // Guid
                default:
                    return null;
            }
        }
    }
}
