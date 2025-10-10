using Crm;
using FakeItEasy;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using Xunit;

namespace Fake4Dataverse.Tests
{
    public class FakeContextTestUpdate
    {
        private readonly IXrmFakedContext _ctx;
        private readonly IOrganizationService _service;
        public FakeContextTestUpdate() 
        {
            _ctx = XrmFakedContextFactory.New();
            _service = _ctx.GetOrganizationService();
        }

        [Fact]
        public void When_a_null_entity_is_updated_an_exception_is_thrown()
        {
            var ex = Assert.Throws<InvalidOperationException>(() => _service.Update(null));
            Assert.Equal(ex.Message, "The entity must not be null");
        }

        [Fact]
        public void When_an_entity_is_updated_with_an_empty_guid_an_exception_is_thrown()
        {
            _ctx.Initialize(new Entity("account") { Id = Guid.NewGuid() });

            var e = new Entity("account") { Id = Guid.Empty };

            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Update(e));
            Assert.Equal("account with Id 00000000-0000-0000-0000-000000000000 Does Not Exist", ex.Message);
        }

        [Fact]
        public void When_an_entity_is_updated_with_an_empty_logical_name_an_exception_is_thrown()
        {
            var e = new Entity("") { Id = Guid.NewGuid() };

            var ex = Assert.Throws<InvalidOperationException>(() => _service.Update(e));
            Assert.Equal("The entity logical name must not be null or empty.", ex.Message);
        }

        [Fact]
        public void When_an_entity_is_updated_with_a_null_attribute_the_attribute_is_removed()
        {
            var entity = new Account { Id = Guid.NewGuid() };
            entity.DoNotEMail = true;
            _ctx.Initialize(entity);

            var update = new Account() { Id = entity.Id };
            update.DoNotEMail = null;
            _service.Update(update);

            var updatedEntityAllAttributes = _service.Retrieve(Account.EntityLogicalName, update.Id, new ColumnSet(true));
            var updatedEntityAllAttributesEarlyBound = updatedEntityAllAttributes.ToEntity<Account>();

            var updatedEntitySingleAttribute = _service.Retrieve(Account.EntityLogicalName, update.Id, new ColumnSet(new string[] { "donotemail" }));
            var updatedEntitySingleAttributeEarlyBound = updatedEntityAllAttributes.ToEntity<Account>();

            Assert.Null(updatedEntityAllAttributesEarlyBound.DoNotEMail);
            Assert.False(updatedEntityAllAttributes.Attributes.ContainsKey("donotemail"));

            Assert.Null(updatedEntitySingleAttributeEarlyBound.DoNotEMail);
            Assert.False(updatedEntitySingleAttribute.Attributes.ContainsKey("donotemail"));
        }

        [Fact]
        public void When_updating_an_entity_the_context_should_reflect_changes()
        {
            var e = new Entity("account") { Id = Guid.Empty };
            e["name"] = "Before update";
            var guid = _service.Create(e);

            Assert.Equal("Before update", _ctx.CreateQuery("account").FirstOrDefault()["name"]);

            //now update the name
            e = new Entity("account") { Id = guid };
            e["name"] = "After update";
            _service.Update(e);

            Assert.Equal("After update", _ctx.CreateQuery("account").FirstOrDefault()["name"]);
        }

#if !FAKE_XRM_EASY && !FAKE_XRM_EASY_2013 && !FAKE_XRM_EASY_2015
        [Fact]
        public void When_updating_an_entity_by_alternate_key_the_context_should_reflect_changes()
        {
            var accountMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityMetadata();
            accountMetadata.LogicalName = Account.EntityLogicalName;
            var alternateKeyMetadata = new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata();
            alternateKeyMetadata.KeyAttributes = new string[] { "AccountNumber" };
            accountMetadata.SetFieldValue("_keys", new Microsoft.Xrm.Sdk.Metadata.EntityKeyMetadata[]
                 {
                 alternateKeyMetadata
                 });
            _ctx.InitializeMetadata(accountMetadata);

            var e = new Entity("account");
            e["AccountNumber"] = 9000;
            e["name"] = "Before update";
            var guid = _service.Create(e);

            Assert.Equal("Before update", _ctx.CreateQuery("account").FirstOrDefault()["name"]);

            //now update the name
            e = new Entity("account", "AccountNumber", 9000);
            e["name"] = "After update";
            _service.Update(e);

             Assert.Equal("After update", _ctx.CreateQuery("account").FirstOrDefault()["name"]);
        }
#endif

#if FAKE_XRM_EASY_9
        [Fact]
        public void When_updating_an_optionsetvaluecollection_the_context_should_reflect_changes()
        {
            var e = new Entity("contact") { Id = Guid.Empty };
            e["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(1) };
            var guid = _service.Create(e);

            Assert.Equal(new OptionSetValueCollection() { new OptionSetValue(1) }, _ctx.CreateQuery("contact").FirstOrDefault()["new_multiselectattribute"]);

            //now update the name
            e = new Entity("contact") { Id = guid };
            e["new_multiselectattribute"] = new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) };
            _service.Update(e);

            Assert.Equal(new OptionSetValueCollection() { new OptionSetValue(2), new OptionSetValue(3) } , _ctx.CreateQuery("contact").FirstOrDefault()["new_multiselectattribute"]);
        }
#endif

        [Fact]
        public void When_update_is_invoked_with_non_existing_entity_an_exception_is_thrown()
        {
            //Initialize the context with a single entity
            var guid = Guid.NewGuid();
            var nonExistingGuid = Guid.NewGuid();
            var data = new List<Entity>() {
                new Entity("account") { Id = guid }
            }.AsQueryable();

            _ctx.Initialize(data);

            var update = new Entity("account") { Id = nonExistingGuid };
            var ex = Assert.Throws<FaultException<OrganizationServiceFault>>(() => _service.Update(update));

            Assert.Equal(ex.Message, string.Format("account with Id {0} Does Not Exist", nonExistingGuid));
        }

        [Fact]
        public void When_updating_an_entity_an_unchanged_attribute_remains_the_same()
        {
            _ctx.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));

            var existingAccount = new Account() { Id = Guid.NewGuid(), Name = "Super Great Customer", AccountNumber = "69" };
            _ctx.Initialize(new List<Entity>()
            {
                existingAccount
            });

            //Create a new entity class to update the name
            var accountToUpdate = new Account() { Id = existingAccount.Id };
            accountToUpdate.Name = "Super Great Customer Name Updated!";

            //Update the entity in the context
            _service.Update(accountToUpdate);

            //Make sure existing entity still maintains AccountNumber property
            var account = _ctx.CreateQuery<Account>().FirstOrDefault();
            Assert.Equal(account.AccountNumber, "69");
        }

        [Fact]
        public void When_updating_an_entity_only_one_entity_is_updated()
        {
            _ctx.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));

            var existingAccount = new Account() { Id = Guid.NewGuid(), Name = "Super Great Customer", AccountNumber = "69" };
            var otherExistingAccount = new Account() { Id = Guid.NewGuid(), Name = "Devil Customer", AccountNumber = "666" };

            _ctx.Initialize(new List<Entity>()
            {
                existingAccount, otherExistingAccount
            });

            //Create a new entity class to update the first account
            var accountToUpdate = new Account() { Id = existingAccount.Id };
            accountToUpdate.Name = "Super Great Customer Name Updated!";

            //Update the entity in the context
            _service.Update(accountToUpdate);

            //Make other account wasn't updated
            var account = _ctx.CreateQuery<Account>().Where(e => e.Id == otherExistingAccount.Id).FirstOrDefault();
            Assert.Equal(account.Name, "Devil Customer");
        }

        [Fact]
        public void When_updating_an_entity_using_organization_context_changes_should_be_saved()
        {
            _ctx.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));

            var existingAccount = new Account() { Id = Guid.NewGuid(), Name = "Super Great Customer", AccountNumber = "69" };

            _ctx.Initialize(new List<Entity>()
            {
                existingAccount
            });

            using (var ctx = new OrganizationServiceContext(_service))
            {
                existingAccount.Name = "Super Great Customer Name Updated!";

                ctx.Attach(existingAccount);
                ctx.UpdateObject(existingAccount);
                ctx.SaveChanges();
            }

            //Make other account wasn't updated
            var account = _ctx.CreateQuery<Account>().Where(e => e.Id == existingAccount.Id).FirstOrDefault();
            Assert.Equal(account.Name, "Super Great Customer Name Updated!");
        }

        [Fact]
        public void When_updating_a_not_existing_entity_using_organization_context_exception_should_be_thrown()
        {
            _ctx.EnableProxyTypes(Assembly.GetAssembly(typeof(Account)));

            var existingAccount = new Account() { Id = Guid.NewGuid(), Name = "Super Great Customer", AccountNumber = "69" };

            using (var ctx = new OrganizationServiceContext(_service))
            {
                existingAccount.Name = "Super Great Customer Name Updated!";

                ctx.Attach(existingAccount);
                ctx.UpdateObject(existingAccount);
                Assert.Throws<SaveChangesException>(() => ctx.SaveChanges());
            }
        }

        [Fact]
        public void Should_Not_Change_Context_Objects_Without_Update()
        {
            var entityId = Guid.NewGuid();

            _ctx.Initialize(new[] {
                new Entity ("account")
                {
                    Id = entityId,
                    Attributes = new AttributeCollection
                    {
                        { "accountname", "Adventure Works" }
                    }
                }
            });

            var firstRetrieve = _service.Retrieve("account", entityId, new ColumnSet(true));
            var secondRetrieve = _service.Retrieve("account", entityId, new ColumnSet(true));

            firstRetrieve["accountname"] = "Updated locally";

            Assert.Equal("Updated locally", firstRetrieve["accountname"]);
            Assert.Equal("Adventure Works", secondRetrieve["accountname"]);
        }

        [Fact]
        public void Should_Not_Change_Context_Early_Bound_Objects_Without_Update()
        {
            var entityId = Guid.NewGuid();

            _ctx.Initialize(new[] {
                new Account()
                {
                    Id = entityId,
                    Attributes = new AttributeCollection
                    {
                        { "accountname", "Adventure Works" }
                    }
                }
            });

            var firstRetrieve = _service.Retrieve("account", entityId, new ColumnSet(true));
            var secondRetrieve = _service.Retrieve("account", entityId, new ColumnSet(true));

            firstRetrieve["accountname"] = "Updated locally";

            Assert.Equal("Updated locally", firstRetrieve["accountname"]);
            Assert.Equal("Adventure Works", secondRetrieve["accountname"]);
        }

        [Fact]
        public void Should_Not_Change_Context_Objects_Without_Update_And_Retrieve_Multiple()
        {
            var entityId = Guid.NewGuid();

            _ctx.Initialize(new[] {
                new Account
                {
                    Id = entityId,
                    Name = "Adventure Works"
                }
            });

            Account firstRetrieve, secondRetrieve = null;
            using (var ctx = new XrmServiceContext(_service))
            {
                firstRetrieve = ctx.CreateQuery<Account>()
                                    .Where(a => a.AccountId == entityId)
                                    .FirstOrDefault();
            }

            using (var ctx = new XrmServiceContext(_service))
            {
                secondRetrieve = ctx.CreateQuery<Account>()
                                    .Where(a => a.AccountId == entityId)
                                    .FirstOrDefault();
            }

            firstRetrieve.Name = "Updated locally";

            Assert.False(firstRetrieve == secondRetrieve);
            Assert.Equal("Updated locally", firstRetrieve.Name);
            Assert.Equal("Adventure Works", secondRetrieve.Name);
        }

        [Fact]
        public void Should_Not_Raise_An_Exception_When_Updating_Status()
        {
            var entityId = Guid.NewGuid();

            _ctx.Initialize(new[] {
                new Account()
                {
                    Id = entityId,
                    Attributes = new AttributeCollection
                    {
                        { "statecode", 0 }  //0 = Active, anything else: Inactive
                    }
                }
            });

            var accountToUpdate = new Account()
            {
                Id = entityId,
                Name = "FC Barcelona",
                ["statecode"] = new OptionSetValue(1)
            };

            _service.Update(accountToUpdate);
            var updatedAccount = _ctx.CreateQuery<Account>().FirstOrDefault();
            Assert.Equal(1, (int)updatedAccount.StateCode.Value);
        }

        [Fact]
        public void Should_Return_Updated_EntityReference_Name()
        {
            var userMetadata = new EntityMetadata() { LogicalName = "systemuser" };
            userMetadata.SetSealedPropertyValue("PrimaryNameAttribute", "fullname");

            var user = new Entity() { LogicalName = "systemuser", Id = Guid.NewGuid() };
            user["fullname"] = "Fake XrmEasy";

            _ctx.InitializeMetadata(userMetadata);
            _ctx.Initialize(user);

            (_ctx as XrmFakedContext).CallerId = user.ToEntityReference();

            var account = new Entity() { LogicalName = "account" };

            var service = _ctx.GetOrganizationService();
            var accountId = service.Create(account);

            user["fullname"] = "Good Job";
            service.Update(user);

            account = service.Retrieve("account", accountId, new ColumnSet(true));

            Assert.Equal("Good Job", account.GetAttributeValue<EntityReference>("ownerid").Name);
        }
    }
}