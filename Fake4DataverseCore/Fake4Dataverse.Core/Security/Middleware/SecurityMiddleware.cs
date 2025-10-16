using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Middleware;
using Fake4Dataverse.Abstractions.Security;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Linq;

namespace Fake4Dataverse.Security.Middleware
{
    /// <summary>
    /// Middleware that enforces Dataverse security model.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
    /// 
    /// This middleware checks:
    /// - User privileges for operations
    /// - Record-level access (ownership and sharing)
    /// - Field-level security
    /// - Business unit hierarchy
    /// </summary>
    public static class SecurityMiddleware
    {
        /// <summary>
        /// Adds security enforcement middleware to the pipeline.
        /// Only enforces security if SecurityConfiguration.SecurityEnabled is true.
        /// </summary>
        public static IMiddlewareBuilder AddSecurity(this IMiddlewareBuilder builder)
        {
            return builder.Use(next => (context, request) =>
            {
                // Only enforce security if enabled
                if (!context.SecurityConfiguration.SecurityEnabled)
                {
                    return next(context, request);
                }

                // Check if user is System Administrator - they bypass all security
                var callerId = context.CallerProperties.CallerId;
                
                if (callerId != null && callerId.LogicalName == "systemuser")
                {
                    if (context.SecurityManager.IsSystemAdministrator(callerId.Id))
                    {
                        // System Administrators bypass all security checks
                        return next(context, request);
                    }
                }

                // Enforce security based on request type
                EnforceSecurityForRequest(context, request);

                // Continue to next middleware
                return next(context, request);
            });
        }

        private static void EnforceSecurityForRequest(IXrmFakedContext context, OrganizationRequest request)
        {
            var callerId = context.CallerProperties.CallerId;

            // Handle CRUD operations
            if (request is CreateRequest createRequest)
            {
                EnforceCreateSecurity(context, createRequest.Target, callerId);
            }
            else if (request is UpdateRequest updateRequest)
            {
                EnforceUpdateSecurity(context, updateRequest.Target, callerId);
            }
            else if (request is DeleteRequest deleteRequest)
            {
                EnforceDeleteSecurity(context, deleteRequest.Target, callerId);
            }
            else if (request is RetrieveRequest retrieveRequest)
            {
                EnforceRetrieveSecurity(context, retrieveRequest.Target, callerId);
            }
            else if (request is RetrieveMultipleRequest retrieveMultipleRequest)
            {
                EnforceRetrieveMultipleSecurity(context, retrieveMultipleRequest.Query, callerId);
            }
            // Handle privilege-specific operations
            else if (request is AssignRequest assignRequest)
            {
                EnforceAssignSecurity(context, assignRequest.Target, callerId);
            }
            else if (request is SetStateRequest setStateRequest)
            {
                EnforceSetStateSecurity(context, setStateRequest.EntityMoniker, callerId);
            }
        }

        private static void EnforceCreateSecurity(IXrmFakedContext context, Entity target, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot create records without a user context.");
            }

            // Check Create privilege for the entity
            // TODO: Implement privilege checking based on security roles
            // For now, allow if record-level security is not enforced
            if (!context.SecurityConfiguration.EnforceRecordLevelSecurity)
            {
                return;
            }

            // If record-level security is enforced, check if the user owns the record
            // or has appropriate access through their business unit
            ValidateRecordLevelAccess(context, target.LogicalName, Guid.Empty, callerId, AccessRights.CreateAccess);
        }

        private static void EnforceUpdateSecurity(IXrmFakedContext context, Entity target, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot update records without a user context.");
            }

            if (context.SecurityConfiguration.EnforceRecordLevelSecurity)
            {
                ValidateRecordLevelAccess(context, target.LogicalName, target.Id, callerId, AccessRights.WriteAccess);
            }

            if (context.SecurityConfiguration.EnforceFieldLevelSecurity)
            {
                ValidateFieldLevelSecurity(context, target, callerId);
            }
        }

        private static void EnforceDeleteSecurity(IXrmFakedContext context, EntityReference target, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot delete records without a user context.");
            }

            if (context.SecurityConfiguration.EnforceRecordLevelSecurity)
            {
                ValidateRecordLevelAccess(context, target.LogicalName, target.Id, callerId, AccessRights.DeleteAccess);
            }
        }

        private static void EnforceRetrieveSecurity(IXrmFakedContext context, EntityReference target, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot retrieve records without a user context.");
            }

            if (context.SecurityConfiguration.EnforceRecordLevelSecurity)
            {
                ValidateRecordLevelAccess(context, target.LogicalName, target.Id, callerId, AccessRights.ReadAccess);
            }
        }

        private static void EnforceRetrieveMultipleSecurity(IXrmFakedContext context, QueryBase query, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot retrieve records without a user context.");
            }

            // For RetrieveMultiple, we would filter results based on record-level security
            // This is a complex operation that requires filtering the result set
            // For now, we'll allow the query to proceed and filter results in the CRUD layer
        }

        private static void EnforceAssignSecurity(IXrmFakedContext context, EntityReference target, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot assign records without a user context.");
            }

            if (context.SecurityConfiguration.EnforceRecordLevelSecurity)
            {
                ValidateRecordLevelAccess(context, target.LogicalName, target.Id, callerId, AccessRights.AssignAccess);
            }
        }

        private static void EnforceSetStateSecurity(IXrmFakedContext context, EntityReference target, EntityReference callerId)
        {
            if (callerId == null)
            {
                throw new UnauthorizedAccessException("No caller specified. Cannot change record state without a user context.");
            }

            if (context.SecurityConfiguration.EnforceRecordLevelSecurity)
            {
                ValidateRecordLevelAccess(context, target.LogicalName, target.Id, callerId, AccessRights.WriteAccess);
            }
        }

        /// <summary>
        /// Validates that the caller has the specified access rights to the record.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/security-sharing-assigning
        /// </summary>
        private static void ValidateRecordLevelAccess(IXrmFakedContext context, string entityName, Guid recordId, EntityReference callerId, AccessRights requiredAccess)
        {
            // If recordId is empty, this is a create operation - allow based on privilege
            if (recordId == Guid.Empty)
            {
                return;
            }

            // Get the record to check ownership
            var record = context.GetEntityById(entityName, recordId);
            if (record == null)
            {
                throw new InvalidOperationException($"Record {entityName} with ID {recordId} not found.");
            }

            // Check if user owns the record
            if (record.Contains("ownerid"))
            {
                var ownerId = record.GetAttributeValue<EntityReference>("ownerid");
                if (ownerId != null && ownerId.Id == callerId.Id)
                {
                    // User owns the record - allow access
                    return;
                }
            }

            // Check shared access through PrincipalObjectAccess
            var accessRightsRepo = context.GetProperty<Fake4Dataverse.Abstractions.Permissions.IAccessRightsRepository>();
            if (accessRightsRepo != null)
            {
                try
                {
                    var principalAccess = accessRightsRepo.RetrievePrincipalAccess(
                        new EntityReference(entityName, recordId),
                        callerId
                    );

                    if (principalAccess != null && principalAccess.AccessRights.HasFlag(requiredAccess))
                    {
                        // User has shared access - allow
                        return;
                    }
                }
                catch
                {
                    // No shared access found
                }
            }

            // Check business unit hierarchy if EnforcePrivilegeDepth is enabled
            if (context.SecurityConfiguration.EnforcePrivilegeDepth)
            {
                // TODO: Implement business unit hierarchy checking
                // This would check if the user's business unit has access based on privilege depth
            }

            // If we get here, access is denied
            throw new UnauthorizedAccessException(
                $"User {callerId.Id} does not have {requiredAccess} access to {entityName} record {recordId}.");
        }

        /// <summary>
        /// Validates field-level security for update operations.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/field-security-entities
        /// </summary>
        private static void ValidateFieldLevelSecurity(IXrmFakedContext context, Entity target, EntityReference callerId)
        {
            // Field-level security checks if specific attributes have field security enabled
            // and if the user has the necessary field security profile
            
            var entityMetadata = context.GetEntityMetadataByName(target.LogicalName);
            if (entityMetadata == null)
            {
                return;
            }

            foreach (var attributeName in target.Attributes.Keys)
            {
                var attributeMetadata = entityMetadata.Attributes?.FirstOrDefault(a => a.LogicalName == attributeName);
                if (attributeMetadata == null)
                {
                    continue;
                }

                // Check if field security is enabled for this attribute
                // In real Dataverse, this is stored in the FieldSecurityProfile and FieldPermission entities
                // For now, we'll allow all field updates if the attribute exists
                // TODO: Implement full field-level security checking
            }
        }
    }
}
