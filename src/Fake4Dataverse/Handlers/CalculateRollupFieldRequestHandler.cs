using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Handlers
{
    /// <summary>
    /// Handles <see cref="CalculateRollupFieldRequest"/> by triggering rollup field calculation
    /// via the <see cref="CalculatedFieldManager"/>.
    /// </summary>
    internal sealed class CalculateRollupFieldRequestHandler : IOrganizationRequestHandler
    {
        public bool CanHandle(OrganizationRequest request) => request is CalculateRollupFieldRequest;

        public OrganizationResponse Handle(OrganizationRequest request, IOrganizationService service)
        {
            var calcRequest = (CalculateRollupFieldRequest)request;
            var target = calcRequest.Target;
            var fieldName = calcRequest.FieldName;

            // Retrieve the entity to apply rollup calculation
            var entity = service.Retrieve(target.LogicalName, target.Id, new ColumnSet(true));

            if (service is FakeOrganizationService fakeService && fakeService.CalculatedFields.HasFields)
            {
                fakeService.CalculatedFields.ApplyCalculatedFields(entity, fakeService.Store);
            }

            var response = new CalculateRollupFieldResponse();
            response.Results["Entity"] = entity;
            return response;
        }
    }
}
