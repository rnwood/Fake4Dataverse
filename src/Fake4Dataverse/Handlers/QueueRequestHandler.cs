using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles AddToQueue requests by creating a queueitem record.
    /// </summary>
    internal sealed class AddToQueueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "AddToQueue", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var target = (EntityReference)request["Target"];
            var destinationQueueId = (Guid)request["DestinationQueueId"];

            // Create a queueitem record
            var queueItem = new Entity("queueitem");
            queueItem["objectid"] = target;
            queueItem["queueid"] = new EntityReference("queue", destinationQueueId);
            var queueItemId = service.Create(queueItem);

            var response = new OrganizationResponse { ResponseName = "AddToQueue" };
            response["QueueItemId"] = queueItemId;
            return response;
        }
    }

    /// <summary>
    /// Handles RemoveFromQueue requests by deleting the queueitem record.
    /// </summary>
    internal sealed class RemoveFromQueueRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "RemoveFromQueue", StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var queueItemId = (Guid)request["QueueItemId"];
            service.Delete("queueitem", queueItemId);
            return new OrganizationResponse { ResponseName = "RemoveFromQueue" };
        }
    }
}
