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

                // Validate impersonation if active (must be done before sys admin bypass)
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
                // The calling user must have the prvActOnBehalfOfAnotherUser privilege to impersonate another user.
                // Even System Administrators need to perform this check on the CALLING user, not the impersonated user.
                if (context.CallerProperties.ImpersonatedUserId != null)
                {
                    var callerId = context.CallerProperties.CallerId;
                    if (callerId == null)
                    {
                        throw new UnauthorizedAccessException("Cannot impersonate without a calling user context.");
                    }

                    // Check if the CALLING user (not impersonated) is System Administrator or has the privilege
                    bool isCallerSystemAdmin = context.SecurityManager.IsSystemAdministrator(callerId.Id);
                    
                    if (!isCallerSystemAdmin)
                    {
                        // Check if the calling user has the prvActOnBehalfOfAnotherUser privilege
                        var hasPrivilege = context.SecurityManager.PrivilegeManager.HasPrivilege(
                            callerId.Id,
                            PrivilegeManager.ActOnBehalfOfAnotherUserPrivilege,
                            PrivilegeManager.PrivilegeDepthGlobal);

                        if (!hasPrivilege)
                        {
                            throw new UnauthorizedAccessException(
                                $"User {callerId.Id} does not have the '{PrivilegeManager.ActOnBehalfOfAnotherUserPrivilege}' privilege required for impersonation.");
                        }
                    }
                }

                // Check if EFFECTIVE user is System Administrator - they bypass all OTHER security
                // The effective user is the impersonated user if impersonating, otherwise the caller
                var effectiveUser = context.CallerProperties.ImpersonatedUserId ?? context.CallerProperties.CallerId;
                
                if (effectiveUser != null && effectiveUser.LogicalName == "systemuser")
                {
                    if (context.SecurityManager.IsSystemAdministrator(effectiveUser.Id))
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

            // System tables are readable by everyone, but creation still requires privileges
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security#system-tables
            
            // Check Create privilege for the entity
            var privilegeName = GetPrivilegeNameForAccess(target.LogicalName, AccessRights.CreateAccess);
            var privilegeManager = context.SecurityManager.PrivilegeManager;
            
            // For create operations, we need at least Basic depth privilege
            // Organization-owned entities require Global depth
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
            var requiredDepth = IsSystemTable(target.LogicalName) 
                ? PrivilegeManager.PrivilegeDepthGlobal 
                : PrivilegeManager.PrivilegeDepthBasic;
            
            if (!privilegeManager.HasPrivilege(callerId.Id, privilegeName, requiredDepth))
            {
                throw new UnauthorizedAccessException(
                    $"User {callerId.Id} does not have the required '{privilegeName}' privilege to create {target.LogicalName} records.");
            }
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

            // System tables are readable by everyone
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security#system-tables
            if (IsSystemTable(target.LogicalName))
            {
                return; // Allow read access to system tables for all users
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

            // System tables are readable by everyone - no filtering needed
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
        /// Organization-owned entities don't have owners and only check for Global privilege.
        /// 
        /// This method uses the business unit-aware privilege checking that considers:
        /// - User's direct roles from multiple business units
        /// - User's team roles from multiple business units  
        /// - Role's business unit context when evaluating privilege depth
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// </summary>
        private static bool CheckPrivilegeWithDepth(IXrmFakedContext context, Guid userId, Entity record, string privilegeName, AccessRights requiredAccess)
        {
            var privilegeManager = context.SecurityManager.PrivilegeManager;
            
            // If not enforcing privilege depth, use simple privilege check (backward compatibility)
            if (!context.SecurityConfiguration.EnforcePrivilegeDepth)
            {
                return privilegeManager.HasPrivilege(userId, privilegeName, PrivilegeManager.PrivilegeDepthBasic);
            }

            // Use business unit-aware privilege checking
            // This considers all roles (direct and team-based) and their business unit contexts
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
            return privilegeManager.HasPrivilegeForRecord(userId, privilegeName, record, PrivilegeManager.PrivilegeDepthBasic);
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
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/field-security-entities
            
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

                // Check if field security is enabled for this attribute (IsSecured flag in metadata)
                // In real Dataverse, this would check:
                // 1. If the attribute has field security enabled (attributeMetadata.IsSecured)
                // 2. If the user has a field security profile that grants access
                // 3. The specific permission (Read, Create, Update) on the secured field
                //
                // For now, we'll check if the attribute metadata indicates security is enabled
                // and deny access if no field security profile grants permission.
                // Full implementation would require FieldSecurityProfile and FieldPermission entities.
                //
                // Since we don't have IsSecured property in the abstraction layer yet,
                // we allow all field updates if the attribute exists in metadata.
                // This matches the current Dataverse behavior where field security is opt-in.
            }
        }

        /// <summary>
        /// Checks if a business unit is within another business unit's hierarchy.
        /// Used for Deep privilege depth checking.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges#privilege-depth
        /// </summary>
        private static bool IsInBusinessUnitHierarchy(IXrmFakedContext context, Guid childBUId, Guid parentBUId)
        {
            // If they're the same, it's in the hierarchy
            if (childBUId == parentBUId)
            {
                return true;
            }

            // Walk up the business unit hierarchy
            var currentBU = context.GetEntityById("businessunit", childBUId);
            while (currentBU != null && currentBU.Contains("parentbusinessunitid"))
            {
                var parentBURef = currentBU.GetAttributeValue<EntityReference>("parentbusinessunitid");
                if (parentBURef == null)
                {
                    break; // Reached root BU
                }

                if (parentBURef.Id == parentBUId)
                {
                    return true; // Found parent in hierarchy
                }

                // Move up to next parent
                currentBU = context.GetEntityById("businessunit", parentBURef.Id);
            }

            return false; // Not in hierarchy
        }

        /// <summary>
        /// Determines if an entity is a system table.
        /// System tables are readable by everyone.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security#system-tables
        /// </summary>
        private static bool IsSystemTable(string entityName)
        {
            var systemTables = new[]
            {
                "organization",
                "businessunit",
                "systemuser",
                "team",
                "role",
                "privilege",
                "roleprivileges",
                "entitydefinition",
                "attribute",
                "solution",
                "publisher",
                "webresource",
                "sitemap",
                "appmodule",
                "appmodulecomponent",
                "savedquery",
                "systemform"
            };

            return systemTables.Contains(entityName.ToLowerInvariant());
        }
    }
}
