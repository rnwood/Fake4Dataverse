using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="SendEmailRequest"/> by marking the email entity as sent.
    /// The email entity is updated with <c>statecode = 1</c> (Completed) and <c>statuscode = 3</c> (Sent).
    /// </summary>
    internal sealed class SendEmailRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is SendEmailRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var sendRequest = (SendEmailRequest)request;
            var emailId = sendRequest.EmailId;

            // Mark the email entity as sent
            var update = new Entity("email", emailId)
            {
                ["statecode"] = new OptionSetValue(1),   // Completed
                ["statuscode"] = new OptionSetValue(3)    // Sent
            };
            service.Update(update);

            return new SendEmailResponse();
        }
    }
}
