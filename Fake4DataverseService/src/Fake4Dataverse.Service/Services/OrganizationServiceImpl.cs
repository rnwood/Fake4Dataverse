using CoreWCF;
using Microsoft.Extensions.Logging;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Service.Services;

/// <summary>
/// WCF service implementation that wraps IOrganizationService from Fake4Dataverse.
/// This service provides 100% compatibility with Microsoft Dataverse SDK types and
/// matches the actual Organization Service SOAP interface.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// The IOrganizationService interface is the core interface for interacting with Dataverse,
/// providing methods for CRUD operations and executing organization requests.
/// 
/// This implementation uses the same method signatures as the real OrganizationService
/// and exposes them via SOAP at the standard endpoint: /XRMServices/2011/Organization.svc
/// </summary>
[ServiceBehavior(IncludeExceptionDetailInFaults = true)]
public class OrganizationServiceImpl : IOrganizationServiceContract
{
    private readonly IOrganizationService _organizationService;
    private readonly ILogger<OrganizationServiceImpl> _logger;

    public OrganizationServiceImpl(IOrganizationService organizationService, ILogger<OrganizationServiceImpl> logger)
    {
        _organizationService = organizationService ?? throw new ArgumentNullException(nameof(organizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a new entity record.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.create
    /// Creates a record in Dataverse and returns the ID of the created record.
    /// </summary>
    public Guid Create(Entity entity)
    {
        _logger.LogInformation("Create request for entity: {LogicalName}", entity?.LogicalName);

        try
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            var id = _organizationService.Create(entity);
            _logger.LogInformation("Created entity {LogicalName} with ID: {Id}", entity.LogicalName, id);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity: {LogicalName}", entity?.LogicalName);
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves an entity record by ID.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.retrieve
    /// Retrieves a single record from Dataverse with the specified columns.
    /// </summary>
    public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet)
    {
        _logger.LogInformation("Retrieve request for entity: {EntityName}, Id: {Id}", entityName, id);

        try
        {
            var entity = _organizationService.Retrieve(entityName, id, columnSet);
            _logger.LogInformation("Retrieved entity {EntityName} with ID: {Id}", entityName, id);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving entity: {EntityName}, Id: {Id}", entityName, id);
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Updates an existing entity record.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.update
    /// Updates the attributes of an existing record in Dataverse.
    /// </summary>
    public void Update(Entity entity)
    {
        _logger.LogInformation("Update request for entity: {LogicalName}, Id: {Id}", 
            entity?.LogicalName, entity?.Id);

        try
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _organizationService.Update(entity);
            _logger.LogInformation("Updated entity {LogicalName} with ID: {Id}", entity.LogicalName, entity.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity: {LogicalName}, Id: {Id}", 
                entity?.LogicalName, entity?.Id);
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Deletes an entity record.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.delete
    /// Deletes a record from Dataverse.
    /// </summary>
    public void Delete(string entityName, Guid id)
    {
        _logger.LogInformation("Delete request for entity: {EntityName}, Id: {Id}", entityName, id);

        try
        {
            _organizationService.Delete(entityName, id);
            _logger.LogInformation("Deleted entity {EntityName} with ID: {Id}", entityName, id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity: {EntityName}, Id: {Id}", entityName, id);
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Associates two entity records.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.associate
    /// Creates a link between two records in Dataverse using a many-to-many relationship.
    /// </summary>
    public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
    {
        _logger.LogInformation("Associate request for entity: {EntityName}, Id: {Id}, Relationship: {Relationship}", 
            entityName, entityId, relationship?.SchemaName);

        try
        {
            _organizationService.Associate(entityName, entityId, relationship, relatedEntities);
            _logger.LogInformation("Associated entities for {EntityName} with ID: {Id}", entityName, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error associating entities");
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Disassociates two entity records.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.disassociate
    /// Removes a link between two records in Dataverse.
    /// </summary>
    public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities)
    {
        _logger.LogInformation("Disassociate request for entity: {EntityName}, Id: {Id}, Relationship: {Relationship}", 
            entityName, entityId, relationship?.SchemaName);

        try
        {
            _organizationService.Disassociate(entityName, entityId, relationship, relatedEntities);
            _logger.LogInformation("Disassociated entities for {EntityName} with ID: {Id}", entityName, entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disassociating entities");
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves multiple entity records.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.retrievemultiple
    /// Retrieves a collection of records from Dataverse based on a query.
    /// Supports QueryExpression, FetchExpression, and QueryByAttribute.
    /// </summary>
    public EntityCollection RetrieveMultiple(QueryBase query)
    {
        var queryType = query?.GetType().Name ?? "Unknown";
        _logger.LogInformation("RetrieveMultiple request with query type: {QueryType}", queryType);

        try
        {
            var result = _organizationService.RetrieveMultiple(query);
            _logger.LogInformation("RetrieveMultiple returned {Count} entities", result?.Entities?.Count ?? 0);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving multiple entities");
            throw new FaultException(ex.Message);
        }
    }

    /// <summary>
    /// Executes an organization request.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.execute
    /// Executes a message request and returns a response.
    /// This method supports all OrganizationRequest types like WhoAmIRequest, RetrieveVersionRequest, etc.
    /// </summary>
    public OrganizationResponse Execute(OrganizationRequest request)
    {
        _logger.LogInformation("Execute request for request type: {RequestType}", request?.GetType().Name);

        try
        {
            var response = _organizationService.Execute(request);
            _logger.LogInformation("Execute completed for request type: {RequestType}", request?.GetType().Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing request");
            throw new FaultException(ex.Message);
        }
    }
}
