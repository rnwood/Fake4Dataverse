# Metadata Persistence

**Implementation Date**: December 2025  
**Issue Reference**: [Persist tables/columns and other metadata](https://github.com/rnwood/Fake4Dataverse/issues/XXX)

Fake4Dataverse now persists entity and attribute metadata to standard Dataverse metadata tables, allowing metadata to be queried like regular entity data.

## Overview

In real Dataverse/Dynamics 365, metadata is accessible through special virtual entities:
- **EntityDefinition** (`entitydefinition`) - Contains entity metadata
- **Attribute** (`attribute`) - Contains attribute/field metadata

Starting with v4.0+, Fake4Dataverse automatically persists metadata to these tables when you initialize or update entity metadata. This enables:
- Querying metadata using standard CRUD operations
- Building tools that work with metadata as data
- Testing code that reads from metadata tables
- More realistic test scenarios

**Reference**: [Dataverse Entity Metadata](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata)

## How It Works

When you initialize entity metadata in Fake4Dataverse, the framework automatically:

1. **Creates EntityDefinition records** - One record per entity with properties like `logicalname`, `schemaname`, `primaryidattribute`, etc.
2. **Creates Attribute records** - One record per attribute with properties like `logicalname`, `entitylogicalname`, `attributetype`, `maxlength`, etc.
3. **Keeps both in sync** - Updates to metadata via `SetEntityMetadata()` are reflected in the tables

This happens transparently - you don't need to change your existing test code. The metadata is stored in both the traditional in-memory dictionary AND in the metadata tables.

## Basic Usage

### Loading System Entity Metadata

First, load the system entity metadata which includes the `entitydefinition` and `attribute` tables:

```csharp
var context = XrmFakedContextFactory.New();
context.InitializeSystemEntityMetadata(); // Loads entitydefinition, attribute, and other system entities

var service = context.GetOrganizationService();
```

### Initialize Entity Metadata

When you initialize metadata, it's automatically persisted to the tables:

```csharp
// Create entity metadata
var accountMetadata = new EntityMetadata()
{
    LogicalName = "account",
    SchemaName = "Account"
};
accountMetadata.SetSealedPropertyValue("MetadataId", Guid.NewGuid());
accountMetadata.SetSealedPropertyValue("PrimaryIdAttribute", "accountid");
accountMetadata.SetSealedPropertyValue("PrimaryNameAttribute", "name");

// Add attributes
var nameAttribute = new StringAttributeMetadata()
{
    LogicalName = "name",
    SchemaName = "Name",
    MaxLength = 100
};
nameAttribute.SetSealedPropertyValue("MetadataId", Guid.NewGuid());

accountMetadata.SetAttributeCollection(new[] { nameAttribute });

// Initialize metadata - automatically persisted to tables
context.InitializeMetadata(accountMetadata);
```

### Query Metadata from Tables

Now you can query the metadata tables like any other entity:

```csharp
// Query entity definitions
var entityQuery = new QueryExpression("entitydefinition")
{
    ColumnSet = new ColumnSet("logicalname", "schemaname", "primaryidattribute"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("logicalname", ConditionOperator.Equal, "account")
        }
    }
};

var entityResults = service.RetrieveMultiple(entityQuery);
Assert.Single(entityResults.Entities);

var entityDef = entityResults.Entities[0];
Assert.Equal("account", entityDef.GetAttributeValue<string>("logicalname"));
Assert.Equal("Account", entityDef.GetAttributeValue<string>("schemaname"));

// Query attributes
var attributeQuery = new QueryExpression("attribute")
{
    ColumnSet = new ColumnSet("logicalname", "schemaname", "maxlength"),
    Criteria = new FilterExpression
    {
        Conditions =
        {
            new ConditionExpression("entitylogicalname", ConditionOperator.Equal, "account")
        }
    }
};

var attributeResults = service.RetrieveMultiple(attributeQuery);
// Returns all attributes for the account entity
```

## Use Cases

### 1. Testing Metadata-Driven Code

If your code reads metadata at runtime to make decisions:

```csharp
// Your production code that reads metadata
public List<string> GetPicklistAttributes(IOrganizationService service, string entityName)
{
    var query = new QueryExpression("attribute")
    {
        ColumnSet = new ColumnSet("logicalname"),
        Criteria = new FilterExpression
        {
            Conditions =
            {
                new ConditionExpression("entitylogicalname", ConditionOperator.Equal, entityName),
                new ConditionExpression("attributetype", ConditionOperator.Equal, 
                    (int)AttributeTypeCode.Picklist)
            }
        }
    };
    
    var results = service.RetrieveMultiple(query);
    return results.Entities
        .Select(e => e.GetAttributeValue<string>("logicalname"))
        .ToList();
}

// Your test
[Fact]
public void Should_Find_Picklist_Attributes()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.InitializeSystemEntityMetadata();
    
    // Create entity with a picklist attribute
    var entityMetadata = new EntityMetadata() { LogicalName = "testentity" };
    var picklistAttr = new PicklistAttributeMetadata() 
    { 
        LogicalName = "statuscode" 
    };
    picklistAttr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Picklist);
    entityMetadata.SetAttributeCollection(new[] { picklistAttr });
    context.InitializeMetadata(entityMetadata);
    
    var service = context.GetOrganizationService();
    
    // Act
    var picklistAttributes = GetPicklistAttributes(service, "testentity");
    
    // Assert
    Assert.Contains("statuscode", picklistAttributes);
}
```

### 2. Building Metadata Discovery Tools

Test tools that discover and analyze entity structures:

```csharp
public class MetadataDiscovery
{
    public Dictionary<string, int> GetAttributeCountPerEntity(IOrganizationService service)
    {
        // Query all attributes grouped by entity
        var query = new QueryExpression("attribute")
        {
            ColumnSet = new ColumnSet("entitylogicalname")
        };
        
        var results = service.RetrieveMultiple(query);
        
        return results.Entities
            .GroupBy(e => e.GetAttributeValue<string>("entitylogicalname"))
            .ToDictionary(g => g.Key, g => g.Count());
    }
}

// Test it
[Fact]
public void Should_Count_Attributes_Per_Entity()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.InitializeSystemEntityMetadata();
    
    var entity1 = new EntityMetadata() { LogicalName = "entity1" };
    var attr1 = new StringAttributeMetadata() { LogicalName = "field1" };
    var attr2 = new StringAttributeMetadata() { LogicalName = "field2" };
    entity1.SetAttributeCollection(new[] { attr1, attr2 });
    
    context.InitializeMetadata(entity1);
    
    var service = context.GetOrganizationService();
    var discovery = new MetadataDiscovery();
    
    // Act
    var counts = discovery.GetAttributeCountPerEntity(service);
    
    // Assert
    Assert.Equal(2, counts["entity1"]);
}
```

### 3. Testing Metadata Queries in Plugins

If your plugin queries metadata tables:

```csharp
public class MyPlugin : IPlugin
{
    public void Execute(IServiceProvider serviceProvider)
    {
        var service = (IOrganizationService)serviceProvider.GetService(typeof(IOrganizationService));
        
        // Query metadata to check if entity supports business process flows
        var query = new QueryExpression("entitydefinition")
        {
            ColumnSet = new ColumnSet("isbusinessprocessenabled"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("logicalname", ConditionOperator.Equal, "account")
                }
            }
        };
        
        var result = service.RetrieveMultiple(query);
        // ... use the metadata
    }
}

// Test the plugin
[Fact]
public void Plugin_Should_Query_Entity_Metadata()
{
    // Arrange
    var context = XrmFakedContextFactory.New();
    context.InitializeSystemEntityMetadata();
    
    var accountMetadata = new EntityMetadata() { LogicalName = "account" };
    accountMetadata.SetSealedPropertyValue("IsBusinessProcessEnabled", true);
    context.InitializeMetadata(accountMetadata);
    
    // Act & Assert
    var plugin = new MyPlugin();
    context.ExecutePluginWith<Entity>(plugin, new Entity("account"));
}
```

## Available Metadata Properties

### EntityDefinition Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `metadataid` | Guid | Unique identifier of the entity metadata |
| `logicalname` | String | Logical name (e.g., "account") |
| `schemaname` | String | Schema name (e.g., "Account") |
| `displayname` | String | Display name |
| `pluralname` | String | Plural display name |
| `description` | String | Description |
| `objecttypecode` | Integer | Object type code |
| `iscustomentity` | Boolean | Whether custom entity |
| `ismanaged` | Boolean | Whether managed |
| `iscustomizable` | Boolean | Whether customizable |
| `isactivity` | Boolean | Whether activity entity |
| `isvalidforqueue` | Boolean | Whether can be added to queues |
| `primaryidattribute` | String | Primary ID attribute name |
| `primarynameattribute` | String | Primary name attribute name |
| `primaryimageattribute` | String | Primary image attribute name |
| `ownershiptype` | Integer | Ownership type (0=None, 1=UserOwned, 2=TeamOwned, 4=OrgOwned) |
| `isauditenabled` | Boolean | Whether auditing is enabled |
| `isbusinessprocessenabled` | Boolean | Whether BPF enabled |
| `isvalidforadvancedfind` | Boolean | Whether appears in Advanced Find |

### Attribute Attributes

| Attribute | Type | Description |
|-----------|------|-------------|
| `metadataid` | Guid | Unique identifier of the attribute metadata |
| `logicalname` | String | Logical name |
| `schemaname` | String | Schema name |
| `displayname` | String | Display name |
| `description` | String | Description |
| `attributetype` | Integer | Attribute type code |
| `attributetypename` | String | Attribute type name |
| `entitylogicalname` | String | Parent entity logical name |
| `iscustomattribute` | Boolean | Whether custom attribute |
| `ismanaged` | Boolean | Whether managed |
| `iscustomizable` | Boolean | Whether customizable |
| `isprimaryid` | Boolean | Whether primary ID |
| `isprimaryname` | Boolean | Whether primary name |
| `isvalidforcreate` | Boolean | Valid for create |
| `isvalidforupdate` | Boolean | Valid for update |
| `isvalidforread` | Boolean | Valid for read |
| `requiredlevel` | Integer | Required level |
| `isauditenabled` | Boolean | Auditing enabled |
| `issecured` | Boolean | Field-level security enabled |
| `maxlength` | Integer | Max length (string attributes) |
| `precision` | Integer | Precision (decimal attributes) |
| `minvalue` | Decimal | Min value (numeric attributes) |
| `maxvalue` | Decimal | Max value (numeric attributes) |

## Backward Compatibility

The metadata persistence feature is **fully backward compatible**:

- If you don't call `InitializeSystemEntityMetadata()`, metadata persistence is silently skipped
- All existing tests continue to work without modification
- The traditional in-memory metadata dictionary is still used and maintained
- Existing metadata queries via `GetEntityMetadataByName()` and `CreateMetadataQuery()` continue to work

## Limitations

### Not Yet Implemented

- **Relationship metadata** - Relationships are not yet persisted to tables
- **OptionSet metadata** - Option sets have their own repository but aren't persisted to tables
- **Reverse queries** - Reading from tables to populate the in-memory dictionary
- **Create/Update via tables** - Creating entities by inserting EntityDefinition records

These may be added in future versions based on community feedback.

### Current Behavior

- Metadata persistence only works when both `entitydefinition` and `attribute` tables exist in the context
- Changes made directly to entity definition or attribute records are NOT reflected back to the EntityMetadata objects
- The source of truth remains the in-memory `EntityMetadata` dictionary

## Related Documentation

- [CDM Import](./cdm-import.md) - Loading metadata from CDM files
- [Basic Concepts](../getting-started/basic-concepts.md) - Understanding XrmFakedContext
- [Testing Plugins](../usage/testing-plugins.md) - Plugin testing patterns

## External Resources

- [Dataverse Entity Metadata](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/entity-metadata)
- [EntityDefinition Table](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/entitydefinition)
- [Attribute Table](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/attribute)
