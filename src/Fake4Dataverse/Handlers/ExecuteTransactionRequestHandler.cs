using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="ExecuteTransactionRequest"/> by executing all requests atomically
    /// within a single logical transaction. If any request fails, all changes made by preceding
    /// requests in the batch are rolled back via an undo log that records the inverse of each
    /// store mutation, matching real Dataverse all-or-nothing semantics. The undo-log approach
    /// is concurrency-safe: rolling back one transaction only undoes its own changes, leaving
    /// concurrent transactions' writes intact.
    /// </summary>
    internal sealed class ExecuteTransactionRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) =>
            string.Equals(request.RequestName, "ExecuteTransaction", System.StringComparison.OrdinalIgnoreCase);

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var txRequest = OrganizationRequestTypeAdapter.AsTyped<ExecuteTransactionRequest>(request);
            var requests = txRequest.Requests;
            if (requests == null)
                throw DataverseFault.InvalidArgumentFault("ExecuteTransactionRequest.Requests must not be null.");

            // Validate nesting: ExecuteTransactionRequest cannot contain ExecuteMultiple or ExecuteTransaction
            for (int i = 0; i < requests.Count; i++)
            {
                var name = requests[i].RequestName;
                if (string.Equals(name, "ExecuteMultiple", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "ExecuteTransaction", StringComparison.OrdinalIgnoreCase))
                {
                    throw DataverseFault.Create(DataverseFault.Unspecified,
                        $"ExecuteTransactionRequest cannot contain nested {name} requests.");
                }
            }

            // Set up the undo log on the store so every Create/Update/Delete automatically
            // records its inverse operation.
            InMemoryEntityStore? store = null;
            if (service is FakeOrganizationService fakeService)
                store = fakeService.Environment.Store;

            var undoLog = new TransactionUndoLog();
            if (store != null)
                store.ActiveUndoLog = undoLog;

            try
            {
                var response = new ExecuteTransactionResponse();
                var responses = new OrganizationResponseCollection();

                for (int i = 0; i < requests.Count; i++)
                {
                    try
                    {
                        var subResponse = service.Execute(requests[i]);
                        responses.Add(subResponse);
                    }
                    catch (Exception ex)
                    {
                        // Roll back only this transaction's mutations, leaving other
                        // concurrent writes intact.
                        if (store != null)
                        {
                            store.ActiveUndoLog = null;
                            undoLog.Rollback(store);
                        }

                        response.Results["FaultedRequestIndex"] = i;
                        throw DataverseFault.Create(DataverseFault.Unspecified,
                            $"ExecuteTransaction failed at request index {i}: {ex.Message}");
                    }
                }

                response.Results["Responses"] = responses;
                return response;
            }
            finally
            {
                if (store != null)
                    store.ActiveUndoLog = null;
            }
        }
    }
}
