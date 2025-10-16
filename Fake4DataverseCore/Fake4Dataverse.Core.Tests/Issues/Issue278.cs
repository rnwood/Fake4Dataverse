using Microsoft.Xrm.Sdk.Metadata;
using System.Collections.Generic;

using Xunit;
using Fake4Dataverse.Extensions;
using Microsoft.Xrm.Sdk.Messages;
using Crm;
using Microsoft.Xrm.Sdk;
using System.Reflection;
using System.Linq;
using Fake4Dataverse.Abstractions.Metadata;

namespace Fake4Dataverse.Tests.Issues
{
    public class Issue278: Fake4DataverseTests
    {
        [Fact]
        public void Reproduce_issue_278()
        {
            string attributeName = "statuscode";
            string label = "A faked label";

            _context.EnableProxyTypes(Assembly.GetAssembly(typeof(Contact)));

            var entityMetadata = new EntityMetadata()
            {
                LogicalName = "contact"
            };

            StatusAttributeMetadata enumAttribute = new StatusAttributeMetadata() { LogicalName = attributeName };

            entityMetadata.SetAttributeCollection(new List<AttributeMetadata>() { enumAttribute });

            var req = new InsertOptionValueRequest()
            {
                EntityLogicalName = Contact.EntityLogicalName,
                AttributeLogicalName = attributeName,
                Label = new Label(label, 0)
            };

            _context.InitializeMetadata(entityMetadata);

            _service.Execute(req);

            //Check the optionsetmetadata was updated
            var key = string.Format("{0}#{1}", Contact.EntityLogicalName, attributeName);

            var optionSetMetadata = _context.GetProperty<IOptionSetMetadataRepository>().GetByName(key);
            Assert.NotNull(optionSetMetadata);

            var option = optionSetMetadata.Options.FirstOrDefault();

            Assert.Equal(label, option.Label.LocalizedLabels[0].Label);

            // Get a list of Option Set values for the Status Reason fields from its metadata
            RetrieveAttributeRequest attReq = new RetrieveAttributeRequest();
            attReq.EntityLogicalName = "contact";
            attReq.LogicalName = "statuscode";
            attReq.RetrieveAsIfPublished = true;

            RetrieveAttributeResponse attResponse = (RetrieveAttributeResponse)_service.Execute(attReq);

            // Cast as StatusAttributeMetadata
            StatusAttributeMetadata statusAttributeMetadata = (StatusAttributeMetadata)attResponse.AttributeMetadata;

            Assert.Equal(label, statusAttributeMetadata.OptionSet.Options.First().Label.LocalizedLabels[0].Label);
        }
    }
}
