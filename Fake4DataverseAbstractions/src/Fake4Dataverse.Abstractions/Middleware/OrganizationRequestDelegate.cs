

using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Abstractions.Middleware
{
    public delegate OrganizationResponse OrganizationRequestDelegate(IXrmFakedContext context, OrganizationRequest request);
}