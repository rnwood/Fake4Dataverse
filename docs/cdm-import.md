# CDM (Common Data Model) Import

Fake4Dataverse supports importing entity metadata from CDM (Common Data Model) JSON files. This allows you to quickly initialize your test context with standard Dataverse entity schemas without needing to manually define metadata or use early-bound entities.

## What is CDM?

The Common Data Model (CDM) is Microsoft's standard schema definition format that provides a shared data language across business applications and data sources. CDM JSON files define entity schemas with attributes, data types, and relationships in a standardized format.

**Reference**: https://github.com/microsoft/CDM

## When to Use CDM Import

Use CDM import when you want to:

- **Test with standard Dataverse entities** - Quickly initialize Account, Contact, Lead, etc. without early-bound classes
- **Test with exported schemas** - Use entity schemas exported from your Dataverse environment
- **Avoid early-bound dependencies** - Don't want to generate or maintain early-bound entity classes
- **Share schemas across projects** - Use portable JSON files instead of compiled assemblies

## Usage Examples

### Loading CDM from Local Files

```csharp
// Arrange
var context = XrmFakedContextFactory.New();

// Load entity metadata from a CDM JSON file
context.InitializeMetadataFromCdmFile("path/to/Account.cdm.json");

// Or load from multiple files
context.InitializeMetadataFromCdmFiles(new[] {
    "path/to/Account.cdm.json",
    "path/to/Contact.cdm.json",
    "path/to/Lead.cdm.json"
});

// Now you can use the entities in your tests
var service = context.GetOrganizationService();
var account = new Entity("account")
{
    ["name"] = "Contoso"
};
var accountId = service.Create(account);
```

### Loading Standard CDM Entities

Fake4Dataverse can download standard entity schemas directly from Microsoft's CDM repository:

```csharp
// Arrange
var context = XrmFakedContextFactory.New();

// Load standard CDM entities by name
// This downloads the schemas from Microsoft's official CDM repository
await context.InitializeMetadataFromStandardCdmEntitiesAsync(new[] {
    "Account",
    "Contact",
    "Lead",
    "Opportunity"
});

// Now use the entities in your tests
var service = context.GetOrganizationService();
var contact = new Entity("contact")
{
    ["firstname"] = "John",
    ["lastname"] = "Doe"
};
var contactId = service.Create(contact);
```

### Using MetadataGenerator Directly

You can also use the `MetadataGenerator` class directly to parse CDM files:

```csharp
using Fake4Dataverse.Metadata;

// Parse CDM from file
var entityMetadataList = MetadataGenerator.FromCdmJsonFile("path/to/Account.cdm.json");

// Parse from multiple files
var allMetadata = MetadataGenerator.FromCdmJsonFiles(new[] {
    "path/to/Account.cdm.json",
    "path/to/Contact.cdm.json"
});

// Download standard entities
var standardMetadata = await MetadataGenerator.FromStandardCdmEntitiesAsync(new[] {
    "Account",
    "Contact"
});

// Initialize context with parsed metadata
context.InitializeMetadata(entityMetadataList);
```

## Fake4DataverseService CLI

The Fake4DataverseService command-line tool supports CDM import via command-line arguments:

### Load CDM from Local Files

```bash
# Load entity metadata from CDM JSON files
dotnet run -- start --cdm-files Account.cdm.json Contact.cdm.json Lead.cdm.json

# With full paths
dotnet run -- start --cdm-files /path/to/schemas/Account.cdm.json /path/to/schemas/Contact.cdm.json
```

### Load Standard CDM Entities

```bash
# Download and load standard CDM entities
dotnet run -- start --cdm-entities Account Contact Lead Opportunity

# Combine with other options
dotnet run -- start --port 8080 --cdm-entities Account Contact User Team
```

### Combine CDM with Other Options

```bash
# Load CDM files, set port, and enable authentication
dotnet run -- start \
  --port 8080 \
  --access-token mytoken123 \
  --cdm-files Account.cdm.json \
  --cdm-entities Contact Lead
```

## CDM JSON Format

CDM JSON files follow this basic structure:

```json
{
  "jsonSchemaSemanticVersion": "1.0.0",
  "imports": [],
  "definitions": [
    {
      "$type": "LocalEntity",
      "name": "Account",
      "description": "Business that represents a customer or potential customer",
      "sourceName": "account",
      "hasAttributes": [
        {
          "name": "accountId",
          "dataType": "guid",
          "sourceName": "accountid",
          "isPrimaryKey": true,
          "description": "Unique identifier of the account"
        },
        {
          "name": "name",
          "dataType": "string",
          "sourceName": "name",
          "maximumLength": 160,
          "description": "Name of the account"
        },
        {
          "name": "revenue",
          "dataType": "money",
          "sourceName": "revenue",
          "description": "Annual revenue"
        }
      ]
    }
  ]
}
```

### Key CDM Properties

- **`$type`**: Must be "LocalEntity" or "CdmEntityDefinition" for entity definitions
- **`name`**: The schema name (typically PascalCase, e.g., "Account")
- **`sourceName`**: The Dataverse logical name (lowercase, e.g., "account")
- **`hasAttributes`**: Array of attribute definitions
  - **`name`**: Attribute schema name
  - **`sourceName`**: Dataverse logical name (e.g., "accountid")
  - **`dataType`**: Data type (string, guid, integer, datetime, money, lookup, etc.)
  - **`isPrimaryKey`**: Set to `true` for the primary key attribute
  - **`maximumLength`**: For string attributes, the maximum length

## Supported Data Types

Fake4Dataverse CDM import supports the following data types:

| CDM Data Type | Dataverse AttributeMetadata Type |
|---------------|----------------------------------|
| `string`, `text` | StringAttributeMetadata |
| `guid`, `uniqueidentifier` | UniqueIdentifierAttributeMetadata |
| `int`, `integer`, `int32` | IntegerAttributeMetadata |
| `long`, `int64`, `bigint` | BigIntAttributeMetadata |
| `decimal` | DecimalAttributeMetadata |
| `double`, `float` | DoubleAttributeMetadata |
| `boolean`, `bool` | BooleanAttributeMetadata |
| `datetime`, `date`, `time` | DateTimeAttributeMetadata |
| `money`, `currency` | MoneyAttributeMetadata |
| `picklist`, `optionset` | PicklistAttributeMetadata |
| `lookup`, `entityreference` | LookupAttributeMetadata |
| `memo`, `multilinetext` | MemoAttributeMetadata |
| `image`, `file` | ImageAttributeMetadata |

## Available Standard CDM Entities

The following standard entities can be downloaded from Microsoft's CDM repository:

- **Account** - Business account entity
- **Contact** - Contact person entity
- **Lead** - Sales lead entity
- **Opportunity** - Sales opportunity entity
- **User** - System user entity
- **Team** - Team entity
- **BusinessUnit** - Business unit entity
- **Organization** - Organization entity

More entities may be available. Check Microsoft's CDM repository: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon

## Exporting CDM from Dataverse

To export CDM JSON schemas from your Dataverse environment, you can:

1. **Use Power Platform CLI tools** - Some tools can export schemas to CDM format
2. **Manually create CDM files** - Based on your entity definitions
3. **Use Microsoft's CDM SDK** - For programmatic export

## Limitations

### Current Limitations

- **Relationships not fully supported** - CDM relationship definitions are parsed but not fully implemented
- **Complex data types** - Some advanced CDM data types may not map perfectly to Dataverse
- **Localization** - Display names and descriptions in other languages are not preserved

### What IS Supported

- ✅ Basic entity definitions with logical names
- ✅ All common attribute types (string, integer, money, lookup, etc.)
- ✅ Primary key identification
- ✅ Attribute max lengths for strings
- ✅ Multiple entities in single CDM file
- ✅ Loading from local files
- ✅ Downloading standard Microsoft CDM schemas

## Best Practices

### 1. Use Standard Entities When Possible

If you're testing with standard Dataverse entities, prefer downloading them:

```csharp
// Good - Downloads official Microsoft schemas
await context.InitializeMetadataFromStandardCdmEntitiesAsync(new[] { "Account", "Contact" });
```

### 2. Store CDM Files in Your Test Project

Keep CDM files alongside your tests for portability:

```
MyProject.Tests/
  ├── Schemas/
  │   ├── Account.cdm.json
  │   ├── Contact.cdm.json
  │   └── CustomEntity.cdm.json
  └── Tests/
      └── MyEntityTests.cs
```

### 3. Combine with Early-Bound for Custom Entities

Use CDM for standard entities and early-bound for custom entities:

```csharp
// Load standard entities from CDM
await context.InitializeMetadataFromStandardCdmEntitiesAsync(new[] { "Account", "Contact" });

// Load custom entities from early-bound assembly
context.InitializeMetadata(typeof(custom_entity).Assembly);
```

### 4. Version Control Your CDM Files

Check CDM files into source control so all developers have consistent schemas.

## Troubleshooting

### File Not Found Error

```
FileNotFoundException: CDM JSON file not found: path/to/file.cdm.json
```

**Solution**: Verify the file path is correct and the file exists.

### Invalid JSON Error

```
InvalidOperationException: Failed to parse CDM JSON
```

**Solution**: Validate your JSON syntax. Use a JSON validator online or in your IDE.

### No Entity Definitions Error

```
InvalidOperationException: CDM document contains no entity definitions
```

**Solution**: Ensure your CDM file has at least one entity definition with `"$type": "LocalEntity"`.

### Unknown Standard Entity Error

```
ArgumentException: Unknown standard CDM entity: CustomEntity
```

**Solution**: The entity name must be a standard Microsoft CDM entity. Check available entities at: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon

## Related Documentation

- [Installation Guide](./installation.md) - Installing Fake4Dataverse packages
- [Basic Concepts](./basic-concepts.md) - Understanding XrmFakedContext
- [Testing Plugins](../usage/testing-plugins.md) - Writing plugin tests
- [Metadata Overview](../concepts/metadata.md) - Understanding entity metadata

## External Resources

- [Microsoft Common Data Model Repository](https://github.com/microsoft/CDM)
- [CDM Documentation](https://docs.microsoft.com/en-us/common-data-model/)
- [Dataverse Entity Metadata](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata)
