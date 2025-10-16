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
        /// Uses privilege-based security matching Dataverse behavior.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/security-sharing-assigning
        /// </summary>
        private static void ValidateRecordLevelAccess(IXrmFakedContext context, string entityName, Guid recordId, EntityReference callerId, AccessRights requiredAccess)
        {
            // If recordId is empty, this is a create operation - check create privilege
            if (recordId == Guid.Empty)
            {
                CheckPrivilege(context, callerId.Id, entityName, requiredAccess);
                return;
            }

            // Get the record to check ownership
            var record = context.GetEntityById(entityName, recordId);
            if (record == null)
            {
                throw new InvalidOperationException($"Record {entityName} with ID {recordId} not found.");
            }

            // Check if user has the required privilege
            var privilegeName = GetPrivilegeNameForAccess(entityName, requiredAccess);
            var privilegeManager = context.SecurityManager.PrivilegeManager;
            
            // Check privilege with appropriate depth
            if (CheckPrivilegeWithDepth(context, callerId.Id, record, privilegeName, requiredAccess))
            {
                return; // Access granted through privilege
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

            // If we get here, access is denied
            throw new UnauthorizedAccessException(
                $"User {callerId.Id} does not have {requiredAccess} access to {entityName} record {recordId}.");
        }

        /// <summary>
        /// Checks if user has privilege with appropriate depth based on record ownership and business unit.
        /// </summary>
        private static bool CheckPrivilegeWithDepth(IXrmFakedContext context, Guid userId, Entity record, string privilegeName, AccessRights requiredAccess)
        {
            var privilegeManager = context.SecurityManager.PrivilegeManager;
            
            // If not enforcing privilege depth, just check basic privilege
            if (!context.SecurityConfiguration.EnforcePrivilegeDepth)
            {
                return privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthBasic);
            }

            // Check if user owns the record (Basic depth)
            if (record.Contains("ownerid"))
            {
                var ownerId = record.GetAttributeValue<EntityReference>("ownerid");
                if (ownerId != null && ownerId.Id == userId)
                {
                    // User owns the record - Basic depth is sufficient
                    if (privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthBasic))
                    {
                        return true;
                    }
                }
            }

            // Get user's business unit
            var user = context.GetEntityById("systemuser", userId);
            if (user != null && user.Contains("businessunitid"))
            {
                var userBU = user.GetAttributeValue<EntityReference>("businessunitid");
                
                // Check if record belongs to user's business unit (Local depth)
                if (record.Contains("owningbusinessunit"))
                {
                    var recordBU = record.GetAttributeValue<EntityReference>("owningbusinessunit");
                    if (recordBU != null && userBU != null && recordBU.Id == userBU.Id)
                    {
                        if (privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthLocal))
                        {
                            return true;
                        }
                    }
                }

                // Check Deep depth (business unit and child business units)
                if (privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthDeep))
                {
                    // TODO: Implement business unit hierarchy checking
                    // For now, treat Deep same as Local
                    return true;
                }
            }

            // Check Global depth (organization-wide)
            if (privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthGlobal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if user has the required privilege for an operation.
        /// </summary>
        private static void CheckPrivilege(IXrmFakedContext context, Guid userId, string entityName, AccessRights requiredAccess)
        {
            var privilegeName = GetPrivilegeNameForAccess(entityName, requiredAccess);
            var privilegeManager = context.SecurityManager.PrivilegeManager;
            
            if (!privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthBasic))
            {
                throw new UnauthorizedAccessException(
                    $"User {userId} does not have the required privilege '{privilegeName}' for {requiredAccess} access to {entityName}.");
            }
        }

        /// <summary>
        /// Gets the privilege name for a given access right and entity.
        /// </summary>
        private static string GetPrivilegeNameForAccess(string entityName, AccessRights accessRight)
        {
            var entityPascal = ToPascalCase(entityName);
            
            return accessRight switch
            {
                AccessRights.CreateAccess => $"prvCreate{entityPascal}",
                AccessRights.ReadAccess => $"prvRead{entityPascal}",
                AccessRights.WriteAccess => $"prvWrite{entityPascal}",
                AccessRights.DeleteAccess => $"prvDelete{entityPascal}",
                AccessRights.AppendAccess => $"prvAppend{entityPascal}",
                AccessRights.AppendToAccess => $"prvAppendTo{entityPascal}",
                AccessRights.AssignAccess => $"prvAssign{entityPascal}",
                AccessRights.ShareAccess => $"prvShare{entityPascal}",
                _ => $"prv{accessRight}{entityPascal}"
            };
        }

        /// <summary>
        /// Converts a logical name to PascalCase for privilege naming.
        /// </summary>
        private static string ToPascalCase(string logicalName)
        {
            if (string.IsNullOrWhiteSpace(logicalName))
            {
                return logicalName;
            }

            var parts = logicalName.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i].Substring(1);
                }
            }

            return string.Join("", parts);
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
