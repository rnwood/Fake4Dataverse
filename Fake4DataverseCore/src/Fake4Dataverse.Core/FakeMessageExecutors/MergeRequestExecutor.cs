using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class MergeRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is MergeRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var mergeRequest = (MergeRequest)request;

            // Validate required parameters per Microsoft documentation
            // Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            if (mergeRequest.Target == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("Cannot merge without a target entity reference.");
            }

            if (mergeRequest.SubordinateId == Guid.Empty)
            {
                throw FakeOrganizationServiceFaultFactory.New("Cannot merge without a subordinate entity ID.");
            }

            // UpdateContent is required per Microsoft documentation
            if (mergeRequest.UpdateContent == null)
            {
                throw FakeOrganizationServiceFaultFactory.New("UpdateContent is required for merge operation.");
            }

            var service = ctx.GetOrganizationService();
            var target = mergeRequest.Target;
            var subordinateId = mergeRequest.SubordinateId;
            var updateContent = mergeRequest.UpdateContent;

            // Verify both entities exist
            if (!ctx.ContainsEntity(target.LogicalName, target.Id))
            {
                throw FakeOrganizationServiceFaultFactory.New($"Target entity {target.LogicalName} with id {target.Id} not found.");
            }

            if (!ctx.ContainsEntity(target.LogicalName, subordinateId))
            {
                throw FakeOrganizationServiceFaultFactory.New($"Subordinate entity {target.LogicalName} with id {subordinateId} not found.");
            }

            // Cannot merge an entity with itself
            if (target.Id == subordinateId)
            {
                throw FakeOrganizationServiceFaultFactory.New("Cannot merge an entity with itself.");
            }

            // Retrieve both entities to check parenting if needed
            var targetEntity = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));
            var subordinateEntity = service.Retrieve(target.LogicalName, subordinateId, new ColumnSet(true));

            // Perform parenting checks if requested
            // Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest.performparentingchecks
            if (mergeRequest.PerformParentingChecks)
            {
                PerformParentingValidation(targetEntity, subordinateEntity);
            }

            // Apply UpdateContent to target (required per Microsoft documentation)
            var targetUpdate = new Entity(target.LogicalName)
            {
                Id = target.Id
            };

            foreach (var attr in updateContent.Attributes)
            {
                targetUpdate[attr.Key] = attr.Value;
            }

            service.Update(targetUpdate);

            // Update all references pointing to the subordinate entity to point to the target
            UpdateReferences(ctx, target.LogicalName, subordinateId, target.Id);

            // Deactivate the subordinate entity (not delete) per Microsoft documentation
            // Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest
            // "The subordinate record is deactivated"
            var deactivateSubordinate = new Entity(target.LogicalName)
            {
                Id = subordinateId
            };
            deactivateSubordinate["statecode"] = new OptionSetValue(1); // Inactive
            deactivateSubordinate["statuscode"] = new OptionSetValue(2); // Inactive

            service.Update(deactivateSubordinate);

            return new MergeResponse();
        }

        public Type GetResponsibleRequestType()
        {
            return typeof(MergeRequest);
        }

        /// <summary>
        /// Validates parent records match when PerformParentingChecks is enabled
        /// Source: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.mergerequest.performparentingchecks
        /// </summary>
        private void PerformParentingValidation(Entity targetEntity, Entity subordinateEntity)
        {
            // Common parent attributes to check based on entity type
            var parentAttributes = new[] { "parentaccountid", "parentcustomerid", "masterid" };

            foreach (var parentAttr in parentAttributes)
            {
                if (targetEntity.Contains(parentAttr) || subordinateEntity.Contains(parentAttr))
                {
                    var targetParent = targetEntity.GetAttributeValue<EntityReference>(parentAttr);
                    var subordinateParent = subordinateEntity.GetAttributeValue<EntityReference>(parentAttr);

                    // If both have parent values and they differ, throw error
                    if (targetParent != null && subordinateParent != null)
                    {
                        if (targetParent.Id != subordinateParent.Id || targetParent.LogicalName != subordinateParent.LogicalName)
                        {
                            throw FakeOrganizationServiceFaultFactory.New(
                                $"Cannot merge records with different parent records when PerformParentingChecks is enabled. " +
                                $"Target parent: {targetParent.LogicalName}:{targetParent.Id}, " +
                                $"Subordinate parent: {subordinateParent.LogicalName}:{subordinateParent.Id}");
                        }
                    }
                }
            }
        }

        private void UpdateReferences(IXrmFakedContext ctx, string entityName, Guid fromId, Guid toId)
        {
            var service = ctx.GetOrganizationService();
            
            // Access the internal data structure to iterate over all entities
            var xrmCtx = ctx as XrmFakedContext;
            if (xrmCtx == null || xrmCtx.Data == null)
            {
                return;
            }

            // Collect all entities that need updates to avoid modification during iteration
            var entitiesToUpdate = new List<Entity>();

            // Iterate through all entity types
            foreach (var entityType in xrmCtx.Data)
            {
                // Iterate through all entities of this type
                foreach (var entityEntry in entityType.Value)
                {
                    var entity = entityEntry.Value;
                    bool needsUpdate = false;
                    var entityToUpdate = new Entity(entity.LogicalName)
                    {
                        Id = entity.Id
                    };

                    // Check each attribute for references to the subordinate entity
                    foreach (var attribute in entity.Attributes)
                    {
                        if (attribute.Value is EntityReference entityRef)
                        {
                            if (entityRef.LogicalName == entityName && entityRef.Id == fromId)
                            {
                                // Update the reference to point to the target
                                entityToUpdate[attribute.Key] = new EntityReference(entityName, toId);
                                needsUpdate = true;
                            }
                        }
                    }

                    if (needsUpdate)
                    {
                        entitiesToUpdate.Add(entityToUpdate);
                    }
                }
            }

            // Now update all collected entities
            foreach (var entityToUpdate in entitiesToUpdate)
            {
                service.Update(entityToUpdate);
            }
        }
    }
}
