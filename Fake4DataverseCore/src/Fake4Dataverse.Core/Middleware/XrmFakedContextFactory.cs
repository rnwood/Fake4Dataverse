
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Middleware.Crud;
using Fake4Dataverse.Middleware.Messages;

namespace Fake4Dataverse.Middleware
{
    public class XrmFakedContextFactory
    {
        public static IXrmFakedContext New()
        {
            return MiddlewareBuilder
                        .New()
       
                        // Add* -> Middleware configuration
                        .AddCrud()   
                        .AddFakeMessageExecutors()

                        // Use* -> Defines pipeline sequence
                        .UseCrud() 
                        .UseMessages()


                        .Build();
        }

        public static IXrmFakedContext New(IIntegrityOptions integrityOptions)
        {
            return MiddlewareBuilder
                        .New()
       
                        // Add* -> Middleware configuration
                        .AddCrud(integrityOptions)   
                        .AddFakeMessageExecutors()

                        // Use* -> Defines pipeline sequence
                        .UseCrud() 
                        .UseMessages()


                        .Build();
        }
    }
}