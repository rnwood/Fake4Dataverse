using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Diagnostics;
using System.Linq;
using Fake4Dataverse.Extensions;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Metadata;

namespace Fake4Dataverse.FakeMessageExecutors
{
    public class InsertStatusValueRequestExecutor : IFakeMessageExecutor
    {
        public bool CanExecute(OrganizationRequest request)
        {
            return request is InsertStatusValueRequest;
        }

        public OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx)
        {
            var req = request as InsertStatusValueRequest;

            Debug.Assert(req != null, nameof(req) + " != null");
            if (req.Label == null)
                throw new Exception("Label must not be null");

            if (string.IsNullOrWhiteSpace(req.Label.LocalizedLabels[0].Label))
            {
                throw new Exception("Label must not be empty");
            }

            if (string.IsNullOrEmpty(req.OptionSetName)
                && (string.IsNullOrEmpty(req.EntityLogicalName)
                || string.IsNullOrEmpty(req.AttributeLogicalName)))
            {
                throw new Exception("At least OptionSetName or both the EntityName and AttributeName must be provided");
            }

            bool isUsingOptionSet = !string.IsNullOrWhiteSpace(req.OptionSetName);

            var key = !string.IsNullOrWhiteSpace(req.OptionSetName) ? req.OptionSetName : $"{req.EntityLogicalName}#{req.AttributeLogicalName}";

            var statusAttributeMetadataRepository = ctx.GetProperty<IStatusAttributeMetadataRepository>();
            
            StatusAttributeMetadata statusValuesMetadata = null;
            if(isUsingOptionSet)
            {
                statusValuesMetadata = statusAttributeMetadataRepository.GetByGlobalOptionSetName(req.OptionSetName);
            }
            else 
            {
                statusValuesMetadata = statusAttributeMetadataRepository.GetByAttributeName(req.EntityLogicalName, req.AttributeLogicalName);
            }

            if(statusValuesMetadata == null)
            {
                statusValuesMetadata = new StatusAttributeMetadata();
            }

            if(isUsingOptionSet)
            {
                statusAttributeMetadataRepository.Set(req.OptionSetName, statusValuesMetadata);
            }
            else 
            {
                statusAttributeMetadataRepository.Set(req.EntityLogicalName, req.AttributeLogicalName, statusValuesMetadata);
            }

            //statusValuesMetadata.
            statusValuesMetadata.OptionSet = new OptionSetMetadata();
            statusValuesMetadata.OptionSet.Options.Add(new StatusOptionMetadata()
            {
                MetadataId = Guid.NewGuid(),
                Value = req.Value,
                Label = req.Label,
                State = req.StateCode,
                Description = req.Label
            });
            

            if (!string.IsNullOrEmpty(req.EntityLogicalName))
            {
                var entityMetadata = ctx.GetEntityMetadataByName(req.EntityLogicalName);
                if (entityMetadata != null)
                {
                    var attribute = entityMetadata
                            .Attributes
                            .FirstOrDefault(a => a.LogicalName == req.AttributeLogicalName);

                    if (attribute == null)
                    {
                        throw new Exception($"You are trying to insert an option set value for entity '{req.EntityLogicalName}' with entity metadata associated but the attribute '{req.AttributeLogicalName}' doesn't exist in metadata");
                    }

                    if (!(attribute is EnumAttributeMetadata))
                    {
                        throw new Exception($"You are trying to insert an option set value for entity '{req.EntityLogicalName}' with entity metadata associated but the attribute '{req.AttributeLogicalName}' is not a valid option set field (not a subtype of EnumAttributeMetadata)");
                    }                    

                    var enumAttribute = attribute as EnumAttributeMetadata;

                    var options = enumAttribute.OptionSet == null ? new OptionMetadataCollection() : enumAttribute.OptionSet.Options;
                    
                    options.Add(new StatusOptionMetadata(){Value = req.Value, Label = req.Label, State = req.StateCode, Description = req.Label});

                    enumAttribute.OptionSet = new OptionSetMetadata(options);                    

                    entityMetadata.SetAttribute(enumAttribute);
                    ctx.SetEntityMetadata(entityMetadata);
                }
            }
            return new InsertStatusValueResponse();
        }       

        public Type GetResponsibleRequestType()
        {
            return typeof(InsertStatusValueRequest);
        }
    }
}