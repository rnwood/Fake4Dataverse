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

### Loading Standard CDM Schema Groups

Fake4Dataverse can download standard schema groups directly from Microsoft's CDM repository. Schema groups automatically include all their dependencies through CDM imports:

```csharp
// Arrange
var context = XrmFakedContextFactory.New();

// Load standard CDM schema groups by name
// This downloads the schemas from Microsoft's official CDM repository
// and follows all imports to load dependent entities
await context.InitializeMetadataFromStandardCdmSchemasAsync(new[] {
    "crmcommon",  // Base CRM entities (Account, Contact, Lead, etc.)
    "sales"       // Sales entities (Opportunity, Quote, Order, Invoice, etc.)
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

### Available Schema Groups

- **crmcommon** - Base CRM entities (Account, Contact, Lead, etc.)
- **sales** - Sales-specific entities (Opportunity, Quote, Order, Invoice, etc.)
- **service** - Service/Support entities (Case, Queue, etc.)
- **portals** - Portal-related entities
- **customerInsights** - Customer Insights entities

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

// Download standard schema groups
var standardMetadata = await MetadataGenerator.FromStandardCdmSchemasAsync(new[] {
    "crmcommon",
    "sales"
});

// Initialize context with parsed metadata
context.InitializeMetadata(entityMetadataList);
```

## Fake4DataverseService CLI

The Fake4DataverseService command-line tool supports CDM import via command-line arguments.

### Default Behavior

By default, the service loads the **crmcommon** schema group when started:

```bash
# Starts service with crmcommon schema loaded by default
dotnet run -- start
```

### Load CDM from Local Files

```bash
# Load entity metadata from CDM JSON files
dotnet run -- start --cdm-files Account.cdm.json Contact.cdm.json Lead.cdm.json

# With full paths
dotnet run -- start --cdm-files /path/to/schemas/Account.cdm.json /path/to/schemas/Contact.cdm.json
```

### Load Standard CDM Schema Groups

```bash
# Download and load standard CDM schema groups
dotnet run -- start --cdm-schemas crmcommon sales service

# Load only sales (without crmcommon default)
dotnet run -- start --cdm-schemas sales

# Combine with other options
dotnet run -- start --port 8080 --cdm-schemas crmcommon sales
```

### Combine CDM with Other Options

```bash
# Load CDM files, set port, and enable authentication
dotnet run -- start \
  --port 8080 \
  --access-token mytoken123 \
  --cdm-files Account.cdm.json \
  --cdm-schemas sales service

# Note: When --cdm-files is specified, crmcommon is NOT loaded by default
# Explicitly include it if needed:
dotnet run -- start --cdm-files custom.cdm.json --cdm-schemas crmcommon
```

## CDM JSON Format

CDM JSON files follow this basic structure:

```json
{
  "jsonSchemaSemanticVersion": "1.0.0",
  "imports": [
    {
      "corpusPath": "Contact.cdm.json"
    },
    {
      "corpusPath": "Lead.cdm.json"
    }
  ],
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

- **`imports`**: Array of dependencies - other CDM files to load before this one
  - **`corpusPath`**: Relative or absolute path to the imported CDM file
  - Fake4Dataverse recursively follows imports to load all dependencies
- **`$type`**: Must be "LocalEntity" or "CdmEntityDefinition" for entity definitions
- **`name`**: The schema name (typically PascalCase, e.g., "Account")
- **`sourceName`**: The Dataverse logical name (lowercase, e.g., "account")
- **`hasAttributes`**: Array of attribute definitions
  - **`name`**: Attribute schema name
  - **`sourceName`**: Dataverse logical name (e.g., "accountid")
  - **`dataType`**: Data type (string, guid, integer, datetime, money, lookup, etc.)
  - **`isPrimaryKey`**: Set to `true` for the primary key attribute
  - **`maximumLength`**: For string attributes, the maximum length

### Import/Dependency Following

When you load a CDM schema group like "crmcommon", Fake4Dataverse:

1. Downloads the main crmCommon.cdm.json file
2. Parses its `imports` array to find dependencies
3. Recursively downloads and parses each imported file
4. Loads dependencies before entities that reference them
5. Avoids circular dependencies by tracking processed files

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

## Available Standard CDM Schema Groups

The following standard schema groups can be downloaded from Microsoft's CDM repository:

### crmcommon (Default)
Base CRM entities including:
- Account - Business account entity
- Contact - Contact person entity
- Lead - Sales lead entity
- And many more foundation entities

### sales
Sales-specific entities including:
- Opportunity - Sales opportunity entity
- Quote - Quote entity
- Order - Sales order entity
- Invoice - Invoice entity
- And more sales entities

### service
Service/Support entities including:
- Case - Customer service case entity
- Queue - Queue entity
- And more service entities

### portals
Portal-related entities for Power Pages and Portals

### customerInsights
Customer Insights entities for analytics and insights

**Reference**: Check Microsoft's CDM repository for the complete list: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon

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

### 1. Use Standard Schema Groups When Possible

If you're testing with standard Dataverse entities, prefer downloading schema groups:

```csharp
// Good - Downloads official Microsoft schemas with all dependencies
await context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "crmcommon", "sales" });
```

### 2. Let the Service Default to crmcommon

When using Fake4DataverseService, the default crmcommon schema provides a good baseline:

```bash
# Simple - starts with crmcommon loaded
dotnet run -- start

# Explicit - same as above
dotnet run -- start --cdm-schemas crmcommon
```

### 3. Store CDM Files in Your Test Project

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

### 4. Combine Standard and Custom Schemas

Use CDM schema groups for standard entities and local files for custom entities:

```csharp
// Load standard entities from CDM
await context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "crmcommon" });

// Load custom entities from local files
context.InitializeMetadataFromCdmFile("CustomEntity.cdm.json");
```

Or combine with early-bound for custom entities:

```csharp
// Load standard entities from CDM
await context.InitializeMetadataFromStandardCdmSchemasAsync(new[] { "crmcommon" });

// Load custom entities from early-bound assembly
context.InitializeMetadata(typeof(custom_entity).Assembly);
```

### 5. Version Control Your CDM Files

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
ArgumentException: Unknown standard CDM schema: CustomEntity
```

**Solution**: The schema name must be one of the standard groups: crmcommon, sales, service, portals, customerInsights. Check available schemas at: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon

## Related Documentation

- [Installation Guide](./installation.md) - Installing Fake4Dataverse packages
- [Basic Concepts](./basic-concepts.md) - Understanding XrmFakedContext
- [Testing Plugins](../usage/testing-plugins.md) - Writing plugin tests
- [Metadata Overview](../concepts/metadata.md) - Understanding entity metadata

## External Resources

- [Microsoft Common Data Model Repository](https://github.com/microsoft/CDM)
- [CDM Documentation](https://docs.microsoft.com/en-us/common-data-model/)
- [Dataverse Entity Metadata](https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata)
