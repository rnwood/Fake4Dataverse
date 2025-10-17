using Crm;
using Fake4Dataverse.Abstractions.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Linq;
using Xunit;

namespace Fake4Dataverse.Tests.FakeContextTests.OptionSetValuesRequestTests
{
    public class OptionSetValueRequestsTests: Fake4DataverseTests
    {
        [Fact]
        public void When_calling_insert_option_set_value_without_label_exception_is_thrown()
        {
            var req = new InsertOptionValueRequest()
            {
                Label = new Label("", 0)
            };

            Assert.Throws<Exception>(() => _service.Execute(req));
        }

        [Fact]
        public void When_calling_insert_option_set_value_without_optionsetname_exception_is_thrown()
        {
            var req = new InsertOptionValueRequest()
            {
                Label = new Label("Yeah! This is a fake label!", 0)
            };

            Assert.Throws<Exception>(() => _service.Execute(req));
        }

        [Fact]
        public void When_calling_insert_option_set_value_without_entityname_or_attributename_exception_is_thrown()
        {
            var req = new InsertOptionValueRequest()
            {
                EntityLogicalName = "Not empty",
                Label = new Label("Yeah! This is a fake label!", 0)
            };

            Assert.Throws<Exception>(() => _service.Execute(req));

            req = new InsertOptionValueRequest()
            {
                AttributeLogicalName = "Not empty",
                Label = new Label("Yeah! This is a fake label!", 0)
            };

            Assert.Throws<Exception>(() => _service.Execute(req));
        }

        [Fact]
        public void When_calling_insert_option_set_value_for_global_optionset_optionmetadata_contains_it()
        {
            var req = new InsertOptionValueRequest()
            {
                OptionSetName = "GlobalOptionSet",
                Label = new Label("Yeah! This is a fake label!", 0)
            };

            _service.Execute(req);

            //Check the optionsetmetadata was updated
            var optionSetMetadata = _context.GetProperty<IOptionSetMetadataRepository>().GetByName("GlobalOptionSet");
            Assert.NotNull(optionSetMetadata);

            var option = optionSetMetadata.Options.FirstOrDefault();
            Assert.NotNull(option);
            Assert.Equal("Yeah! This is a fake label!", option.Label.LocalizedLabels[0].Label);
        }

        [Fact]
        public void When_calling_insert_option_set_value_for_local_optionset_optionmetadata_contains_it()
        {
            var req = new InsertOptionValueRequest()
            {
                EntityLogicalName = Account.EntityLogicalName,
                AttributeLogicalName = "new_custom",
                Label = new Label("Yeah! This is a fake label!", 0)
            };

            _service.Execute(req);

            //Check the optionsetmetadata was updated
            var key = string.Format("{0}#{1}", req.EntityLogicalName, req.AttributeLogicalName);
            var optionSetMetadata = _context.GetProperty<IOptionSetMetadataRepository>().GetByName(key);

            Assert.NotNull(optionSetMetadata);

            var option = optionSetMetadata.Options.FirstOrDefault();
            Assert.NotNull(option);
            Assert.Equal("Yeah! This is a fake label!", option.Label.LocalizedLabels[0].Label);
        }
    }
}