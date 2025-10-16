using FakeItEasy;
using Microsoft.Xrm.Sdk;
using System;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Plugins;

namespace Fake4Dataverse.Plugins
{
    /// <summary>
    /// Default implementation of plugin context properties for plugin execution
    /// </summary>
    public class XrmFakedPluginContextProperties : IXrmFakedPluginContextProperties
    {
        private readonly IXrmFakedContext _context;

        public XrmFakedPluginContextProperties(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            OrganizationService = context.GetOrganizationService();
            TracingService = context.GetTracingService();
            
            // Create fake organization service factory
            OrganizationServiceFactory = A.Fake<IOrganizationServiceFactory>();
            A.CallTo(() => OrganizationServiceFactory.CreateOrganizationService(A<Guid?>._))
                .ReturnsLazily((Guid? userId) => OrganizationService);
            
            // Create fake service endpoint notification service
            ServiceEndpointNotificationService = A.Fake<IServiceEndpointNotificationService>();
            
#if FAKE_XRM_EASY_9
            // Create fake entity data source retriever service
            EntityDataSourceRetrieverService = A.Fake<IEntityDataSourceRetrieverService>();
#endif
        }

        public IOrganizationService OrganizationService { get; }
        
        public IXrmFakedTracingService TracingService { get; }
        
        public IServiceEndpointNotificationService ServiceEndpointNotificationService { get; }
        
        public IOrganizationServiceFactory OrganizationServiceFactory { get; }

#if FAKE_XRM_EASY_9
        public IEntityDataSourceRetrieverService EntityDataSourceRetrieverService { get; }
        
        public Entity EntityDataSourceRetriever { get; set; }
#endif

        /// <summary>
        /// Creates a service provider for plugin execution
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/write-plug-in
        /// Plugins receive an IServiceProvider that provides access to services like IPluginExecutionContext, 
        /// IOrganizationServiceFactory, ITracingService, etc.
        /// </summary>
        public IServiceProvider GetServiceProvider(XrmFakedPluginExecutionContext pluginContext)
        {
            var serviceProvider = A.Fake<IServiceProvider>();
            
            // Setup service provider to return appropriate services
            A.CallTo(() => serviceProvider.GetService(A<Type>.That.IsEqualTo(typeof(IPluginExecutionContext))))
                .Returns(pluginContext);
            
            A.CallTo(() => serviceProvider.GetService(A<Type>.That.IsEqualTo(typeof(ITracingService))))
                .Returns(TracingService);
            
            A.CallTo(() => serviceProvider.GetService(A<Type>.That.IsEqualTo(typeof(IOrganizationServiceFactory))))
                .Returns(OrganizationServiceFactory);
            
            A.CallTo(() => serviceProvider.GetService(A<Type>.That.IsEqualTo(typeof(IServiceEndpointNotificationService))))
                .Returns(ServiceEndpointNotificationService);

#if FAKE_XRM_EASY_9
            A.CallTo(() => serviceProvider.GetService(A<Type>.That.IsEqualTo(typeof(IEntityDataSourceRetrieverService))))
                .Returns(EntityDataSourceRetrieverService);
#endif
            
            return serviceProvider;
        }
    }
}
