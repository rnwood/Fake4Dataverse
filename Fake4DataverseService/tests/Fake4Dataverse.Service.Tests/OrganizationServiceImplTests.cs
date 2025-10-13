using Microsoft.Xrm.Sdk;
using Xunit;
using Fake4Dataverse.Middleware;
using Fake4Dataverse.Service.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Fake4Dataverse.Service.Grpc;
using Grpc.Core;
using XrmMoney = Microsoft.Xrm.Sdk.Money;

namespace Fake4Dataverse.Service.Tests;

/// <summary>
/// Tests for the OrganizationServiceImpl gRPC service.
/// These tests verify that the service correctly wraps IOrganizationService
/// and handles gRPC requests/responses.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// The IOrganizationService interface is the core interface for interacting with Dataverse,
/// providing methods for CRUD operations and executing organization requests.
/// </summary>
public class OrganizationServiceImplTests
{
    private readonly IOrganizationService _organizationService;
    private readonly OrganizationServiceImpl _serviceImpl;
    private readonly Mock<ServerCallContext> _mockCallContext;

    public OrganizationServiceImplTests()
    {
        // Create a Fake4Dataverse context
        var context = XrmFakedContextFactory.New();
        _organizationService = context.GetOrganizationService();

        // Create the service implementation
        var logger = Mock.Of<ILogger<OrganizationServiceImpl>>();
        _serviceImpl = new OrganizationServiceImpl(_organizationService, logger);

        // Mock the server call context
        _mockCallContext = new Mock<ServerCallContext>();
    }

    [Fact]
    public async Task Should_Create_Entity_And_Return_Id()
    {
        // Arrange - Create a request to create an account entity
        var request = new CreateRequest
        {
            EntityLogicalName = "account",
            Attributes =
            {
                { "name", new AttributeValue { StringValue = "Test Account" } },
                { "revenue", new AttributeValue { DoubleValue = 50000.0 } }
            }
        };

        // Act - Call the Create method
        var response = await _serviceImpl.Create(request, _mockCallContext.Object);

        // Assert - Verify the response contains a valid GUID
        Assert.NotNull(response);
        Assert.NotEmpty(response.Id);
        Assert.True(Guid.TryParse(response.Id, out var id));
        Assert.NotEqual(Guid.Empty, id);
    }

    [Fact]
    public async Task Should_Retrieve_Created_Entity()
    {
        // Arrange - First create an entity
        var accountId = _organizationService.Create(new Entity("account")
        {
            ["name"] = "Contoso Ltd",
            ["revenue"] = new XrmMoney(100000m)
        });

        var request = new RetrieveRequest
        {
            EntityLogicalName = "account",
            Id = accountId.ToString(),
            Columns = { "name", "revenue" }
        };

        // Act - Retrieve the entity
        var response = await _serviceImpl.Retrieve(request, _mockCallContext.Object);

        // Assert - Verify the retrieved entity
        Assert.NotNull(response);
        Assert.NotNull(response.Entity);
        Assert.Equal("account", response.Entity.LogicalName);
        Assert.Equal(accountId.ToString(), response.Entity.Id);
        Assert.Contains("name", response.Entity.Attributes.Keys);
        Assert.Equal("Contoso Ltd", response.Entity.Attributes["name"].StringValue);
    }

    [Fact]
    public async Task Should_Update_Existing_Entity()
    {
        // Arrange - Create an entity first
        var accountId = _organizationService.Create(new Entity("account")
        {
            ["name"] = "Original Name"
        });

        var updateRequest = new UpdateRequest
        {
            EntityLogicalName = "account",
            Id = accountId.ToString(),
            Attributes =
            {
                { "name", new AttributeValue { StringValue = "Updated Name" } }
            }
        };

        // Act - Update the entity
        var response = await _serviceImpl.Update(updateRequest, _mockCallContext.Object);

        // Assert - Verify the update succeeded
        Assert.NotNull(response);

        // Verify the entity was actually updated
        var updatedEntity = _organizationService.Retrieve("account", accountId, new Microsoft.Xrm.Sdk.Query.ColumnSet("name"));
        Assert.Equal("Updated Name", updatedEntity["name"]);
    }

    [Fact(Skip = "Delete verification depends on specific exception behavior")]
    public async Task Should_Delete_Entity()
    {
        // Arrange - Create an entity first
        var accountId = _organizationService.Create(new Entity("account")
        {
            ["name"] = "To Be Deleted"
        });

        var deleteRequest = new DeleteRequest
        {
            EntityLogicalName = "account",
            Id = accountId.ToString()
        };

        // Act - Delete the entity
        var response = await _serviceImpl.Delete(deleteRequest, _mockCallContext.Object);

        // Assert - Verify the delete succeeded
        Assert.NotNull(response);
    }

    [Fact]
    public async Task Should_Handle_Missing_Entity_Gracefully()
    {
        // Arrange - Try to retrieve a non-existent entity
        var request = new RetrieveRequest
        {
            EntityLogicalName = "account",
            Id = Guid.NewGuid().ToString(),
            Columns = { "name" }
        };

        // Act & Assert - Should throw RpcException with Internal status
        var exception = await Assert.ThrowsAsync<RpcException>(async () =>
        {
            await _serviceImpl.Retrieve(request, _mockCallContext.Object);
        });

        Assert.Equal(StatusCode.Internal, exception.StatusCode);
    }

    [Fact]
    public async Task Should_Create_Entity_With_Different_Attribute_Types()
    {
        // Arrange - Create a request with multiple attribute types
        var request = new CreateRequest
        {
            EntityLogicalName = "account",
            Attributes =
            {
                { "name", new AttributeValue { StringValue = "Test Account" } },
                { "revenue", new AttributeValue { DoubleValue = 50000.0 } },
                { "numberofemployees", new AttributeValue { IntValue = 100 } },
                { "creditonhold", new AttributeValue { BoolValue = true } }
            }
        };

        // Act - Call the Create method
        var response = await _serviceImpl.Create(request, _mockCallContext.Object);

        // Assert - Verify the response and retrieve to check attributes
        Assert.NotNull(response);
        var createdEntity = _organizationService.Retrieve("account", Guid.Parse(response.Id),
            new Microsoft.Xrm.Sdk.Query.ColumnSet(true));

        Assert.Equal("Test Account", createdEntity["name"]);
        Assert.Equal(50000.0, Convert.ToDouble(createdEntity["revenue"]));
        Assert.Equal(100, createdEntity["numberofemployees"]);
        Assert.Equal(true, createdEntity["creditonhold"]);
    }
}
