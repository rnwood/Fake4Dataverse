using FakeItEasy;
using Fake4Dataverse.Extensions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk.Client;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Integrity;

namespace Fake4Dataverse
{
    public partial class XrmFakedContext : IXrmFakedContext
    {
        protected const int EntityActiveStateCode = 0;
        protected const int EntityInactiveStateCode = 1;

        #region CRUD
        public Guid GetRecordUniqueId(EntityReference record, bool validate = true)
        {
            if (string.IsNullOrWhiteSpace(record.LogicalName))
            {
                throw new InvalidOperationException("The entity logical name must not be null or empty.");
            }

            // Don't fail with invalid operation exception, if no record of this entity exists, but entity is known
            if (!Data.ContainsKey(record.LogicalName) && !EntityMetadata.ContainsKey(record.LogicalName))
            {
                if (ProxyTypesAssembly == null)
                {
                    throw new InvalidOperationException($"The entity logical name {record.LogicalName} is not valid.");
                }

                if (!ProxyTypesAssembly.GetTypes().Any(type => FindReflectedType(record.LogicalName) != null))
                {
                    throw new InvalidOperationException($"The entity logical name {record.LogicalName} is not valid.");
                }
            }

            if (record.Id == Guid.Empty && record.HasKeyAttributes())
            {
                if (EntityMetadata.ContainsKey(record.LogicalName))
                {
                    var entityMetadata = EntityMetadata[record.LogicalName];
                    foreach (var key in entityMetadata.Keys)
                    {
                        if (record.KeyAttributes.Keys.Count == key.KeyAttributes.Length && key.KeyAttributes.All(x => record.KeyAttributes.Keys.Contains(x)))
                        {
                            if (Data.TryGetValue(record.LogicalName, out var entityCollection))
                            {
                                var matchedRecord = entityCollection.Values.SingleOrDefault(x => record.KeyAttributes.All(k => x.Attributes.ContainsKey(k.Key) && x.Attributes[k.Key] != null && x.Attributes[k.Key].Equals(k.Value)));
                                if (matchedRecord != null)
                                {
                                    return matchedRecord.Id;
                                }
                            }
                            if (validate)
                            {
                                throw new FaultException<OrganizationServiceFault>(new OrganizationServiceFault() { Message = $"{record.LogicalName} with the specified Alternate Keys Does Not Exist"});
                            }
                        }
                    }
                }
                if (validate)
                {
                    throw new InvalidOperationException($"The requested key attributes do not exist for the entity {record.LogicalName}");
                }
            }
            /*
            if (validate && record.Id == Guid.Empty)
            {
                throw new InvalidOperationException("The id must not be empty.");
            }
            */
            
            return record.Id;
        }   
        
        /// <summary>
        /// Fakes the Create message
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fakedService"></param>
        protected static void FakeCreate(XrmFakedContext context, IOrganizationService fakedService)
        {
            A.CallTo(() => fakedService.Create(A<Entity>._))
                .ReturnsLazily((Entity e) =>
                {
                    return context.CreateEntity(e);
                });
        }

        public void UpdateEntity(Entity e)
        {
            if (e == null)
            {
                throw new InvalidOperationException("The entity must not be null");
            }
            e = e.Clone(e.GetType());
            var reference = e.ToEntityReferenceWithKeyAttributes();
            e.Id = GetRecordUniqueId(reference);

            // Validate attribute types if enabled
            var integrityOptions = GetProperty<IIntegrityOptions>();
            if (integrityOptions.ValidateAttributeTypes)
            {
                ValidateAttributeTypes(e);
            }

            // Thread-safe update with per-entity-type locking for better concurrency
            var entityLock = _entityLocks.GetOrAdd(e.LogicalName, _ => new object());
            lock (entityLock)
            {
                // Update specific validations: The entity record must exist in the context
                if (Data.TryGetValue(e.LogicalName, out var entityCollection) &&
                    entityCollection.ContainsKey(e.Id))
                {
                // Track modified attributes for filtering
                var modifiedAttributes = new HashSet<string>(e.Attributes.Keys, StringComparer.OrdinalIgnoreCase);
                
                // Execute business rules before PreValidation
                // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
                // "Business rules execute when records are updated to validate data and set field values"
                var businessRuleResult = this.BusinessRuleExecutor.ExecuteRules(e, BusinessRules.BusinessRuleTrigger.OnUpdate, isServerSide: true);
                
                // If business rules generated errors, throw validation exception
                if (businessRuleResult.HasErrors)
                {
                    var errorMessages = string.Join("; ", businessRuleResult.Errors.Select(err => 
                        string.IsNullOrEmpty(err.FieldName) ? err.Message : $"{err.FieldName}: {err.Message}"));
                    var fullMessage = $"Business rule validation failed: {errorMessages}";
                    
                    var fault = new Microsoft.Xrm.Sdk.OrganizationServiceFault
                    {
                        ErrorCode = (int)Fake4Dataverse.Abstractions.ErrorCodes.BusinessRuleEditorSupportsOnlyIfConditionBranch,
                        Message = fullMessage
                    };
                    
                    throw new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
                        fault,
                        new System.ServiceModel.FaultReason(fullMessage));
                }
                
                if (this.UsePipelineSimulation)
                {
                    // Execute PreValidation stage (outside transaction)
                    PluginPipelineSimulator.ExecutePipelineStage(
                        "Update",
                        e.LogicalName,
                        Abstractions.Plugins.Enums.ProcessingStepStage.Prevalidation,
                        e,
                        modifiedAttributes,
                        userId: CallerProperties.CallerId.Id,
                        organizationId: Guid.NewGuid());
                    
                    // Execute PreOperation stage (inside transaction)
                    PluginPipelineSimulator.ExecutePipelineStage(
                        "Update",
                        e.LogicalName,
                        Abstractions.Plugins.Enums.ProcessingStepStage.Preoperation,
                        e,
                        modifiedAttributes,
                        userId: CallerProperties.CallerId.Id,
                        organizationId: Guid.NewGuid());
                }

                // Capture old entity state for audit tracking
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
                var oldEntityForAudit = entityCollection[e.Id].Clone(entityCollection[e.Id].GetType(), this);

                // Add as many attributes to the entity as the ones received (this will keep existing ones)
                var cachedEntity = entityCollection[e.Id];
                foreach (var sAttributeName in e.Attributes.Keys.ToList())
                {
                    var attribute = e[sAttributeName];
                    if (attribute == null)
                    {
                        cachedEntity.Attributes.Remove(sAttributeName);
                    }
                    else if (attribute is DateTime)
                    {
                        cachedEntity[sAttributeName] = ConvertToUtc((DateTime)e[sAttributeName]);
                    }
                    else
                    {
                        if (attribute is EntityReference && integrityOptions.ValidateEntityReferences)
                        {
                            var target = (EntityReference)e[sAttributeName];
                            attribute = ResolveEntityReference(target);
                        }
                        cachedEntity[sAttributeName] = attribute;
                    }
                }

                // Update ModifiedOn
                cachedEntity["modifiedon"] = DateTime.UtcNow;
                cachedEntity["modifiedby"] = CallerId;

                // Evaluate calculated fields after update
                // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
                // "Calculated columns are calculated in real-time when they are retrieved"
                EvaluateCalculatedFieldsForEntity(cachedEntity);

                // Trigger rollup recalculation for related entities
                // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
                // "When you create, update, or delete a record, the rollup columns on related records are recalculated"
                TriggerRollupRecalculationForRelatedEntities(cachedEntity);

                // Record audit entry for Update operation
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
                RecordUpdateAudit(oldEntityForAudit, cachedEntity);

                if (this.UsePipelineSimulation)
                {
                    // Execute PostOperation stage (inside transaction)
                    PluginPipelineSimulator.ExecutePipelineStage(
                        "Update",
                        e.LogicalName,
                        Abstractions.Plugins.Enums.ProcessingStepStage.Postoperation,
                        e,
                        modifiedAttributes,
                        userId: CallerProperties.CallerId.Id,
                        organizationId: Guid.NewGuid());
                    
                    // Trigger Cloud Flows after PostOperation plugins
                    // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
                    // Cloud Flows trigger asynchronously after the operation completes
                    CloudFlowSimulator?.TriggerDataverseFlows("Update", e.LogicalName, cachedEntity, modifiedAttributes);
                }
                }
                else
                {
                    // The entity record was not found, return a CRM-ish update error message
                    throw FakeOrganizationServiceFaultFactory.New($"{e.LogicalName} with Id {e.Id} Does Not Exist");
                }
            }
        }

        public Entity GetEntityById(string sLogicalName, Guid id)
        {
            var entityLock = _entityLocks.GetOrAdd(sLogicalName, _ => new object());
            lock (entityLock)
            {
                if(!Data.TryGetValue(sLogicalName, out var entityCollection)) 
                {
                    throw new InvalidOperationException($"The entity logical name '{sLogicalName}' is not valid.");
                }

                if(!entityCollection.TryGetValue(id, out var entity)) 
                {
                    throw new InvalidOperationException($"The id parameter '{id.ToString()}' for entity logical name '{sLogicalName}' is not valid.");
                }

                return entity;
            }
        }

        public bool ContainsEntity(string sLogicalName, Guid id)
        {
            var entityLock = _entityLocks.GetOrAdd(sLogicalName, _ => new object());
            lock (entityLock)
            {
                if(!Data.TryGetValue(sLogicalName, out var entityCollection)) 
                {
                    return false;
                }

                return entityCollection.ContainsKey(id);
            }
        }

        public T GetEntityById<T>(Guid id) where T: Entity
        {
            var typeParameter = typeof(T);

            var logicalName = "";

            if (typeParameter.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true).Length > 0)
            {
                logicalName = (typeParameter.GetCustomAttributes(typeof(EntityLogicalNameAttribute), true)[0] as EntityLogicalNameAttribute).LogicalName;
            }

            return GetEntityById(logicalName, id) as T;
        }

        protected EntityReference ResolveEntityReference(EntityReference er)
        {
            if (!Data.TryGetValue(er.LogicalName, out var entityCollection) || !entityCollection.ContainsKey(er.Id))
            {
                if (er.Id == Guid.Empty && er.HasKeyAttributes())
                {
                    return ResolveEntityReferenceByAlternateKeys(er);
                }
                else
                {
                    throw FakeOrganizationServiceFaultFactory.New($"{er.LogicalName} With Id = {er.Id:D} Does Not Exist");
                }
            }
            return er;
        }

        protected EntityReference ResolveEntityReferenceByAlternateKeys(EntityReference er)
        {
            var resolvedId = GetRecordUniqueId(er);

            return new EntityReference()
            {
                LogicalName = er.LogicalName,
                Id = resolvedId
            };
        }
        /// <summary>
        /// Fakes the delete method. Very similar to the Retrieve one
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fakedService"></param>
        

        public void DeleteEntity(EntityReference er)
        {
            // Thread-safe delete with per-entity-type locking
            var entityLock = _entityLocks.GetOrAdd(er.LogicalName, _ => new object());
            lock (entityLock)
            {
                // Don't fail with invalid operation exception, if no record of this entity exists, but entity is known
                if (!this.Data.ContainsKey(er.LogicalName))
                {
                    if (ProxyTypesAssemblies.Count() == 0)
                    {
                        throw new InvalidOperationException($"The entity logical name {er.LogicalName} is not valid.");
                    }

                    if (FindReflectedType(er.LogicalName) == null)
                    {
                        throw new InvalidOperationException($"The entity logical name {er.LogicalName} is not valid.");
                    }
                }

                // Entity logical name exists, so , check if the requested entity exists
                if (this.Data.TryGetValue(er.LogicalName, out var entityCollection) && 
                    entityCollection != null &&
                    entityCollection.TryGetValue(er.Id, out var entityToDelete))
                {
                if (this.UsePipelineSimulation)
                {
                    // Execute PreValidation stage (outside transaction)
                    PluginPipelineSimulator.ExecutePipelineStage(
                        "Delete",
                        er.LogicalName,
                        Abstractions.Plugins.Enums.ProcessingStepStage.Prevalidation,
                        entityToDelete,
                        userId: CallerProperties.CallerId.Id,
                        organizationId: Guid.NewGuid());
                    
                    // Execute PreOperation stage (inside transaction)
                    PluginPipelineSimulator.ExecutePipelineStage(
                        "Delete",
                        er.LogicalName,
                        Abstractions.Plugins.Enums.ProcessingStepStage.Preoperation,
                        entityToDelete,
                        userId: CallerProperties.CallerId.Id,
                        organizationId: Guid.NewGuid());
                }

                // Entity found => delete it (Main Operation)
                entityCollection.Remove(er.Id);

                // Trigger rollup recalculation for related entities after deletion
                // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
                // "When you create, update, or delete a record, the rollup columns on related records are recalculated"
                TriggerRollupRecalculationForRelatedEntities(entityToDelete);

                // Record audit entry for Delete operation
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
                RecordDeleteAudit(er);

                if (this.UsePipelineSimulation)
                {
                    // Execute PostOperation stage (inside transaction)
                    PluginPipelineSimulator.ExecutePipelineStage(
                        "Delete",
                        er.LogicalName,
                        Abstractions.Plugins.Enums.ProcessingStepStage.Postoperation,
                        entityToDelete,
                        userId: CallerProperties.CallerId.Id,
                        organizationId: Guid.NewGuid());
                    
                    // Trigger Cloud Flows after PostOperation plugins
                    // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
                    // Cloud Flows trigger asynchronously after the operation completes
                    CloudFlowSimulator?.TriggerDataverseFlows("Delete", er.LogicalName, entityToDelete);
                }
                }
                else
                {
                    // Entity not found in the context => throw not found exception
                    // The entity record was not found, return a CRM-ish update error message
                    throw FakeOrganizationServiceFaultFactory.New($"{er.LogicalName} with Id {er.Id} Does Not Exist");
                }
            }
        }
        #endregion

        #region Other protected methods
        

        public void AddEntityDefaultAttributes(Entity e)
        {
            // Add createdon, modifiedon, createdby, modifiedby properties
            if (CallerId == null)
            {
                CallerId = new EntityReference("systemuser", Guid.NewGuid()); // Create a new instance by default

                var integrityOptions = GetProperty<IIntegrityOptions>();

                if (integrityOptions.ValidateEntityReferences)
                {
                    var systemUserLock = _entityLocks.GetOrAdd("systemuser", _ => new object());
                    lock (systemUserLock)
                    {
                        var systemUserCollection = Data.GetOrAdd("systemuser", _ => new Dictionary<Guid, Entity>());
                        if (!systemUserCollection.ContainsKey(CallerId.Id))
                        {
                            systemUserCollection.Add(CallerId.Id, new Entity("systemuser") { Id = CallerId.Id });
                        }
                    }
                }

            }

            var isManyToManyRelationshipEntity = e.LogicalName != null && this._relationships.ContainsKey(e.LogicalName);

            EntityInitializerService.Initialize(e, CallerId.Id, this, isManyToManyRelationshipEntity);
        }

        protected void ValidateEntity(Entity e)
        {
            if (e == null)
            {
                throw new InvalidOperationException("The entity must not be null");
            }

            // Validate the entity
            if (string.IsNullOrWhiteSpace(e.LogicalName))
            {
                throw new InvalidOperationException("The LogicalName property must not be empty");
            }

            if (e.Id == Guid.Empty)
            {
                throw new InvalidOperationException("The Id property must not be empty");
            }
        }

        /// <summary>
        /// Validates that attribute values match their metadata types and that lookup targets are valid.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-attribute-metadata
        /// Dataverse validates attribute types at runtime and throws exceptions for type mismatches.
        /// </summary>
        protected void ValidateAttributeTypes(Entity e)
        {
            if (e == null || e.Attributes == null || e.Attributes.Count == 0)
            {
                return;
            }

            // Check if metadata is available for this entity
            var entityMetadata = GetEntityMetadataByName(e.LogicalName);
            if (entityMetadata == null)
            {
                // Replicate Dataverse behavior - entity must exist in metadata
                var fault = new Microsoft.Xrm.Sdk.OrganizationServiceFault
                {
                    Message = $"Could not find entity '{e.LogicalName}' in metadata. Entity metadata must be initialized before validation can occur."
                };
                throw new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
                    fault,
                    new System.ServiceModel.FaultReason(fault.Message));
            }

            if (entityMetadata.Attributes == null)
            {
                // If no attributes defined, nothing to validate
                return;
            }

            // System attributes that are automatically added by the framework
            var systemAttributes = new[] { "createdby", "createdon", "modifiedby", "modifiedon", "ownerid", 
                                          "statecode", "statuscode", "createdonbehalfby", "modifiedonbehalfby",
                                          $"{e.LogicalName}id" };  // Primary key attribute

            foreach (var attributeName in e.Attributes.Keys.ToList())
            {
                var attributeValue = e[attributeName];
                
                // Skip null values - they are always valid
                if (attributeValue == null)
                {
                    continue;
                }

                // Skip system attributes that may not be in metadata
                if (systemAttributes.Contains(attributeName.ToLower()))
                {
                    continue;
                }

                // Find the attribute metadata
                var attributeMetadata = entityMetadata.Attributes
                    .FirstOrDefault(a => a.LogicalName == attributeName);

                if (attributeMetadata == null)
                {
                    // Attribute doesn't exist in metadata
                    var fault = new Microsoft.Xrm.Sdk.OrganizationServiceFault
                    {
                        Message = $"The attribute '{attributeName}' does not exist on entity '{e.LogicalName}'."
                    };
                    throw new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
                        fault,
                        new System.ServiceModel.FaultReason(fault.Message));
                }

                if (attributeMetadata.AttributeType == null)
                {
                    // Can't validate without type information
                    continue;
                }

                // Validate the type matches
                var expectedType = this.FindAttributeTypeInInjectedMetadata(e.LogicalName, attributeName);
                if (expectedType == null)
                {
                    continue;
                }

                var actualValue = e[attributeName];
                var actualType = actualValue?.GetType();

                if (actualType == null)
                {
                    continue;
                }

                // Handle special type validations
                bool isValid = false;

                // Allow AliasedValue to pass through - it's used in queries
                if (actualType == typeof(Microsoft.Xrm.Sdk.AliasedValue))
                {
                    isValid = true;
                }
                // Check if actual type matches expected type or is assignable to it
                else if (expectedType.IsAssignableFrom(actualType))
                {
                    isValid = true;
                }
                // Handle numeric type conversions (Int32 vs Int64, Decimal vs Double, etc.)
                else if (IsNumericType(expectedType) && IsNumericType(actualType))
                {
                    isValid = true;
                }
                // Handle EntityReference types - check for valid lookup targets
                else if (expectedType == typeof(EntityReference) && actualType == typeof(EntityReference))
                {
                    var lookupMetadata = attributeMetadata as Microsoft.Xrm.Sdk.Metadata.LookupAttributeMetadata;
                    if (lookupMetadata != null && lookupMetadata.Targets != null && lookupMetadata.Targets.Length > 0)
                    {
                        var entityRef = (EntityReference)actualValue;
                        if (!lookupMetadata.Targets.Contains(entityRef.LogicalName))
                        {
                            var fault = new Microsoft.Xrm.Sdk.OrganizationServiceFault
                            {
                                Message = $"The lookup attribute '{attributeName}' on entity '{e.LogicalName}' cannot reference entity type '{entityRef.LogicalName}'. Valid targets are: {string.Join(", ", lookupMetadata.Targets)}."
                            };
                            throw new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
                                fault,
                                new System.ServiceModel.FaultReason(fault.Message));
                        }
                    }
                    isValid = true;
                }

                if (!isValid)
                {
                    var fault = new Microsoft.Xrm.Sdk.OrganizationServiceFault
                    {
                        Message = $"The attribute '{attributeName}' on entity '{e.LogicalName}' has an invalid type. Expected: {expectedType.Name}, but got: {actualType.Name}."
                    };
                    throw new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
                        fault,
                        new System.ServiceModel.FaultReason(fault.Message));
                }
            }
        }

        /// <summary>
        /// Checks if a type is a numeric type for validation purposes
        /// </summary>
        private static bool IsNumericType(Type type)
        {
            return type == typeof(int) || type == typeof(long) || type == typeof(decimal) || 
                   type == typeof(double) || type == typeof(float) || type == typeof(short) ||
                   type == typeof(byte) || type == typeof(uint) || type == typeof(ulong) ||
                   type == typeof(ushort) || type == typeof(sbyte);
        }

        public Guid CreateEntity(Entity e)
        {
            if (e == null)
            {
                throw new InvalidOperationException("The entity must not be null");
            }

            var clone = e.Clone(e.GetType());

            if (clone.Id == Guid.Empty)
            {
                clone.Id = Guid.NewGuid(); // Add default guid if none present
            }

            // Hack for Dynamic Entities where the Id property doesn't populate the "entitynameid" primary key
            var primaryKeyAttribute = $"{e.LogicalName}id";
            if (!clone.Attributes.ContainsKey(primaryKeyAttribute))
            {
                clone[primaryKeyAttribute] = clone.Id;
            }

            ValidateEntity(clone);

            // Create specific validations
            if (clone.Id != Guid.Empty && 
                Data.TryGetValue(clone.LogicalName, out var existingCollection) &&
                existingCollection.ContainsKey(clone.Id))
            {
                throw new InvalidOperationException($"There is already a record of entity {clone.LogicalName} with id {clone.Id}, can't create with this Id.");
            }

            // Create specific validations
            if (clone.Attributes.ContainsKey("statecode"))
            {
                throw new InvalidOperationException($"When creating an entity with logical name '{clone.LogicalName}', or any other entity, it is not possible to create records with the statecode property. Statecode must be set after creation.");
            }

            AddEntityWithDefaults(clone, false, this.UsePipelineSimulation);

            if (e.RelatedEntities.Count > 0)
            {
                foreach (var relationshipSet in e.RelatedEntities)
                {
                    var relationship = relationshipSet.Key;

                    var entityReferenceCollection = new EntityReferenceCollection();

                    foreach (var relatedEntity in relationshipSet.Value.Entities)
                    {
                        var relatedId = CreateEntity(relatedEntity);
                        entityReferenceCollection.Add(new EntityReference(relatedEntity.LogicalName, relatedId));
                    }

                    var messageExecutors = GetProperty<MessageExecutors>();
                    if(messageExecutors == null) 
                    {
                        throw PullRequestException.NotImplementedOrganizationRequest(typeof(AssociateRequest));
                    }
                    else 
                    {
                        var request = new AssociateRequest
                        {
                            Target = clone.ToEntityReference(),
                            Relationship = relationship,
                            RelatedEntities = entityReferenceCollection
                        };
                        _service.Execute(request);
                    }
                }
            }

            return clone.Id;
        }

        public void AddEntityWithDefaults(Entity e, bool clone = false, bool usePluginPipeline = false, bool skipValidation = false)
        {
            // Create the entity with defaults
            AddEntityDefaultAttributes(e);

            // Execute business rules before PreValidation
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
            // "Business rules execute before the record is saved to validate data and set field values"
            var businessRuleResult = this.BusinessRuleExecutor.ExecuteRules(e, BusinessRules.BusinessRuleTrigger.OnCreate, isServerSide: true);
            
            // If business rules generated errors, throw validation exception
            if (businessRuleResult.HasErrors)
            {
                var errorMessages = string.Join("; ", businessRuleResult.Errors.Select(err => 
                    string.IsNullOrEmpty(err.FieldName) ? err.Message : $"{err.FieldName}: {err.Message}"));
                var fullMessage = $"Business rule validation failed: {errorMessages}";
                
                var fault = new Microsoft.Xrm.Sdk.OrganizationServiceFault
                {
                    ErrorCode = (int)Fake4Dataverse.Abstractions.ErrorCodes.BusinessRuleEditorSupportsOnlyIfConditionBranch,
                    Message = fullMessage
                };
                
                throw new System.ServiceModel.FaultException<Microsoft.Xrm.Sdk.OrganizationServiceFault>(
                    fault,
                    new System.ServiceModel.FaultReason(fullMessage));
            }

            if (usePluginPipeline)
            {
                // Execute PreValidation stage (outside transaction)
                PluginPipelineSimulator.ExecutePipelineStage(
                    "Create",
                    e.LogicalName,
                    Abstractions.Plugins.Enums.ProcessingStepStage.Prevalidation,
                    e,
                    userId: CallerProperties.CallerId.Id,
                    organizationId: Guid.NewGuid());
                
                // Execute PreOperation stage (inside transaction)
                PluginPipelineSimulator.ExecutePipelineStage(
                    "Create",
                    e.LogicalName,
                    Abstractions.Plugins.Enums.ProcessingStepStage.Preoperation,
                    e,
                    userId: CallerProperties.CallerId.Id,
                    organizationId: Guid.NewGuid());
            }

            // Store (Main Operation)
            AddEntity(clone ? e.Clone(e.GetType()) : e, skipValidation);

            // Trigger rollup recalculation for related entities after creation
            // Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
            // "When you create, update, or delete a record, the rollup columns on related records are recalculated"
            TriggerRollupRecalculationForRelatedEntities(e);

            if (usePluginPipeline)
            {
                // Execute PostOperation stage (inside transaction)
                PluginPipelineSimulator.ExecutePipelineStage(
                    "Create",
                    e.LogicalName,
                    Abstractions.Plugins.Enums.ProcessingStepStage.Postoperation,
                    e,
                    userId: CallerProperties.CallerId.Id,
                    organizationId: Guid.NewGuid());
                
                // Trigger Cloud Flows after PostOperation plugins
                // Reference: https://learn.microsoft.com/en-us/power-automate/dataverse/create-update-delete-trigger
                // Cloud Flows trigger asynchronously after the operation completes
                CloudFlowSimulator?.TriggerDataverseFlows("Create", e.LogicalName, e);
            }

            // Record audit entry for Create operation
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
            // Auditing tracks Create, Update, Delete operations and attribute changes
            RecordCreateAudit(e);
        }

        public void AddEntity(Entity e, bool skipValidation = false)
        {
            //Automatically detect proxy types assembly if an early bound type was used.
            if (ProxyTypesAssemblies.Count() == 0 &&
                e.GetType().IsSubclassOf(typeof(Entity)))
            {
                EnableProxyTypes(Assembly.GetAssembly(e.GetType()));
            }

            ValidateEntity(e); //Entity must have a logical name and an Id

            var integrityOptions = GetProperty<IIntegrityOptions>();

            // Validate attribute types if enabled and not skipping validation
            if (!skipValidation && integrityOptions.ValidateAttributeTypes)
            {
                ValidateAttributeTypes(e);
            }

            foreach (var sAttributeName in e.Attributes.Keys.ToList())
            {
                var attribute = e[sAttributeName];
                if (attribute is DateTime)
                {
                    e[sAttributeName] = ConvertToUtc((DateTime)e[sAttributeName]);
                }
                if (attribute is EntityReference && integrityOptions.ValidateEntityReferences)
                {
                    var target = (EntityReference)e[sAttributeName];
                    e[sAttributeName] = ResolveEntityReference(target);
                }
            }

            // Thread-safe add with per-entity-type locking
            var entityLock = _entityLocks.GetOrAdd(e.LogicalName, _ => new object());
            lock (entityLock)
            {
                //Add the entity collection - GetOrAdd is thread-safe
                var entityCollection = Data.GetOrAdd(e.LogicalName, _ => new Dictionary<Guid, Entity>());

                if (entityCollection.ContainsKey(e.Id))
                {
                    entityCollection[e.Id] = e;
                }
                else
                {
                    entityCollection.Add(e.Id, e);
                }

                //Update metadata for that entity
                if (!AttributeMetadataNames.ContainsKey(e.LogicalName))
                    AttributeMetadataNames.Add(e.LogicalName, new Dictionary<string, string>());

                //Update attribute metadata
                if (ProxyTypesAssemblies.Count() > 0)
                {
                    //If the context is using a proxy types assembly then we can just guess the metadata from the generated attributes
                    var type = FindReflectedType(e.LogicalName);
                    if (type != null)
                    {
                        var props = type.GetProperties();
                        foreach (var p in props)
                        {
                            if (!AttributeMetadataNames[e.LogicalName].ContainsKey(p.Name))
                                AttributeMetadataNames[e.LogicalName].Add(p.Name, p.Name);
                        }
                    }
                    else
                        throw new Exception(string.Format("Couldnt find reflected type for {0}", e.LogicalName));

                }
                else
                {
                    //If dynamic entities are being used, then the only way of guessing if a property exists is just by checking
                    //if the entity has the attribute in the dictionary
                    foreach (var attKey in e.Attributes.Keys)
                    {
                        if (!AttributeMetadataNames[e.LogicalName].ContainsKey(attKey))
                            AttributeMetadataNames[e.LogicalName].Add(attKey, attKey);
                    }
                }
            }

        }

        protected internal DateTime ConvertToUtc(DateTime attribute)
        {
            return DateTime.SpecifyKind(attribute, DateTimeKind.Utc);
        }
        #endregion
    }
}
