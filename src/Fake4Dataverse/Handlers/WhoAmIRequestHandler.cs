using System;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="WhoAmIRequest"/> by returning a configurable fake user/org/business unit.
    /// </summary>
    public sealed class WhoAmIRequestHandler : IOrganizationRequestHandler
    {
        /// <summary>
        /// The user id returned by WhoAmI. Defaults to a deterministic GUID.
        /// </summary>
        public Guid UserId { get; set; } = new Guid("00000000-0000-0000-0000-000000000001");

        /// <summary>
        /// The organization id returned by WhoAmI.
        /// </summary>
        public Guid OrganizationId { get; set; } = new Guid("00000000-0000-0000-0000-000000000002");

        /// <summary>
        /// The business unit id returned by WhoAmI.
        /// </summary>
        public Guid BusinessUnitId { get; set; } = new Guid("00000000-0000-0000-0000-000000000003");

        /// <inheritdoc />
        public bool CanHandle(OrganizationRequest request) => request is WhoAmIRequest;

        /// <inheritdoc />
        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var response = new WhoAmIResponse();
            response.Results["UserId"] = UserId;
            response.Results["OrganizationId"] = OrganizationId;
            response.Results["BusinessUnitId"] = BusinessUnitId;
            return response;
        }
    }
}
