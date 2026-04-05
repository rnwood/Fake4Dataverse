using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles InstantiateTemplate requests by creating an email entity based on a template.
    /// </summary>
    internal sealed class InstantiateTemplateRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "InstantiateTemplate", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var templateId = (Guid)request["TemplateId"];
            var objectId = (Guid)request["ObjectId"];
            var objectType = request.Parameters.ContainsKey("ObjectType") ? (string)request["ObjectType"] : "email";

            // Try to retrieve the template
            Entity? template = null;
            try { template = service.Retrieve("template", templateId, new ColumnSet(true)); }
            catch { /* template doesn't exist, use defaults */ }

            // Create an email entity based on template
            var email = new Entity("email");
            email["subject"] = template?.GetAttributeValue<string>("subject") ?? "Template Email";
            email["description"] = template?.GetAttributeValue<string>("body") ?? "";
            email["regardingobjectid"] = new EntityReference(objectType, objectId);

            var collection = new EntityCollection();
            collection.Entities.Add(email);

            var response = new OrganizationResponse { ResponseName = "InstantiateTemplate" };
            response["EntityCollection"] = collection;
            return response;
        }
    }
}
