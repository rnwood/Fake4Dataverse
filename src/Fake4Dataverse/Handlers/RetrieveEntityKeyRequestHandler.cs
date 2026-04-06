using System;
using System.Linq;
using System.Reflection;
using Fake4Dataverse.Metadata;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Handlers
{
    internal sealed class RetrieveEntityKeyRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is RetrieveEntityKeyRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var retrieveRequest = (RetrieveEntityKeyRequest)request;
            var fakeService = (FakeOrganizationService)service;
            var store = fakeService.Environment.MetadataStore;

            var entityName = retrieveRequest.Parameters.ContainsKey("EntityLogicalName") ? (string)retrieveRequest.Parameters["EntityLogicalName"] : null;
            var keyName = retrieveRequest.Parameters.ContainsKey("LogicalName") ? (string)retrieveRequest.Parameters["LogicalName"] : null;
            if (string.IsNullOrEmpty(entityName))
                throw DataverseFault.InvalidArgumentFault("Entity logical name is required.");
            if (string.IsNullOrEmpty(keyName))
                throw DataverseFault.InvalidArgumentFault("Key name is required.");

            var keyInfo = store.GetAlternateKey(entityName!, keyName!);
            if (keyInfo == null)
            {
                throw DataverseFault.Create(
                    DataverseFault.ObjectDoesNotExist,
                    $"Alternate key '{keyName}' does not exist on entity '{entityName}'.");
            }

            var sdkKey = new EntityKeyMetadata();
            sdkKey.LogicalName = keyInfo.Name;
            sdkKey.KeyAttributes = keyInfo.AttributeNames;

            var response = new RetrieveEntityKeyResponse();
            response.Results["EntityKeyMetadata"] = sdkKey;
            return response;
        }
    }
}
