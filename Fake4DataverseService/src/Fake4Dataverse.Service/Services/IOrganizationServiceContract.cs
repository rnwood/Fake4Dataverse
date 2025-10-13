using CoreWCF;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Service.Services;

/// <summary>
/// WCF Service Contract matching the Microsoft Dynamics 365/Dataverse Organization Service.
/// This interface defines the SOAP operations exposed by the service.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// The IOrganizationService interface is the core interface for interacting with Dataverse,
/// providing methods for CRUD operations and executing organization requests.
/// 
/// Microsoft Dynamics 365 uses SOAP 1.1/1.2 for the Organization Service with endpoints at:
/// /XRMServices/2011/Organization.svc
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview
/// </summary>
[ServiceContract(Namespace = "http://schemas.microsoft.com/xrm/2011/Contracts/Services")]
public interface IOrganizationServiceContract
{
    /// <summary>
    /// Creates a new entity record.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.create
    /// </summary>
    [OperationContract]
    Guid Create(Entity entity);

    /// <summary>
    /// Retrieves an entity record by ID.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.retrieve
    /// </summary>
    [OperationContract]
    Entity Retrieve(string entityName, Guid id, ColumnSet columnSet);

    /// <summary>
    /// Updates an existing entity record.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.update
    /// </summary>
    [OperationContract]
    void Update(Entity entity);

    /// <summary>
    /// Deletes an entity record.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.delete
    /// </summary>
    [OperationContract]
    void Delete(string entityName, Guid id);

    /// <summary>
    /// Associates two entity records.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.associate
    /// </summary>
    [OperationContract]
    void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities);

    /// <summary>
    /// Disassociates two entity records.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.disassociate
    /// </summary>
    [OperationContract]
    void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities);

    /// <summary>
    /// Retrieves multiple entity records.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.retrievemultiple
    /// </summary>
    [OperationContract]
    EntityCollection RetrieveMultiple(QueryBase query);

    /// <summary>
    /// Executes an organization request.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice.execute
    /// </summary>
    [OperationContract]
    OrganizationResponse Execute(OrganizationRequest request);
}
