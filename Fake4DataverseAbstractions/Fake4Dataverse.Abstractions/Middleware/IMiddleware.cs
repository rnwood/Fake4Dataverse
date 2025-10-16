using System;

namespace Fake4Dataverse.Abstractions.Middleware
{
    public interface IMiddlewareBuilder
    {
        IMiddlewareBuilder Add(Action<IXrmFakedContext> context);
        IMiddlewareBuilder Use(Func<OrganizationRequestDelegate, OrganizationRequestDelegate> middleware);

        IXrmFakedContext Build();
    }
}
