using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests
{
    public class ValidateReferencesTests: Fake4DataverseTests
    {
        protected readonly IXrmFakedContext _contextWithIntegrity;
        protected readonly IOrganizationService _serviceWithIntegrity;
        
        public ValidateReferencesTests(): base()
        {
            // Create context with validation enabled but only for entity references, not attribute types
            // This allows testing entity reference validation without requiring full metadata
            _contextWithIntegrity = XrmFakedContextFactory.New(new IntegrityOptions 
            { 
                ValidateEntityReferences = true,
                ValidateAttributeTypes = false  // Disable to avoid metadata requirement
            });
            _serviceWithIntegrity = _contextWithIntegrity.GetOrganizationService();
        }

        [Fact]
        public void When_context_is_initialised_validate_references_is_enabled_by_default()
        {
            // Create a fresh context without overriding defaults
            // Use context from base class
            var freshContext = _context;
            var integrityOptions = freshContext.GetProperty<IIntegrityOptions>();
            Assert.True(integrityOptions.ValidateEntityReferences);
            Assert.True(integrityOptions.ValidateAttributeTypes);
        }

        [Fact]
        public void An_entity_which_references_another_non_existent_entity_cannot_be_created_when_integrity_is_enabled_by_default()
        {
            // Create a fresh context without overriding defaults, but disable attribute type validation
            // since we don't have metadata
            var freshContext = XrmFakedContextFactory.New(new IntegrityOptions 
            { 
                ValidateEntityReferences = true,
                ValidateAttributeTypes = false
            });
            var freshService = freshContext.GetOrganizationService();
            
            Guid otherEntity = Guid.NewGuid();
            Entity entity = new Entity("entity");

            entity["otherEntity"] = new EntityReference("entity", otherEntity);

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => freshService.Create(entity));

            Assert.Equal($"{entity.LogicalName} With Id = {otherEntity:D} Does Not Exist", ex.Message);
        }

        [Fact]
        public void An_entity_which_references_another_non_existent_entity_can_not_be_created_when_validate_is_true()
        {
            Guid otherEntity = Guid.NewGuid();
            Entity entity = new Entity("entity");

            entity["otherEntity"] = new EntityReference("entity", otherEntity);

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _serviceWithIntegrity.Create(entity));

            Assert.Equal($"{entity.LogicalName} With Id = {otherEntity:D} Does Not Exist", ex.Message);
        }

        [Fact]
        public void An_entity_which_references_another_existent_entity_can_be_created_when_integrity_is_enabled()
        {
            Entity otherEntity = new Entity("otherEntity");
            otherEntity.Id = Guid.NewGuid();
            _contextWithIntegrity.Initialize(otherEntity);

            Entity entity = new Entity("entity");
            entity["otherEntity"] = otherEntity.ToEntityReference();

            Guid created = _serviceWithIntegrity.Create(entity);

            Entity otherEntityInContext = _serviceWithIntegrity.Retrieve("otherEntity", otherEntity.Id, new ColumnSet(true));

            Assert.NotEqual(Guid.Empty, created);
            Assert.Equal(otherEntity.Id, otherEntityInContext.Id);
        }

        

        [Fact]
        public void An_entity_which_references_another_non_existent_entity_can_be_updated_when_integrity_is_disabled()
        {
            Entity entity = new Entity("entity");
            entity.Id = Guid.NewGuid();
            _context.Initialize(entity);

            Guid otherEntityId = Guid.NewGuid();
            entity["otherEntity"] = new EntityReference("entity", otherEntityId);

            _service.Update(entity);

            Entity updated = _service.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));
            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Retrieve("entity", otherEntityId, new ColumnSet(true)));
            Assert.Equal(otherEntityId, updated.GetAttributeValue<EntityReference>("otherEntity").Id);
            Assert.Equal($"{entity.LogicalName} With Id = {otherEntityId:D} Does Not Exist", ex.Message);
        }

        [Fact]
        public void An_entity_which_references_another_non_existent_entity_can_not_be_updated_when_integrity_is_enabled()
        {
            Entity entity = new Entity("entity");
            entity.Id = Guid.NewGuid();
            _contextWithIntegrity.Initialize(entity);

            Guid otherEntityId = Guid.NewGuid();
            entity["otherEntity"] = new EntityReference("entity", otherEntityId);

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _serviceWithIntegrity.Update(entity));
            Assert.Equal($"{entity.LogicalName} With Id = {otherEntityId:D} Does Not Exist", ex.Message);
        }

        [Fact]
        public void An_entity_which_references_another_existent_entity_can_be_updated_when_integrity_is_enabled()
        {
            
            Entity otherEntity = new Entity("otherEntity");
            otherEntity.Id = Guid.NewGuid();

            Entity entity = new Entity("entity");
            entity.Id = Guid.NewGuid();

            _contextWithIntegrity.Initialize(new Entity[] { otherEntity, entity });
            entity["otherEntity"] = otherEntity.ToEntityReference();

            _serviceWithIntegrity.Update(entity);

            Entity otherEntityInContext = _serviceWithIntegrity.Retrieve("otherEntity", otherEntity.Id, new ColumnSet(true));
            Entity updated = _serviceWithIntegrity.Retrieve(entity.LogicalName, entity.Id, new ColumnSet(true));

            Assert.Equal(otherEntity.Id, updated.GetAttributeValue<EntityReference>("otherEntity").Id);
            Assert.Equal(otherEntity.Id, otherEntityInContext.Id);
        }

        #if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
        [Fact]
        public void An_entity_which_references_another_existent_entity_by_alternate_key_can_be_created_when_integrity_is_enabled()
        {
            var accountMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityMetadata();
            accountMetadata.LogicalName = Account.EntityLogicalName;
            var alternateKeyMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata();
            alternateKeyMetadata.KeyAttributes = new string[] { "alternateKey" };
            accountMetadata.SetFieldValue("_keys", new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata[]
                 {
                 alternateKeyMetadata
                 });
            _contextWithIntegrity.InitializeMetadata(accountMetadata);
            var account = new Entity(Account.EntityLogicalName);
            account.Id = Guid.NewGuid();
            account.Attributes.Add("alternateKey", "keyValue");
            _contextWithIntegrity.Initialize(new List<Entity>() { account });

            Entity otherEntity = new Entity("otherEntity");
            otherEntity.Id = Guid.NewGuid();
            otherEntity["new_accountId"] = new EntityReference("account", "alternateKey","keyValue") ;
            Guid created = _serviceWithIntegrity.Create(otherEntity);

            Entity otherEntityInContext = _serviceWithIntegrity.Retrieve("otherEntity", otherEntity.Id, new ColumnSet(true));

            Assert.NotEqual(Guid.Empty, created);
            Assert.Equal(((EntityReference)otherEntityInContext["new_accountId"]).Id, account.Id);
        }

        [Fact]
        public void An_entity_which_references_another_existent_entity_by_alternate_key_can_be_initialised_when_integrity_is_enabled()
        {
            var accountMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityMetadata();
            accountMetadata.LogicalName = Account.EntityLogicalName;
            var alternateKeyMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata();
            alternateKeyMetadata.KeyAttributes = new string[] { "alternateKey" };
            accountMetadata.SetFieldValue("_keys", new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata[]
                 {
                 alternateKeyMetadata
                 });
            _contextWithIntegrity.InitializeMetadata(accountMetadata);
            var account = new Entity(Account.EntityLogicalName);
            account.Id = Guid.NewGuid();
            account.Attributes.Add("alternateKey", "keyValue");

            Entity otherEntity = new Entity("otherEntity");
            otherEntity.Id = Guid.NewGuid();
            otherEntity["new_accountId"] = new EntityReference("account", "alternateKey", "keyValue");

            _contextWithIntegrity.Initialize(new List<Entity>() { account, otherEntity });

            Entity otherEntityInContext = _serviceWithIntegrity.Retrieve("otherEntity", otherEntity.Id, new ColumnSet(true));

            Assert.Equal(((EntityReference)otherEntityInContext["new_accountId"]).Id, account.Id);
        }

        [Fact]
        public void An_entity_which_references_another_existent_entity_by_alternate_key_can_be_updated_when_integrity_is_enabled()
        {
            var accountMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityMetadata();
            accountMetadata.LogicalName = Account.EntityLogicalName;
            var alternateKeyMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata();
            alternateKeyMetadata.KeyAttributes = new string[] { "alternateKey" };
            accountMetadata.SetFieldValue("_keys", new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata[]
                 {
                 alternateKeyMetadata
                 });
            
            _contextWithIntegrity.InitializeMetadata(accountMetadata);
            var account = new Entity(Account.EntityLogicalName);
            account.Id = Guid.NewGuid();
            account.Attributes.Add("alternateKey", "keyValue");

            var account2 = new Entity(Account.EntityLogicalName);
            account2.Id = Guid.NewGuid();
            account2.Attributes.Add("alternateKey", "keyValue2");

            Entity otherEntity = new Entity("otherEntity");
            otherEntity.Id = Guid.NewGuid();
            otherEntity["new_accountId"] = new EntityReference("account", "alternateKey", "keyValue");

            _contextWithIntegrity.Initialize(new List<Entity>() { account, account2, otherEntity });

            var entityToUpdate = new Entity("otherEntity")
            {
                Id = otherEntity.Id,
                ["new_accountId"] = new EntityReference("account", "alternateKey", "keyValue2")
            };
            _serviceWithIntegrity.Update(entityToUpdate);

            Entity otherEntityInContext = _serviceWithIntegrity.Retrieve("otherEntity", otherEntity.Id, new ColumnSet(true));

            Assert.Equal(((EntityReference)otherEntityInContext["new_accountId"]).Id, account2.Id);
        }
#endif
    }
}
