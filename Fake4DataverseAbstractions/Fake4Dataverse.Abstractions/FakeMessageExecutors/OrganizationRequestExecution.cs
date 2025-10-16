
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Abstractions.FakeMessageExecutors
{
    public delegate OrganizationResponse OrganizationRequestExecution(OrganizationRequest req);
}