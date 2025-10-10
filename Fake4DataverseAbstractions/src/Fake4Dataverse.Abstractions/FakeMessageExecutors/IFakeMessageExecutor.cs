

using System;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Abstractions.FakeMessageExecutors
{
    /// <summary>
    /// An interface to delegate custom messages to be executed via a IOrganizationService.Execute method.
    /// Each executor is in charge of encapsulating a single request and declare which requests can handle via de CanExecute method
    /// </summary>
    public interface IFakeMessageExecutor
    {
        bool CanExecute(OrganizationRequest request);

        Type GetResponsibleRequestType();

        OrganizationResponse Execute(OrganizationRequest request, IXrmFakedContext ctx);
    }
}