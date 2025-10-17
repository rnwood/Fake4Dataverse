using Crm;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Middleware;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.UpsertRequestTests
{
    public class UpsertRequestTests : Fake4DataverseTests
    {

        [Fact]
        public void Upsert_Creates_Record_When_It_Does_Not_Exist()
        {
            _context.EnableProxyTypes(Assembly.GetExecutingAssembly());

            var contact = new Contact()
            {
                Id = Guid.NewGuid(),
                FirstName = "FakeXrm",
                LastName = "Easy"
            };

            var request = new UpsertRequest()
            {
                Target = contact
            };

            var response = (UpsertResponse)_service.Execute(request);

            var contactCreated = _context.CreateQuery<Contact>().FirstOrDefault();

            Assert.True(response.RecordCreated);
            Assert.NotNull(contactCreated);
        }

        [Fact]
        public void Upsert_Updates_Record_When_It_Exists()
        {
            var contact = new Contact()
            {
                Id = Guid.NewGuid(),
                FirstName = "FakeXrm"
            };
            _context.Initialize(new[] { contact });

            contact = new Contact()
            {
                Id = contact.Id,
                FirstName = "FakeXrm2",
                LastName = "Easy"
            };

            var request = new UpsertRequest()
            {
                Target = contact
            };


            var response = (UpsertResponse)_service.Execute(request);
            var contactUpdated = _context.CreateQuery<Contact>().FirstOrDefault();

            Assert.False(response.RecordCreated);
            Assert.Equal("FakeXrm2", contactUpdated.FirstName);
        }

        [Fact]
        public void Upsert_Creates_Record_When_It_Does_Not_Exist_Using_Alternate_Key()
        {
            _context.EnableProxyTypes(Assembly.GetExecutingAssembly());
            _context.InitializeMetadata(Assembly.GetExecutingAssembly());

            var metadata = _context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("_keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname"}
                }
            });
            _context.SetEntityMetadata(metadata);
            var contact = new Contact()
            {
                FirstName = "FakeXrm",
                LastName = "Easy"
            };
            contact.KeyAttributes.Add("firstname", contact.FirstName);

            var request = new UpsertRequest()
            {
                Target = contact
            };

            var response = (UpsertResponse)_service.Execute(request);

            Assert.True(response.RecordCreated);
        }

        [Fact]
        public void Upsert_Updates_Record_When_It_Exists_Using_Alternate_Key()
        {
            _context.EnableProxyTypes(Assembly.GetExecutingAssembly());
            _context.InitializeMetadata(Assembly.GetExecutingAssembly());


            var metadata = _context.GetEntityMetadataByName("contact");
            metadata.SetFieldValue("_keys", new EntityKeyMetadata[]
            {
                new EntityKeyMetadata()
                {
                    KeyAttributes = new string[]{"firstname"}
                }
            });
            _context.SetEntityMetadata(metadata);

            var contact = new Contact()
            {
                Id = Guid.NewGuid(),
                FirstName = "FakeXrm",
                LastName = "Easy"
            };
            _context.Initialize(new[] { contact });

            contact = new Contact()
            {
                FirstName = "FakeXrm2",
                LastName = "Easy2"
            };

            contact.KeyAttributes.Add("firstname", "FakeXrm");

            var request = new UpsertRequest()
            {
                Target = contact
            };

            var response = (UpsertResponse)_service.Execute(request);

            Assert.False(response.RecordCreated);
        }
    }
}
