using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    internal sealed class SendEmailFromTemplateRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "SendEmailFromTemplate", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var templateId = (Guid)request["TemplateId"];
            var regardingId = (Guid)request["RegardingId"];
            var regardingType = (string)request["RegardingType"];

            var email = new Entity("email");
            email["subject"] = "Template Email";
            email["regardingobjectid"] = new EntityReference(regardingType, regardingId);
            var emailId = service.Create(email);

            var response = new OrganizationResponse { ResponseName = "SendEmailFromTemplate" };
            response["Id"] = emailId;
            return response;
        }
    }

    internal sealed class SendFaxRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "SendFax", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            return new OrganizationResponse { ResponseName = "SendFax" };
        }
    }

    internal sealed class SendTemplateRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "SendTemplate", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            return new OrganizationResponse { ResponseName = "SendTemplate" };
        }
    }

    internal sealed class ExportPdfDocumentRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "ExportPdfDocument", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new OrganizationResponse { ResponseName = "ExportPdfDocument" };
            response["PdfFile"] = Array.Empty<byte>();
            return response;
        }
    }
}
