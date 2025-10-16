using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.Abstractions.Plugins;
using Fake4Dataverse.Abstractions.Security;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Fake4Dataverse.Abstractions
{
    public interface IXrmFakedContext: IXrmBaseContext
    {
        /// <summary>
        /// Returns the caller properties, that is, the default user and business unit used to impersonate service calls
        /// </summary>
        ICallerProperties CallerProperties { get; set; }

        /// <summary>
        /// Gets the security configuration for this context.
        /// Security is disabled by default for backward compatibility.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
        /// </summary>
        ISecurityConfiguration SecurityConfiguration { get; }

        /// <summary>
        /// Returns an instance of a tracing service
        /// </summary>
        /// <returns></returns>
        IXrmFakedTracingService GetTracingService();

        /// <summary>
        /// Creates a queryable for a strongly-typed entity
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IQueryable<T> CreateQuery<T>() where T : Entity;

        /// <summary>
        /// Creates a queryable for a late bound entity
        /// </summary>
        /// <param name="logicalName"></param>
        /// <returns></returns>
        IQueryable<Entity> CreateQuery(string logicalName);

        /// <summary>
        /// Retrieves an entity by primary key as currently stored in the in-memory database.
        /// Useful if you want to bypass a retrieve message, and simpler than using CreateQuery.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        T GetEntityById<T>(Guid id) where T: Entity;

        /// <summary>
        /// Same as GetEntityById<T> but for late bound entities
        /// </summary>
        /// <param name="logicalName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        Entity GetEntityById(string logicalName, Guid id);

        /// <summary>
        /// Returns true if record of the logicalName and id exists in the in-memory database
        /// </summary>
        /// <param name="logicalName"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        bool ContainsEntity(string logicalName, Guid id);

        /// <summary>
        /// Receives a list of entities, that are used to initialize the context with those
        /// </summary>
        /// <param name="entities"></param>
        void Initialize(IEnumerable<Entity> entities);
        
        /// Initializes the context with a single entity when only one is needed
        void Initialize(Entity entity);

        IXrmFakedPluginContextProperties PluginContextProperties { get; set; }

        /// <summary>
        /// Gets the plugin pipeline simulator for registering and executing plugins
        /// </summary>
        IPluginPipelineSimulator PluginPipelineSimulator { get; }

        /// <summary>
        /// Gets or sets whether pipeline simulation is enabled.
        /// When true, plugins registered via PluginPipelineSimulator will automatically execute during CRUD operations.
        /// Default is false.
        /// </summary>
        bool UsePipelineSimulation { get; set; }

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
        ICloudFlowSimulator CloudFlowSimulator { get; set; }

        void AddEntity(Entity e, bool skipValidation = false);
        void AddEntityWithDefaults(Entity e, bool clone = false, bool usePluginPipeline = false, bool skipValidation = false);
        Guid CreateEntity(Entity e);
        void UpdateEntity(Entity e);
        void DeleteEntity(EntityReference er);
        
        
        Type FindReflectedType(string logicalName);
        Type FindReflectedAttributeType(Type earlyBoundType, string sEntityName, string attributeName);

        void EnableProxyTypes(Assembly assembly);
        IEnumerable<Assembly> ProxyTypesAssemblies { get; }
        void InitializeMetadata(IEnumerable<EntityMetadata> entityMetadataList);
        void InitializeMetadata(EntityMetadata entityMetadata);
        void InitializeMetadata(Assembly earlyBoundEntitiesAssembly);
        void InitializeMetadataFromCdmFile(string cdmJsonFilePath);
        void InitializeMetadataFromCdmFiles(IEnumerable<string> cdmJsonFilePaths);
        Task InitializeMetadataFromStandardCdmSchemasAsync(IEnumerable<string> schemaNames);
        Task InitializeMetadataFromStandardCdmEntitiesAsync(IEnumerable<string> entityNames);
        void InitializeSystemEntityMetadata();
        IQueryable<EntityMetadata> CreateMetadataQuery();
        EntityMetadata GetEntityMetadataByName(string sLogicalName);
        void SetEntityMetadata(EntityMetadata em);
        void AddRelationship(string schemaname, XrmFakedRelationship relationship);
        void RemoveRelationship(string schemaname);
        XrmFakedRelationship GetRelationship(string schemaName);
        IEnumerable<XrmFakedRelationship> Relationships { get; }

        Guid GetRecordUniqueId(EntityReference record, bool validate = true);
    }
}