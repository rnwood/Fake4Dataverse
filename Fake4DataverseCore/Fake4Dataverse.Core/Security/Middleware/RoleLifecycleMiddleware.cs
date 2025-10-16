using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;

namespace Fake4Dataverse.Security.Middleware
{
    /// <summary>
    /// Middleware that manages role lifecycle and business unit shadow copies.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/create-edit-business-units
    /// 
    /// Handles:
    /// - Creating shadow role copies when roles or BUs are created
    /// - Deleting shadow copies when roles or BUs are deleted
    /// - Validating role assignments
    /// - Removing role assignments when user/team BU changes
    /// </summary>
    public static class RoleLifecycleMiddleware
    {
        /// <summary>
        /// Adds role lifecycle middleware to the pipeline.
        /// This should be added before CRUD middleware to intercept entity operations.
        /// </summary>
        public static IMiddlewareBuilder AddRoleLifecycle(this IMiddlewareBuilder builder)
        {
            return builder.Use(next => (context, request) =>
            {
                // Handle different request types
                if (request is CreateRequest createRequest)
                {
                    // Let the create happen first
                    var response = next(context, request);
                    
                    // Then handle lifecycle events
                    HandleEntityCreated(context, createRequest.Target);
                    
                    return response;
                }
                else if (request is DeleteRequest deleteRequest)
                {
                    // Handle lifecycle before deletion
                    HandleEntityDeleting(context, deleteRequest.Target);
                    
                    // Then let the delete happen
                    return next(context, request);
                }
                else if (request is UpdateRequest updateRequest)
                {
                    // Check if businessunitid is being changed for systemuser or team
                    if ((updateRequest.Target.LogicalName == "systemuser" || updateRequest.Target.LogicalName == "team") &&
                        updateRequest.Target.Contains("businessunitid"))
                    {
                        // Handle BU change before update
                        HandleBusinessUnitChange(context, updateRequest.Target);
                    }
                    
                    return next(context, request);
                }
                else if (request is AssociateRequest associateRequest)
                {
                    // Validate role assignments
                    if (associateRequest.Relationship.SchemaName == "systemuserroles_association" ||
                        associateRequest.Relationship.SchemaName == "teamroles_association")
                    {
                        ValidateRoleAssignments(context, associateRequest);
                    }
                    
                    return next(context, request);
                }
                
                // For other requests, just pass through
                return next(context, request);
            });
        }

        private static void HandleEntityCreated(IXrmFakedContext context, Entity entity)
        {
            if (entity.LogicalName == "role")
            {
                context.SecurityManager.RoleLifecycleManager.OnRoleCreated(entity);
            }
            else if (entity.LogicalName == "businessunit")
            {
                context.SecurityManager.RoleLifecycleManager.OnBusinessUnitCreated(entity);
            }
        }

        private static void HandleEntityDeleting(IXrmFakedContext context, EntityReference target)
        {
            if (target.LogicalName == "role")
            {
                context.SecurityManager.RoleLifecycleManager.OnRoleDeleted(target.Id);
            }
            else if (target.LogicalName == "businessunit")
            {
                context.SecurityManager.RoleLifecycleManager.OnBusinessUnitDeleted(target.Id);
            }
        }

        private static void HandleBusinessUnitChange(IXrmFakedContext context, Entity entity)
        {
            // User or team business unit is changing - remove role assignments
            context.SecurityManager.RoleLifecycleManager.OnUserTeamBusinessUnitChanged(
                entity.LogicalName, 
                entity.Id);
        }

        private static void ValidateRoleAssignments(IXrmFakedContext context, AssociateRequest request)
        {
            // Determine principal type
            string principalType = request.Relationship.SchemaName == "systemuserroles_association" 
                ? "systemuser" 
                : "team";

            // Validate each role being assigned
            foreach (var relatedEntity in request.RelatedEntities)
            {
                if (relatedEntity.LogicalName == "role")
                {
                    context.SecurityManager.RoleLifecycleManager.ValidateRoleAssignment(
                        relatedEntity.Id,
                        principalType,
                        request.Target.Id);
                }
            }
        }
    }
}
