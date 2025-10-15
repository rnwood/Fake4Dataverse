using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Service.Services;
using Microsoft.Extensions.Logging;
using Moq;
using XrmMoney = Microsoft.Xrm.Sdk.Money;
using Fake4Dataverse.Abstractions.Integrity;
using Fake4Dataverse.Integrity;

namespace Fake4Dataverse.Service.Tests;

/// <summary>
/// Tests for the OrganizationServiceImpl WCF/SOAP service.
/// These tests verify that the service correctly wraps IOrganizationService
/// and implements all CRUD operations matching Microsoft Dynamics 365/Dataverse.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// The IOrganizationService interface is the core interface for interacting with Dataverse,
/// providing methods for CRUD operations and executing organization requests.
/// 
/// The service uses SOAP/WCF protocol at endpoint: /XRMServices/2011/Organization.svc
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview
/// </summary>
public class OrganizationServiceImplTests
{
    private readonly IOrganizationService _organizationService;
    private readonly OrganizationServiceImpl _serviceImpl;

    public OrganizationServiceImplTests()
    {
        // Create a Fake4Dataverse context with validation disabled for now
        // TODO: Load required metadata and enable validation
        var context = XrmFakedContextFactory.New(new IntegrityOptions 
        { 
            ValidateEntityReferences = false,
            ValidateAttributeTypes = false 
        });
        _organizationService = context.GetOrganizationService();

        // Create the WCF service implementation
        var logger = Mock.Of<ILogger<OrganizationServiceImpl>>();
        _serviceImpl = new OrganizationServiceImpl(_organizationService, logger);
    }

    [Fact]
    public void Should_Create_Entity_And_Return_Id()
    {
        // Arrange - Create an account entity
        var account = new Entity("account");
        account["name"] = "Test Account";
        account["revenue"] = new XrmMoney(50000m);

        // Act - Call the Create method
        var id = _serviceImpl.Create(account);

        // Assert - Verify the response contains a valid GUID
        Assert.NotEqual(Guid.Empty, id);

        // Verify the entity was actually created
        var createdEntity = _organizationService.Retrieve("account", id, new ColumnSet(true));
        Assert.Equal("Test Account", createdEntity["name"]);
    }

    [Fact]
    public void Should_Retrieve_Created_Entity()
    {
        // Arrange - First create an entity
        var accountId = _organizationService.Create(new Entity("account")
        {
            ["name"] = "Contoso Ltd",
            ["revenue"] = new XrmMoney(100000m)
        });

        // Act - Retrieve the entity
        var retrievedEntity = _serviceImpl.Retrieve("account", accountId, new ColumnSet("name", "revenue"));

        // Assert - Verify the retrieved entity
        Assert.NotNull(retrievedEntity);
        Assert.Equal("account", retrievedEntity.LogicalName);
        Assert.Equal(accountId, retrievedEntity.Id);
        Assert.Equal("Contoso Ltd", retrievedEntity["name"]);
    }

    [Fact]
    public void Should_Update_Existing_Entity()
    {
        // Arrange - Create an entity first
        var accountId = _organizationService.Create(new Entity("account")
        {
            ["name"] = "Original Name"
        });

        var updateEntity = new Entity("account", accountId);
        updateEntity["name"] = "Updated Name";

        // Act - Update the entity
        _serviceImpl.Update(updateEntity);

        // Assert - Verify the entity was actually updated
        var updatedEntity = _organizationService.Retrieve("account", accountId, new ColumnSet("name"));
        Assert.Equal("Updated Name", updatedEntity["name"]);
    }

    [Fact]
    public void Should_Delete_Entity()
    {
        // Arrange - Create an entity first
        var accountId = _organizationService.Create(new Entity("account")
        {
            ["name"] = "To Be Deleted"
        });

        // Act - Delete the entity
        _serviceImpl.Delete("account", accountId);

        // Assert - Verify the entity no longer exists by attempting to retrieve it
        // In Fake4Dataverse, deleted entities are removed from the in-memory store
        Assert.Throws<CoreWCF.FaultException>(() =>
        {
            _serviceImpl.Retrieve("account", accountId, new ColumnSet("name"));
        });
    }

    [Fact]
    public void Should_RetrieveMultiple_Entities()
    {
        // Arrange - Create multiple entities
        _organizationService.Create(new Entity("account") { ["name"] = "Account 1", ["revenue"] = new XrmMoney(100000m) });
        _organizationService.Create(new Entity("account") { ["name"] = "Account 2", ["revenue"] = new XrmMoney(200000m) });
        _organizationService.Create(new Entity("account") { ["name"] = "Account 3", ["revenue"] = new XrmMoney(50000m) });

        var query = new QueryExpression("account");
        query.ColumnSet.AddColumns("name", "revenue");
        query.Criteria.AddCondition("revenue", ConditionOperator.GreaterThan, 75000m);

        // Act - Retrieve multiple entities
        var result = _serviceImpl.RetrieveMultiple(query);

        // Assert - Verify the results
        Assert.NotNull(result);
        Assert.Equal(2, result.Entities.Count); // Only 2 accounts have revenue > 75000
    }

    [Fact]
    public void Should_Handle_Missing_Entity_Gracefully()
    {
        // Arrange - Try to retrieve a non-existent entity
        var nonExistentId = Guid.NewGuid();

        // Act & Assert - Should throw FaultException
        Assert.Throws<CoreWCF.FaultException>(() =>
        {
            _serviceImpl.Retrieve("account", nonExistentId, new ColumnSet("name"));
        });
    }

    [Fact]
    public void Should_Create_Entity_With_Different_Attribute_Types()
    {
        // Arrange - Create an entity with multiple attribute types
        var account = new Entity("account");
        account["name"] = "Test Account";
        account["revenue"] = new XrmMoney(50000m);
        account["numberofemployees"] = 100;
        account["creditonhold"] = true;

        // Act - Create the entity
        var id = _serviceImpl.Create(account);

        // Assert - Verify the response and retrieve to check attributes
        Assert.NotEqual(Guid.Empty, id);
        var createdEntity = _organizationService.Retrieve("account", id, new ColumnSet(true));

        Assert.Equal("Test Account", createdEntity["name"]);
        Assert.Equal(100, createdEntity["numberofemployees"]);
        Assert.Equal(true, createdEntity["creditonhold"]);
    }

    [Fact]
    public void Should_Execute_WhoAmI_Request()
    {
        // Arrange - Create a WhoAmI request
        var request = new Microsoft.Crm.Sdk.Messages.WhoAmIRequest();

        // Act - Execute the request
        var response = (Microsoft.Crm.Sdk.Messages.WhoAmIResponse)_serviceImpl.Execute(request);

        // Assert - Verify the response
        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.UserId);
    }
}
