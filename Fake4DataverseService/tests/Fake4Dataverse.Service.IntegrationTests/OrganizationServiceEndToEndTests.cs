#if !NET462
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Crm.Sdk.Messages;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.PowerPlatform.Dataverse.Client;
using Xunit;
using System.Diagnostics;

namespace Fake4Dataverse.Service.IntegrationTests;

/// <summary>
/// End-to-end integration tests that verify the Fake4DataverseService works with standard WCF/SOAP clients.
/// These tests start the service and connect using the IOrganizationService interface directly via WCF.
/// 
/// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.iorganizationservice
/// The IOrganizationService interface is the core interface for interacting with Dataverse.
/// 
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/org-service/overview
/// The Organization Service uses SOAP 1.1/1.2 protocol via WCF bindings.
/// </summary>
[Collection("Service Integration Tests")]
public class OrganizationServiceEndToEndTests
{
    private readonly ServiceFixture _fixture;
    private IOrganizationService? _organizationService;

    public OrganizationServiceEndToEndTests(ServiceFixture fixture)
    {
        _fixture = fixture ?? throw new ArgumentNullException(nameof(fixture));
        // Create WCF client for IOrganizationService
        _organizationService = CreateOrganizationServiceClient();
    }

    /// <summary>
    /// Creates a WCF channel to connect to the IOrganizationService endpoint.
    /// This uses the same approach that CrmServiceClient uses internally.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-create-a-wcf-client
    /// </summary>
    private IOrganizationService CreateOrganizationServiceClient()
    {
        var binding = new BasicHttpBinding
        {
            MaxReceivedMessageSize = 2147483647,
            MaxBufferSize = 2147483647,
            MaxBufferPoolSize = 2147483647,
            SendTimeout = TimeSpan.FromMinutes(20),
            ReceiveTimeout = TimeSpan.FromMinutes(20)
        };

        var serviceUrl = $"{_fixture.ServiceUrl}/XRMServices/2011/Organization.svc";
        var endpoint = new EndpointAddress(serviceUrl);
        var factory = new ChannelFactory<IOrganizationService>(binding, endpoint);

        // Disable authentication for local testing
        factory.Credentials.Windows.AllowedImpersonationLevel =
            System.Security.Principal.TokenImpersonationLevel.Impersonation;

        return factory.CreateChannel();
    }

    [Fact]
    public void Should_Connect_To_Service_Via_WCF()
    {
        // Assert - Verify we have a valid service proxy
        Assert.NotNull(_organizationService);
    }

    [Fact]
    public void Should_Create_Entity_Via_WCF()
    {
        // Arrange
        var account = new Entity("account");
        account["name"] = "Contoso Ltd - E2E Test";
        account["revenue"] = new Money(250000m);
        account["numberofemployees"] = 150;

        // Act - Create entity via WCF channel
        var accountId = _organizationService!.Create(account);

        // Assert
        Assert.NotEqual(Guid.Empty, accountId);
    }

    [Fact]
    public void Should_Retrieve_Entity_Via_WCF()
    {
        // Arrange - First create an entity
        var account = new Entity("account");
        account["name"] = "Fabrikam Inc - E2E";
        account["revenue"] = new Money(500000m);
        var accountId = _organizationService!.Create(account);

        // Act - Retrieve the entity via WCF
        var retrievedAccount = _organizationService.Retrieve("account", accountId, 
            new ColumnSet("name", "revenue"));

        // Assert
        Assert.NotNull(retrievedAccount);
        Assert.Equal("Fabrikam Inc - E2E", retrievedAccount["name"]);
        Assert.Equal(500000m, ((Money)retrievedAccount["revenue"]).Value);
    }

    [Fact]
    public void Should_Update_Entity_Via_WCF()
    {
        // Arrange
        var account = new Entity("account");
        account["name"] = "Original Name - E2E";
        var accountId = _organizationService!.Create(account);

        // Act - Update via WCF
        var updateAccount = new Entity("account", accountId);
        updateAccount["name"] = "Updated Name via WCF";
        _organizationService.Update(updateAccount);

        // Assert - Retrieve and verify
        var retrievedAccount = _organizationService.Retrieve("account", accountId, 
            new ColumnSet("name"));
        Assert.Equal("Updated Name via WCF", retrievedAccount["name"]);
    }

    [Fact]
    public void Should_Delete_Entity_Via_WCF()
    {
        // Arrange
        var account = new Entity("account");
        account["name"] = "To Be Deleted - E2E";
        var accountId = _organizationService!.Create(account);

        // Act - Delete via WCF
        _organizationService.Delete("account", accountId);

        // Assert - Should throw when trying to retrieve deleted entity
        Assert.Throws<FaultException>(() =>
        {
            _organizationService.Retrieve("account", accountId, new ColumnSet("name"));
        });
    }

    [Fact]
    public void Should_RetrieveMultiple_With_QueryExpression_Via_WCF()
    {
        // Arrange - Create multiple entities
        var account1 = new Entity("account") 
        { 
            ["name"] = "High Revenue 1 - E2E", 
            ["revenue"] = new Money(800000m) 
        };
        var account2 = new Entity("account") 
        { 
            ["name"] = "High Revenue 2 - E2E", 
            ["revenue"] = new Money(900000m) 
        };
        var account3 = new Entity("account") 
        { 
            ["name"] = "Low Revenue - E2E", 
            ["revenue"] = new Money(50000m) 
        };

        _organizationService!.Create(account1);
        _organizationService.Create(account2);
        _organizationService.Create(account3);

        // Act - Query using QueryExpression via WCF
        var query = new QueryExpression("account");
        query.ColumnSet.AddColumns("name", "revenue");
        query.Criteria.AddCondition("name", ConditionOperator.Like, "%E2E%");
        query.Criteria.AddCondition("revenue", ConditionOperator.GreaterThan, 100000m);

        var results = _organizationService.RetrieveMultiple(query);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Entities.Count >= 2, 
            $"Expected at least 2 high-revenue accounts, found {results.Entities.Count}");
    }

    [Fact]
    public void Should_Execute_WhoAmI_Request_Via_WCF()
    {
        // Arrange - Create WhoAmI request
        // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.whoamirequest
        // WhoAmI returns the SystemUser ID of the currently authenticated user
        var whoAmIRequest = new WhoAmIRequest();

        // Act - Execute via WCF
        var whoAmIResponse = (WhoAmIResponse)_organizationService!.Execute(whoAmIRequest);

        // Assert
        Assert.NotNull(whoAmIResponse);
        Assert.NotEqual(Guid.Empty, whoAmIResponse.UserId);
        Assert.NotEqual(Guid.Empty, whoAmIResponse.BusinessUnitId);
        Assert.NotEqual(Guid.Empty, whoAmIResponse.OrganizationId);
    }

    [Fact]
    public void Should_Query_Using_FetchXml_Via_WCF()
    {
        // Arrange - Create test data
        var account1 = new Entity("account") 
        { 
            ["name"] = "FetchXml E2E Test 1", 
            ["revenue"] = new Money(300000m) 
        };
        var account2 = new Entity("account") 
        { 
            ["name"] = "FetchXml E2E Test 2", 
            ["revenue"] = new Money(400000m) 
        };
        _organizationService!.Create(account1);
        _organizationService.Create(account2);

        // Act - Query using FetchXml via WCF
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/fetchxml/overview
        // FetchXml is an XML-based query language used in Dataverse
        var fetchXml = @"
            <fetch>
                <entity name='account'>
                    <attribute name='name' />
                    <attribute name='revenue' />
                    <filter>
                        <condition attribute='name' operator='like' value='%FetchXml E2E%' />
                    </filter>
                </entity>
            </fetch>";

        var query = new FetchExpression(fetchXml);
        var results = _organizationService.RetrieveMultiple(query);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Entities.Count >= 2, 
            $"Expected at least 2 accounts matching FetchXml, found {results.Entities.Count}");
    }

    [Fact]
    public void Should_Handle_Multiple_Operations_In_Sequence()
    {
        // This test verifies that the service can handle multiple operations
        // in a row without issues, simulating real-world usage patterns

        // Create
        var contact = new Entity("contact");
        contact["firstname"] = "John";
        contact["lastname"] = "Doe";
        contact["emailaddress1"] = "john.doe@example.com";
        var contactId = _organizationService!.Create(contact);
        Assert.NotEqual(Guid.Empty, contactId);

        // Retrieve
        var retrievedContact = _organizationService.Retrieve("contact", contactId, 
            new ColumnSet("firstname", "lastname", "emailaddress1"));
        Assert.Equal("John", retrievedContact["firstname"]);

        // Update
        var updateContact = new Entity("contact", contactId);
        updateContact["lastname"] = "Smith";
        _organizationService.Update(updateContact);

        // Retrieve again to verify update
        retrievedContact = _organizationService.Retrieve("contact", contactId, 
            new ColumnSet("lastname"));
        Assert.Equal("Smith", retrievedContact["lastname"]);

        // Delete
        _organizationService.Delete("contact", contactId);

        // Verify deletion
        Assert.Throws<FaultException>(() =>
        {
            _organizationService.Retrieve("contact", contactId, new ColumnSet("firstname"));
        });
    }

    [Fact]
    public void Should_Support_Complex_Queries_With_Multiple_Conditions()
    {
        // Arrange - Create varied test data
        _organizationService!.Create(new Entity("account") 
        { 
            ["name"] = "Large Corp E2E", 
            ["revenue"] = new Money(1000000m),
            ["numberofemployees"] = 500,
            ["creditonhold"] = false
        });
        _organizationService.Create(new Entity("account") 
        { 
            ["name"] = "Small Business E2E", 
            ["revenue"] = new Money(50000m),
            ["numberofemployees"] = 10,
            ["creditonhold"] = false
        });
        _organizationService.Create(new Entity("account") 
        { 
            ["name"] = "Medium Corp E2E", 
            ["revenue"] = new Money(500000m),
            ["numberofemployees"] = 200,
            ["creditonhold"] = true
        });

        // Act - Complex query with multiple conditions
        var query = new QueryExpression("account");
        query.ColumnSet.AddColumns("name", "revenue", "numberofemployees");
        query.Criteria.AddCondition("name", ConditionOperator.Like, "%E2E%");
        query.Criteria.AddCondition("revenue", ConditionOperator.GreaterThan, 100000m);
        query.Criteria.AddCondition("numberofemployees", ConditionOperator.GreaterThan, 50);
        query.Criteria.AddCondition("creditonhold", ConditionOperator.Equal, false);

        var results = _organizationService.RetrieveMultiple(query);

        // Assert
        Assert.NotNull(results);
        Assert.True(results.Entities.Count >= 1, 
            $"Expected at least 1 account matching all criteria, found {results.Entities.Count}");
        
        // Verify all results meet the criteria
        foreach (var entity in results.Entities)
        {
            Assert.True(((Money)entity["revenue"]).Value > 100000m);
            Assert.True((int)entity["numberofemployees"] > 50);
        }
    }

    /// <summary>
    /// Demonstrates connecting via ServiceClient using a direct connection string.
    /// ServiceClient is the modern replacement for CrmServiceClient.
    /// 
    /// Note: ServiceClient typically requires OAuth authentication. For testing scenarios,
    /// using WCF channels directly (as shown in other tests) is simpler and doesn't require
    /// authentication setup. However, this test demonstrates that the service endpoint
    /// structure is compatible with ServiceClient if authentication is configured.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/xrm-tooling/use-connection-strings-xrm-tooling-connect
    /// ServiceClient supports connection strings with ServiceUri parameter.
    /// </summary>
    [Fact(Skip = "ServiceClient requires OAuth authentication which is complex to set up for testing. Use WCF channels instead (see other tests).")]
    public void Should_Work_With_ServiceClient_When_Auth_Configured()
    {
        // This test is skipped because ServiceClient requires OAuth authentication setup.
        // For real-world usage, users would need to:
        // 1. Set up OAuth app registration in Azure AD
        // 2. Configure appropriate permissions
        // 3. Use connection string like: "AuthType=OAuth;Url=http://localhost:5558;..."
        
        // For testing purposes, using WCF ChannelFactory<IOrganizationService> (as shown in 
        // CreateOrganizationServiceClient method) is much simpler and doesn't require authentication.
        
        // Example of what ServiceClient usage would look like (when auth is configured):
        // var connectionString = "AuthType=OAuth;Url=http://localhost:5558;ClientId=...;RedirectUri=...;LoginPrompt=Auto";
        // using (var serviceClient = new ServiceClient(connectionString))
        // {
        //     if (serviceClient.IsReady)
        //     {
        //         var account = new Entity("account") { ["name"] = "Test Account" };
        //         var accountId = serviceClient.Create(account);
        //         Assert.NotEqual(Guid.Empty, accountId);
        //     }
        // }
    }

    /// <summary>
    /// Tests RetrieveVersion request via Execute method.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveversionrequest
    /// RetrieveVersion returns version information about the Dataverse organization.
    /// </summary>
    [Fact]
    public void Should_Execute_RetrieveVersion_Request_Via_WCF()
    {
        // Arrange
        var retrieveVersionRequest = new RetrieveVersionRequest();

        // Act
        var retrieveVersionResponse = (RetrieveVersionResponse)_organizationService!.Execute(retrieveVersionRequest);

        // Assert
        Assert.NotNull(retrieveVersionResponse);
        Assert.NotNull(retrieveVersionResponse.Version);
        Assert.False(string.IsNullOrEmpty(retrieveVersionResponse.Version));
    }

    /// <summary>
    /// Tests SetState request via Execute method.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.setstaterequest
    /// SetState changes the state and status of an entity.
    /// </summary>
    [Fact]
    public void Should_Execute_SetState_Request_Via_WCF()
    {
        // Arrange - Create an account
        var account = new Entity("account") { ["name"] = "SetState Test Account" };
        var accountId = _organizationService!.Create(account);

        // Act - Change account state
        var setStateRequest = new SetStateRequest
        {
            EntityMoniker = new EntityReference("account", accountId),
            State = new OptionSetValue(1), // Inactive
            Status = new OptionSetValue(2)  // Inactive status code
        };
        var setStateResponse = (SetStateResponse)_organizationService.Execute(setStateRequest);

        // Assert
        Assert.NotNull(setStateResponse);
    }

    /// <summary>
    /// Tests Assign request via Execute method.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.assignrequest
    /// Assign changes the owner of a record to another user or team.
    /// </summary>
    [Fact]
    public void Should_Execute_Assign_Request_Via_WCF()
    {
        // Arrange - Create an account
        var account = new Entity("account") { ["name"] = "Assign Test Account" };
        var accountId = _organizationService!.Create(account);

        // Act - Assign to a new owner (using a fake user ID)
        var newOwnerId = Guid.NewGuid();
        var assignRequest = new AssignRequest
        {
            Target = new EntityReference("account", accountId),
            Assignee = new EntityReference("systemuser", newOwnerId)
        };
        var assignResponse = (AssignResponse)_organizationService.Execute(assignRequest);

        // Assert
        Assert.NotNull(assignResponse);
        
        // Verify the assignment by retrieving the record
        var retrievedAccount = _organizationService.Retrieve("account", accountId, new ColumnSet("ownerid"));
        Assert.Equal(newOwnerId, ((EntityReference)retrievedAccount["ownerid"]).Id);
    }

    /// <summary>
    /// Tests ExecuteMultiple request via Execute method.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.executemultiplerequest
    /// ExecuteMultiple allows batching multiple requests in a single call.
    /// </summary>
    [Fact]
    public void Should_Execute_ExecuteMultiple_Request_Via_WCF()
    {
        // Arrange - Create multiple requests
        var executeMultipleRequest = new ExecuteMultipleRequest
        {
            Settings = new ExecuteMultipleSettings
            {
                ContinueOnError = true,
                ReturnResponses = true
            },
            Requests = new OrganizationRequestCollection()
        };

        // Add multiple create requests
        for (int i = 0; i < 3; i++)
        {
            var account = new Entity("account") { ["name"] = $"Batch Account {i}" };
            var createRequest = new CreateRequest { Target = account };
            executeMultipleRequest.Requests.Add(createRequest);
        }

        // Act
        var executeMultipleResponse = (ExecuteMultipleResponse)_organizationService!.Execute(executeMultipleRequest);

        // Assert
        Assert.NotNull(executeMultipleResponse);
        Assert.NotNull(executeMultipleResponse.Responses);
        Assert.Equal(3, executeMultipleResponse.Responses.Count);
        
        // Verify all succeeded
        foreach (var response in executeMultipleResponse.Responses)
        {
            Assert.Null(response.Fault);
            Assert.NotNull(response.Response);
        }
    }

    /// <summary>
    /// Tests Upsert request via Execute method.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.messages.upsertrequest
    /// Upsert inserts or updates a record based on alternate key values.
    /// </summary>
    [Fact]
    public void Should_Execute_Upsert_Request_Via_WCF()
    {
        // Arrange - Create entity with alternate key
        var account = new Entity("account") { ["name"] = "Upsert Test Account" };
        var accountId = _organizationService!.Create(account);

        // Act - Upsert the same record (should update)
        account.Id = accountId;
        account["revenue"] = new Money(500000m);
        var upsertRequest = new UpsertRequest { Target = account };
        var upsertResponse = (UpsertResponse)_organizationService.Execute(upsertRequest);

        // Assert
        Assert.NotNull(upsertResponse);
        Assert.False(upsertResponse.RecordCreated); // Should be false (updated existing)
        Assert.Equal(accountId, upsertResponse.Target.Id);
        
        // Verify the update
        var retrievedAccount = _organizationService.Retrieve("account", accountId, new ColumnSet("revenue"));
        Assert.Equal(500000m, ((Money)retrievedAccount["revenue"]).Value);
    }
}
#endif
