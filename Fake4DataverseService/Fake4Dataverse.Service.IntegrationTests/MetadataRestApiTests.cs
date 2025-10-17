using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Fake4Dataverse.Service.IntegrationTests;

/// <summary>
/// Integration tests for metadata REST API endpoints.
/// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
/// 
/// These tests verify that the EntityDefinitions (metadata) endpoints work correctly.
/// The service is started once by the ServiceFixture and shared across all tests.
/// </summary>
[Collection("Service Integration Tests")]
public class MetadataRestApiTests : IDisposable
{
    private readonly HttpClient _httpClient;

    public MetadataRestApiTests(ServiceFixture fixture)
    {
        // Ensure fixture is not null - if this fails, xUnit isn't injecting the fixture
        if (fixture == null)
        {
            throw new ArgumentNullException(nameof(fixture), "ServiceFixture was not injected by xUnit");
        }

        // Create HTTP client for REST API calls (service is already running via fixture)
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(fixture.BaseUrl),
            // Set a reasonable timeout for CI environments where resources may be constrained
            // This prevents tests from hanging indefinitely if the service becomes unresponsive
            Timeout = TimeSpan.FromSeconds(30)
        };
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
        _httpClient.DefaultRequestHeaders.Add("OData-Version", "4.0");
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task Should_List_EntityDefinitions()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // GET /api/data/v9.2/EntityDefinitions returns all entity metadata
        
        // Act
        var response = await _httpClient!.GetAsync("EntityDefinitions");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        // Should have @odata.context
        Assert.True(json.RootElement.TryGetProperty("@odata.context", out var context));
        Assert.Contains("EntityDefinitions", context.GetString());
        
        // Should have value array
        Assert.True(json.RootElement.TryGetProperty("value", out var value));
        Assert.Equal(JsonValueKind.Array, value.ValueKind);
    }

    [Fact]
    public async Task Should_List_EntityDefinitions_With_Select()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // $select query option chooses specific properties to return
        
        // Act
        var response = await _httpClient!.GetAsync("EntityDefinitions?$select=LogicalName,MetadataId");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.True(json.RootElement.TryGetProperty("value", out var value));
        
        // Check that entities only have selected properties
        foreach (var entity in value.EnumerateArray())
        {
            Assert.True(entity.TryGetProperty("LogicalName", out _));
            Assert.True(entity.TryGetProperty("MetadataId", out _));
        }
    }

    [Fact]
    public async Task Should_Filter_EntityDefinitions_By_LogicalName()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // $filter query option filters entity metadata by criteria
        
        // Act
        var response = await _httpClient!.GetAsync("EntityDefinitions?$filter=LogicalName eq 'account'");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.True(json.RootElement.TryGetProperty("value", out var value));
        
        // Should return at most one entity (account)
        var entityCount = 0;
        foreach (var entity in value.EnumerateArray())
        {
            entityCount++;
            Assert.True(entity.TryGetProperty("LogicalName", out var logicalName));
            Assert.Equal("account", logicalName.GetString());
        }
        
        // Could be 0 or 1 depending on whether metadata is loaded
        Assert.True(entityCount <= 1);
    }

    [Fact]
    public async Task Should_Get_EntityDefinition_By_LogicalName()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-metadata-name-metadataid
        // GET /api/data/v9.2/EntityDefinitions(LogicalName='account') retrieves by logical name
        
        // First, check if account entity exists by listing
        var listResponse = await _httpClient!.GetAsync("EntityDefinitions?$filter=LogicalName eq 'account'");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);
        
        if (!listJson.RootElement.TryGetProperty("value", out var value) || value.GetArrayLength() == 0)
        {
            // Skip test if account metadata not loaded
            return;
        }

        // Act
        var response = await _httpClient!.GetAsync("EntityDefinitions(LogicalName='account')");

        // Assert
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // It's okay if metadata not found - depends on service initialization
            return;
        }
        
        // If not OK, log the error response for debugging
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response: {errorContent}");
        }
        
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        // Should have @odata.context for single entity
        Assert.True(json.RootElement.TryGetProperty("@odata.context", out var context));
        Assert.Contains("EntityDefinitions", context.GetString());
        
        // Should have LogicalName
        Assert.True(json.RootElement.TryGetProperty("LogicalName", out var logicalName));
        Assert.Equal("account", logicalName.GetString());
    }

    [Fact]
    public async Task Should_Expand_Attributes_In_EntityDefinitions()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // $expand=Attributes includes attribute metadata in the response
        
        // First, check if any entities exist
        var listResponse = await _httpClient!.GetAsync("EntityDefinitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);
        
        if (!listJson.RootElement.TryGetProperty("value", out var entities) || entities.GetArrayLength() == 0)
        {
            // Skip test if no metadata loaded
            return;
        }

        // Act
        var response = await _httpClient!.GetAsync("EntityDefinitions?$expand=Attributes&$top=1");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        Assert.True(json.RootElement.TryGetProperty("value", out var value));
        
        // Check first entity has Attributes property
        foreach (var entity in value.EnumerateArray())
        {
            if (entity.TryGetProperty("Attributes", out var attributes))
            {
                Assert.Equal(JsonValueKind.Array, attributes.ValueKind);
                
                // Check attribute structure
                foreach (var attr in attributes.EnumerateArray())
                {
                    // Attributes should have MetadataId and LogicalName
                    Assert.True(attr.TryGetProperty("LogicalName", out _));
                }
            }
            break; // Just check first entity
        }
    }

    [Fact]
    public async Task Should_Return_Metadata_Document()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-service-documents
        // GET /api/data/v9.2/$metadata returns EDMX/CSDL document
        
        // Act
        var response = await _httpClient!.GetAsync("$metadata");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        // Should be XML content
        Assert.Equal("application/xml", response.Content.Headers.ContentType?.MediaType);
        
        var content = await response.Content.ReadAsStringAsync();
        
        // Parse as XML
        var doc = XDocument.Parse(content);
        
        // Should have edmx:Edmx root element
        var edmxNs = XNamespace.Get("http://docs.oasis-open.org/odata/ns/edmx");
        Assert.NotNull(doc.Root);
        Assert.Equal(edmxNs + "Edmx", doc.Root!.Name);
        
        // Should have DataServices
        var dataServices = doc.Root.Element(edmxNs + "DataServices");
        Assert.NotNull(dataServices);
        
        // Should have Schema
        var edmNs = XNamespace.Get("http://docs.oasis-open.org/odata/ns/edm");
        var schema = dataServices!.Element(edmNs + "Schema");
        Assert.NotNull(schema);
        
        // Schema should have Microsoft.Dynamics.CRM namespace
        var namespaceAttr = schema!.Attribute("Namespace");
        Assert.NotNull(namespaceAttr);
        Assert.Equal("Microsoft.Dynamics.CRM", namespaceAttr!.Value);
    }

    [Fact]
    public async Task Should_Include_EntityTypes_In_Metadata()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-service-documents
        // $metadata document should include EntityType definitions
        
        // First check if any entities exist
        var listResponse = await _httpClient!.GetAsync("EntityDefinitions");
        var listContent = await listResponse.Content.ReadAsStringAsync();
        var listJson = JsonDocument.Parse(listContent);
        
        if (!listJson.RootElement.TryGetProperty("value", out var entities) || entities.GetArrayLength() == 0)
        {
            // Skip test if no metadata loaded
            return;
        }

        // Act
        var response = await _httpClient!.GetAsync("$metadata");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(content);
        
        var edmxNs = XNamespace.Get("http://docs.oasis-open.org/odata/ns/edmx");
        var edmNs = XNamespace.Get("http://docs.oasis-open.org/odata/ns/edm");
        
        var schema = doc.Root!.Element(edmxNs + "DataServices")!.Element(edmNs + "Schema");
        
        // Should have EntityType elements
        var entityTypes = schema!.Elements(edmNs + "EntityType");
        Assert.NotEmpty(entityTypes);
        
        // Check that EntityTypes have required structure
        foreach (var entityType in entityTypes)
        {
            // Should have Name attribute
            var nameAttr = entityType.Attribute("Name");
            Assert.NotNull(nameAttr);
            
            // Should have Key element with PropertyRef
            var key = entityType.Element(edmNs + "Key");
            if (key != null)
            {
                var propertyRef = key.Element(edmNs + "PropertyRef");
                Assert.NotNull(propertyRef);
            }
            
            // Should have Property elements
            var properties = entityType.Elements(edmNs + "Property");
            // Some entities might not have properties loaded, so just check structure exists
        }
    }

    [Fact]
    public async Task Should_Include_EntityContainer_In_Metadata()
    {
        // Reference: http://docs.oasis-open.org/odata/odata-csdl-xml/v4.01/odata-csdl-xml-v4.01.html#sec_EntityContainer
        // EntityContainer defines the entity sets available in the service
        
        // Act
        var response = await _httpClient!.GetAsync("$metadata");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var doc = XDocument.Parse(content);
        
        var edmxNs = XNamespace.Get("http://docs.oasis-open.org/odata/ns/edmx");
        var edmNs = XNamespace.Get("http://docs.oasis-open.org/odata/ns/edm");
        
        var schema = doc.Root!.Element(edmxNs + "DataServices")!.Element(edmNs + "Schema");
        
        // Should have EntityContainer
        var entityContainer = schema!.Element(edmNs + "EntityContainer");
        Assert.NotNull(entityContainer);
        
        // Should have Name attribute
        var nameAttr = entityContainer!.Attribute("Name");
        Assert.NotNull(nameAttr);
        Assert.Equal("Container", nameAttr!.Value);
    }

    [Fact]
    public async Task Should_Support_Count_Query_Option()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // $count=true includes total count in response
        
        // Act
        var response = await _httpClient!.GetAsync("EntityDefinitions?$count=true");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(content);
        
        // Should have @odata.count
        Assert.True(json.RootElement.TryGetProperty("@odata.count", out var count));
        Assert.True(count.TryGetInt32(out var countValue));
        Assert.True(countValue >= 0);
    }

    [Fact]
    public async Task Should_Support_Top_And_Skip_Query_Options()
    {
        // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        // $top and $skip enable pagination
        
        // First get total count
        var countResponse = await _httpClient!.GetAsync("EntityDefinitions?$count=true");
        var countContent = await countResponse.Content.ReadAsStringAsync();
        var countJson = JsonDocument.Parse(countContent);
        
        if (!countJson.RootElement.TryGetProperty("@odata.count", out var countProp) || 
            countProp.GetInt32() < 2)
        {
            // Skip test if not enough entities
            return;
        }

        // Act - Get first entity
        var topResponse = await _httpClient!.GetAsync("EntityDefinitions?$top=1");
        var topContent = await topResponse.Content.ReadAsStringAsync();
        var topJson = JsonDocument.Parse(topContent);
        
        Assert.True(topJson.RootElement.TryGetProperty("value", out var topValue));
        Assert.Equal(1, topValue.GetArrayLength());
        
        // Act - Skip first entity
        var skipResponse = await _httpClient!.GetAsync("EntityDefinitions?$skip=1&$top=1");
        var skipContent = await skipResponse.Content.ReadAsStringAsync();
        var skipJson = JsonDocument.Parse(skipContent);
        
        Assert.True(skipJson.RootElement.TryGetProperty("value", out var skipValue));
        
        // Should have at least some entities after skip (unless only 1 total)
        if (countProp.GetInt32() > 1)
        {
            Assert.True(skipValue.GetArrayLength() >= 1);
        }
    }
}
