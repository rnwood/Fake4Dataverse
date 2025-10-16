using FakeItEasy;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.FakeMessageExecutors;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Abstractions.Metadata;
using Fake4Dataverse.Abstractions.Permissions;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Security;
using Fake4Dataverse.Integrity;
using Fake4Dataverse.Metadata;
using Fake4Dataverse.Permissions;
using Fake4Dataverse.Security;
using Fake4Dataverse.Services;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Fake4Dataverse
{
    /// <summary>
    /// A fake context that stores In-Memory entites indexed by logical name and then Entity records, simulating
    /// how entities are persisted in Tables (with the logical name) and then the records themselves
    /// where the Primary Key is the Guid
    /// </summary>
    public partial class XrmFakedContext : IXrmFakedContext
    {
        protected internal IOrganizationService _service;

        public IXrmFakedPluginContextProperties PluginContextProperties { get; set; }

        /// <summary>
        /// Gets the plugin pipeline simulator for registering and executing plugins
        /// </summary>
        public IPluginPipelineSimulator PluginPipelineSimulator { get; private set; }

        /// <summary>
        /// Gets the Cloud Flow simulator for registering and testing Cloud Flows (Power Automate flows).
        /// Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
        /// 
        /// The Cloud Flow simulator enables testing of:
        /// - Dataverse-triggered flows (Create, Update, Delete)
        /// - Dataverse connector actions within flows
        /// - Custom connector actions (via extensibility)
        /// - Flow execution verification and assertion
        /// 
        /// Note: To use Cloud Flow simulation, install the Fake4Dataverse.CloudFlows package
        /// and set this property to an instance of CloudFlowSimulator.
        /// Example: context.CloudFlowSimulator = new CloudFlowSimulator(context);
        /// </summary>
        public ICloudFlowSimulator CloudFlowSimulator { get; set; }

        /// <summary>
        /// All proxy type assemblies available on mocked database.
        /// </summary>
        private List<Assembly> _proxyTypesAssemblies { get; set; }
        public IEnumerable<Assembly> ProxyTypesAssemblies 
        {
            get => _proxyTypesAssemblies;
        }

        protected internal bool Initialised { get; set; }

        /// <summary>
        /// Thread-safe data store with per-entity-type locking for better concurrency.
        /// Operations on different entity types can execute concurrently.
        /// </summary>
        public ConcurrentDictionary<string, Dictionary<Guid, Entity>> Data { get; set; }
        
        /// <summary>
        /// Per-entity-type locks for thread-safe CRUD operations.
        /// Allows concurrent operations on different entity types while maintaining consistency within each type.
        /// </summary>
        private readonly ConcurrentDictionary<string, object> _entityLocks = new ConcurrentDictionary<string, object>();

        [Obsolete("Please use ProxyTypesAssemblies to retrieve assemblies and EnableProxyTypes to add new ones")]
        public Assembly ProxyTypesAssembly
        {
            get
            {
                // TODO What we should do when ProxyTypesAssemblies contains multiple assemblies? One shouldn't throw exceptions from properties.
                return _proxyTypesAssemblies.FirstOrDefault();
            }
            set
            {
                _proxyTypesAssemblies = new List<Assembly>();
                if (value != null)
                {
                    _proxyTypesAssemblies.Add(value);
                }
            }
        }

        /// <summary>
        /// Sets the user to assign the CreatedBy and ModifiedBy properties when entities are added to the context.
        /// All requests will be executed on behalf of this user
        /// </summary>
        [Obsolete("Please use CallerProperties instead")]
        public EntityReference CallerId { get; set; }

        [Obsolete("Please use CallerProperties instead")]
        public EntityReference BusinessUnitId { get; set; }

        public delegate OrganizationResponse ServiceRequestExecution(OrganizationRequest req);

        /// <summary>
        /// Probably should be replaced by FakeMessageExecutors, more generic, which can use custom interfaces rather than a single method / delegate
        /// </summary>
        private Dictionary<Type, ServiceRequestExecution> ExecutionMocks { get; set; }

        private Dictionary<string, IFakeMessageExecutor> GenericFakeMessageExecutors { get; set; }

        private Dictionary<string, XrmFakedRelationship> _relationships { get; set; }
        public IEnumerable<XrmFakedRelationship> Relationships 
        { 
            get => _relationships.Values;
        }

        public IEntityInitializerService EntityInitializerService { get; set; }

        public int MaxRetrieveCount { get; set; }

        public EntityInitializationLevel InitializationLevel { get; set; }

        public ICallerProperties CallerProperties { get; set; }

        /// <summary>
        /// Gets the security configuration for this context.
        /// Security is disabled by default for backward compatibility.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
        /// </summary>
        public ISecurityConfiguration SecurityConfiguration { get; private set; }

        /// <summary>
        /// Gets the security manager for this context.
        /// Provides access to security infrastructure like root BU and System Administrator role IDs.
        /// </summary>
        public ISecurityManager SecurityManager { get; private set; }

        private readonly Dictionary<string, object> _properties;
        private readonly IXrmFakedTracingService _fakeTracingService;

        public XrmFakedContext()
        {
            _fakeTracingService = new XrmFakedTracingService();

            _properties = new Dictionary<string, object>();

            CallerProperties = new CallerProperties();
            SecurityConfiguration = new SecurityConfiguration();
            SecurityManager = new Security.SecurityManager(this);
            
            MaxRetrieveCount = 5000;

            AttributeMetadataNames = new Dictionary<string, Dictionary<string, string>>();
            Data = new ConcurrentDictionary<string, Dictionary<Guid, Entity>>();
            ExecutionMocks = new Dictionary<Type, ServiceRequestExecution>();

            GenericFakeMessageExecutors = new Dictionary<string, IFakeMessageExecutor>();

            _relationships = new Dictionary<string, XrmFakedRelationship>();

            EntityInitializerService = new DefaultEntityInitializerService();

            SetProperty<IAccessRightsRepository>(new AccessRightsRepository());
            SetProperty<IOptionSetMetadataRepository>(new OptionSetMetadataRepository());
            SetProperty<IStatusAttributeMetadataRepository>(new StatusAttributeMetadataRepository());
            SetProperty<IIntegrityOptions>(new IntegrityOptions());
            
            // Initialize audit repository
            InitializeAuditRepository();

            SystemTimeZone = TimeZoneInfo.Local;

            EntityMetadata = new Dictionary<string, EntityMetadata>();

            UsePipelineSimulation = false;

            InitializationLevel = EntityInitializationLevel.Default;

            _proxyTypesAssemblies = new List<Assembly>();

            // Initialize plugin context properties for plugin execution
            PluginContextProperties = new Fake4Dataverse.Plugins.XrmFakedPluginContextProperties(this);

            // Initialize plugin pipeline simulator
            PluginPipelineSimulator = new PluginPipelineSimulator(this);

            // CloudFlowSimulator is now in a separate package (Fake4Dataverse.CloudFlows)
            // To use it, install that package and set: CloudFlowSimulator = new CloudFlowSimulator(this);

            GetOrganizationService();

            // Initialize system entity metadata automatically
            // This ensures metadata tables (entity, attribute) are always available
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/about-entity-reference
            InitializeSystemEntityMetadata();

            // Initialize System Administrator role if security is enabled
            // Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
            InitializeSecurityRoles();

        }

        /// <summary>
        /// Initializes the System Administrator role if security configuration requires it.
        /// This ensures the well-known System Administrator role is always available.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
        /// </summary>
        private void InitializeSecurityRoles()
        {
            if (SecurityConfiguration.AutoGrantSystemAdministratorPrivileges)
            {
                SecurityManager.InitializeSystemAdministratorRole();
            }
        }

        public bool HasProperty<T>()
        {
            return _properties.ContainsKey(typeof(T).FullName);
        }
        
        public T GetProperty<T>() 
        {
            if(!_properties.ContainsKey(typeof(T).FullName)) 
            {
                throw new TypeAccessException($"Property of type '{typeof(T).FullName}' doesn't exists");  
            }

            return (T) _properties[typeof(T).FullName];
        }

        public void SetProperty<T>(T property) 
        {
            if(!_properties.ContainsKey(typeof(T).FullName)) 
            {
                _properties.Add(typeof(T).FullName, property);
            }
            else 
            {
                _properties[typeof(T).FullName] = property;
            }
        }

        public IOrganizationService GetOrganizationService()
        {
            return GetFakedOrganizationService(this);
        }

        public IOrganizationServiceFactory GetOrganizationServiceFactory() 
        {
            var fakedServiceFactory = A.Fake<IOrganizationServiceFactory>();
            A.CallTo(() => fakedServiceFactory.CreateOrganizationService(A<Guid?>._)).ReturnsLazily((Guid? g) => GetOrganizationService());
            return fakedServiceFactory;
        }

        public IXrmFakedTracingService GetTracingService()
        {
            return _fakeTracingService;
        }

        /// <summary>
        /// Initializes the context with the provided entities
        /// </summary>
        /// <param name="entities"></param>
        public virtual void Initialize(IEnumerable<Entity> entities)
        {
            if (Initialised)
            {
                throw new Exception("Initialize should be called only once per unit test execution and XrmFakedContext instance.");
            }

            if (entities == null)
            {
                throw new InvalidOperationException("The entities parameter must be not null");
            }

            foreach (var e in entities)
            {
                // Initialize skips validation to allow test data setup with any state
                // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforcreate
                // Test initialization should allow statecode and other restricted attributes for flexibility
                AddEntityWithDefaults(e, clone: true, usePluginPipeline: false, skipValidation: true);
            }

            Initialised = true;
        }

        public void Initialize(Entity e)
        {
            this.Initialize(new List<Entity>() { e });
        }

        /// <summary>
        /// Enables support for the early-cound types exposed in a specified assembly.
        /// </summary>
        /// <param name="assembly">
        /// An assembly containing early-bound entity types.
        /// </param>
        /// <remarks>
        /// See issue #334 on GitHub. This has quite similar idea as is on SDK method
        /// https://docs.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.client.organizationserviceproxy.enableproxytypes.
        /// </remarks>
        public void EnableProxyTypes(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (_proxyTypesAssemblies.Contains(assembly))
            {
                throw new InvalidOperationException($"Proxy types assembly { assembly.GetName().Name } is already enabled.");
            }

            _proxyTypesAssemblies.Add(assembly);
        }


        public void AddGenericFakeMessageExecutor(string message, IFakeMessageExecutor executor)
        {
            if (!GenericFakeMessageExecutors.ContainsKey(message))
                GenericFakeMessageExecutors.Add(message, executor);
            else
                GenericFakeMessageExecutors[message] = executor;
        }

        public void RemoveGenericFakeMessageExecutor(string message)
        {
            if (GenericFakeMessageExecutors.ContainsKey(message))
                GenericFakeMessageExecutors.Remove(message);
        }

        public void AddRelationship(string schemaname, XrmFakedRelationship relationship)
        {
            _relationships.Add(schemaname, relationship);
        }

        public void RemoveRelationship(string schemaname)
        {
            _relationships.Remove(schemaname);
        }

        public XrmFakedRelationship GetRelationship(string schemaName)
        {
            if (_relationships.ContainsKey(schemaName))
            {
                return _relationships[schemaName];
            }

            return null;
        }

        public void AddAttributeMapping(string sourceEntityName, string sourceAttributeName, string targetEntityName, string targetAttributeName)
        {
            if (string.IsNullOrWhiteSpace(sourceEntityName))
                throw new ArgumentNullException("sourceEntityName");
            if (string.IsNullOrWhiteSpace(sourceAttributeName))
                throw new ArgumentNullException("sourceAttributeName");
            if (string.IsNullOrWhiteSpace(targetEntityName))
                throw new ArgumentNullException("targetEntityName");
            if (string.IsNullOrWhiteSpace(targetAttributeName))
                throw new ArgumentNullException("targetAttributeName");

            var entityMap = new Entity
            {
                LogicalName = "entitymap",
                Id = Guid.NewGuid(),
                ["targetentityname"] = targetEntityName,
                ["sourceentityname"] = sourceEntityName
            };

            var attributeMap = new Entity
            {
                LogicalName = "attributemap",
                Id = Guid.NewGuid(),
                ["entitymapid"] = new EntityReference("entitymap", entityMap.Id),
                ["targetattributename"] = targetAttributeName,
                ["sourceattributename"] = sourceAttributeName
            };

            AddEntityWithDefaults(entityMap);
            AddEntityWithDefaults(attributeMap);
        }

        

        /// <summary>
        /// Deprecated. Use GetOrganizationService instead
        /// </summary>
        /// <returns></returns>
        [Obsolete("Use GetOrganizationService instead")]
        public IOrganizationService GetFakedOrganizationService()
        {
            return GetFakedOrganizationService(this);
        }

        protected IOrganizationService GetFakedOrganizationService(XrmFakedContext context)
        {
            if (context._service != null)
            {
                return context._service;
            }

            var fakedService = A.Fake<IOrganizationService>();

            //Fake / Intercept other requests
            FakeExecute(context, fakedService);
            context._service = fakedService;

            return context._service;
        }

        /// <summary>
        /// Imports multiple solution files into the faked context.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.importsolutionrequest
        /// 
        /// This helper method allows importing multiple solution files in sequence,
        /// which is useful for setting up test environments with multiple solutions.
        /// If any solution import fails, an exception is thrown and no further solutions are imported.
        /// </summary>
        /// <param name="solutionFiles">Array of solution ZIP files as byte arrays</param>
        /// <param name="publishWorkflows">Whether to activate workflows after import (default: false)</param>
        /// <param name="overwriteUnmanagedCustomizations">Whether to overwrite unmanaged customizations (default: false)</param>
        /// <exception cref="System.ArgumentNullException">Thrown when solutionFiles is null</exception>
        /// <exception cref="System.ServiceModel.FaultException">Thrown when any solution import fails</exception>
        /// <example>
        /// <code>
        /// var solution1 = File.ReadAllBytes("Solution1.zip");
        /// var solution2 = File.ReadAllBytes("Solution2.zip");
        /// context.ImportSolutions(new[] { solution1, solution2 });
        /// </code>
        /// </example>
        public void ImportSolutions(byte[][] solutionFiles, bool publishWorkflows = false, bool overwriteUnmanagedCustomizations = false)
        {
            if (solutionFiles == null)
            {
                throw new ArgumentNullException(nameof(solutionFiles), "Solution files array cannot be null");
            }

            var service = GetOrganizationService();
            
            for (int i = 0; i < solutionFiles.Length; i++)
            {
                var solutionFile = solutionFiles[i];
                
                if (solutionFile == null || solutionFile.Length == 0)
                {
                    throw new ArgumentException($"Solution file at index {i} is null or empty", nameof(solutionFiles));
                }

                try
                {
                    var request = new Microsoft.Crm.Sdk.Messages.ImportSolutionRequest
                    {
                        CustomizationFile = solutionFile,
                        PublishWorkflows = publishWorkflows,
                        OverwriteUnmanagedCustomizations = overwriteUnmanagedCustomizations
                    };
                    
                    service.Execute(request);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to import solution at index {i}. {solutionFiles.Length - i - 1} solution(s) not imported.", 
                        ex);
                }
            }
        }

        /// <summary>
        /// Fakes the Execute method of the organization service.
        /// Not all the OrganizationRequest are going to be implemented, so stay tunned on updates!
        /// </summary>
        /// <param name="context"></param>
        /// <param name="fakedService"></param>
        public static void FakeExecute(XrmFakedContext context, IOrganizationService fakedService)
        {
            OrganizationResponse response = null;
            Func<OrganizationRequest, OrganizationResponse> execute = (req) =>
            {
                if (context.ExecutionMocks.ContainsKey(req.GetType()))
                    return context.ExecutionMocks[req.GetType()].Invoke(req);

                if (req.GetType() == typeof(OrganizationRequest)
                    && context.GenericFakeMessageExecutors.ContainsKey(req.RequestName))
                    return context.GenericFakeMessageExecutors[req.RequestName].Execute(req, context);

                throw PullRequestException.NotImplementedOrganizationRequest(req.GetType());
            };

            
            A.CallTo(() => fakedService.Execute(A<OrganizationRequest>._))
                .Invokes((OrganizationRequest req) => response = execute(req))
                .ReturnsLazily((OrganizationRequest req) => response);
        }

        

        

        

    }
}