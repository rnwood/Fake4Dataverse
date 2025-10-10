# Custom API Support

## Overview

Custom APIs are the modern way to create custom messages in Dataverse, replacing the legacy Custom Actions approach. They provide strongly-typed request/response parameters and better integration with the Power Platform.

**Implemented:** 2025-10-10 (Issue #4)

## Microsoft Documentation

Official references:
- [Custom API Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api)
- [Custom API Tables](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables)
- [Parameter Data Types](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables#parameter-data-types)

## What are Custom APIs?

Custom APIs allow you to:
- Define custom business logic with strongly-typed parameters
- Create both Functions (read operations) and Actions (write operations)
- Integrate with Power Automate and Power Apps
- Build reusable business logic across the Power Platform

## Usage

### Define a Simple Custom API

```csharp
using Microsoft.Xrm.Sdk;
using Fake4Dataverse.Middleware;

var context = XrmFakedContextFactory.New();
var service = context.GetOrganizationService();

// Define the Custom API metadata
var customApi = new Entity("customapi")
{
    Id = Guid.NewGuid(),
    ["uniquename"] = "sample_CalculateDiscount",
    ["displayname"] = "Calculate Discount",
    ["bindingtype"] = new OptionSetValue(0), // 0 = Global (not entity-bound)
    ["isfunction"] = false, // false = Action, true = Function
    ["isenabled"] = true,
    ["executeprivilegename"] = null // No special privilege required
};

context.Initialize(new[] { customApi });

// Execute the Custom API
var request = new OrganizationRequest("sample_CalculateDiscount");
var response = service.Execute(request);

// Response contains any output parameters defined
```

### Custom API with Input Parameters

```csharp
var customApiId = Guid.NewGuid();

// Define the Custom API
var customApi = new Entity("customapi")
{
    Id = customApiId,
    ["uniquename"] = "sample_CalculateTotal",
    ["displayname"] = "Calculate Total",
    ["bindingtype"] = new OptionSetValue(0),
    ["isfunction"] = true, // Function (read operation)
    ["isenabled"] = true
};

// Define input parameter: Amount (Decimal, Required)
var inputParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "Amount",
    ["displayname"] = "Amount",
    ["type"] = new OptionSetValue(2), // 2 = Decimal
    ["isoptional"] = false, // Required parameter
    ["logicalentityname"] = null
};

context.Initialize(new[] { customApi, inputParam });

// Execute with input parameter
var request = new OrganizationRequest("sample_CalculateTotal");
request.Parameters["Amount"] = 100.50m;

var response = service.Execute(request);
```

### Custom API with Input and Output Parameters

```csharp
var customApiId = Guid.NewGuid();

// Define the Custom API
var customApi = new Entity("customapi")
{
    Id = customApiId,
    ["uniquename"] = "sample_ProcessOrder",
    ["displayname"] = "Process Order",
    ["bindingtype"] = new OptionSetValue(0),
    ["isfunction"] = false,
    ["isenabled"] = true
};

// Input parameter: OrderId (Guid, Required)
var inputParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "OrderId",
    ["displayname"] = "Order ID",
    ["type"] = new OptionSetValue(12), // 12 = Guid
    ["isoptional"] = false
};

// Output parameter: OrderNumber (String)
var outputParam = new Entity("customapiresponseparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "OrderNumber",
    ["displayname"] = "Order Number",
    ["type"] = new OptionSetValue(10) // 10 = String
};

context.Initialize(new[] { customApi, inputParam, outputParam });

// Execute
var request = new OrganizationRequest("sample_ProcessOrder");
request.Parameters["OrderId"] = Guid.NewGuid();

var response = service.Execute(request);
var orderNumber = response.Results["OrderNumber"]; // Output parameter
```

### Entity-Bound Custom API

Entity-bound Custom APIs operate on a specific entity instance:

```csharp
var customApiId = Guid.NewGuid();

// Define entity-bound Custom API
var customApi = new Entity("customapi")
{
    Id = customApiId,
    ["uniquename"] = "sample_CalculateAccountRevenue",
    ["displayname"] = "Calculate Account Revenue",
    ["bindingtype"] = new OptionSetValue(1), // 1 = Entity-bound
    ["boundentitylogicalname"] = "account", // Bound to account entity
    ["isfunction"] = true,
    ["isenabled"] = true
};

// Output parameter: TotalRevenue (Money)
var outputParam = new Entity("customapiresponseparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "TotalRevenue",
    ["type"] = new OptionSetValue(8) // 8 = Money
};

// Create test account
var account = new Entity("account")
{
    Id = Guid.NewGuid(),
    ["name"] = "Contoso Corp"
};

context.Initialize(new[] { customApi, outputParam, account });

// Execute entity-bound Custom API
var request = new OrganizationRequest("sample_CalculateAccountRevenue");
request.Parameters["Target"] = new EntityReference("account", account.Id);

var response = service.Execute(request);
var revenue = response.Results["TotalRevenue"];
```

## Parameter Data Types

Custom APIs support 13 different parameter data types:

| Type Value | Data Type | C# Type | Description |
|------------|-----------|---------|-------------|
| 0 | Boolean | `bool` | True/False value |
| 1 | DateTime | `DateTime` | Date and time |
| 2 | Decimal | `decimal` | Decimal number |
| 3 | Entity | `Entity` | Entire entity record |
| 4 | EntityCollection | `EntityCollection` | Collection of entities |
| 5 | EntityReference | `EntityReference` | Reference to an entity |
| 6 | Float | `float` | Floating point number |
| 7 | Integer | `int` | Whole number |
| 8 | Money | `Money` | Currency value |
| 9 | Picklist | `OptionSetValue` | Option set value |
| 10 | String | `string` | Text string |
| 11 | StringArray | `string[]` | Array of strings |
| 12 | Guid | `Guid` | Unique identifier |

### Using Different Parameter Types

```csharp
var customApiId = Guid.NewGuid();

var customApi = new Entity("customapi")
{
    Id = customApiId,
    ["uniquename"] = "sample_ComplexOperation",
    ["displayname"] = "Complex Operation",
    ["bindingtype"] = new OptionSetValue(0),
    ["isfunction"] = false,
    ["isenabled"] = true
};

// Boolean parameter
var boolParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "IsActive",
    ["type"] = new OptionSetValue(0), // Boolean
    ["isoptional"] = false
};

// EntityReference parameter
var refParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "RelatedAccount",
    ["type"] = new OptionSetValue(5), // EntityReference
    ["isoptional"] = true,
    ["logicalentityname"] = "account"
};

// Money output parameter
var moneyOutput = new Entity("customapiresponseparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "TotalAmount",
    ["type"] = new OptionSetValue(8) // Money
};

context.Initialize(new[] { customApi, boolParam, refParam, moneyOutput });

// Execute
var request = new OrganizationRequest("sample_ComplexOperation");
request.Parameters["IsActive"] = true;
request.Parameters["RelatedAccount"] = new EntityReference("account", Guid.NewGuid());

var response = service.Execute(request);
```

## Optional vs Required Parameters

```csharp
// Required parameter (isoptional = false)
var requiredParam = new Entity("customapirequestparameter")
{
    ["uniquename"] = "RequiredInput",
    ["isoptional"] = false // Must be provided
};

// Optional parameter (isoptional = true)
var optionalParam = new Entity("customapirequestparameter")
{
    ["uniquename"] = "OptionalInput",
    ["isoptional"] = true // Can be omitted
};

// Execute - RequiredInput must be provided
var request = new OrganizationRequest("sample_MyApi");
request.Parameters["RequiredInput"] = "value"; // Required
// OptionalInput can be omitted

var response = service.Execute(request);
```

## Functions vs Actions

### Functions (Read Operations)

Functions are used for read operations that don't modify data:

```csharp
var customApi = new Entity("customapi")
{
    ["uniquename"] = "sample_GetAccountStatus",
    ["isfunction"] = true, // Function
    ["bindingtype"] = new OptionSetValue(1),
    ["boundentitylogicalname"] = "account"
};

// Functions typically return data without side effects
var request = new OrganizationRequest("sample_GetAccountStatus");
request.Parameters["Target"] = accountRef;

var response = service.Execute(request);
var status = response.Results["Status"];
```

### Actions (Write Operations)

Actions are used for operations that modify data:

```csharp
var customApi = new Entity("customapi")
{
    ["uniquename"] = "sample_ApproveOrder",
    ["isfunction"] = false, // Action
    ["bindingtype"] = new OptionSetValue(0)
};

// Actions can modify data and have side effects
var request = new OrganizationRequest("sample_ApproveOrder");
request.Parameters["OrderId"] = orderId;

var response = service.Execute(request);
```

## Error Handling

### Custom API Not Found

```csharp
var request = new OrganizationRequest("sample_NonExistent");

// Throws: Custom API with unique name 'sample_NonExistent' is not registered
try
{
    service.Execute(request);
}
catch (FaultException<OrganizationServiceFault> ex)
{
    Console.WriteLine(ex.Message);
}
```

### Custom API Disabled

```csharp
var customApi = new Entity("customapi")
{
    ["uniquename"] = "sample_DisabledApi",
    ["isenabled"] = false // Disabled
};

context.Initialize(new[] { customApi });

var request = new OrganizationRequest("sample_DisabledApi");

// Throws: Custom API 'sample_DisabledApi' is not enabled
try
{
    service.Execute(request);
}
catch (FaultException<OrganizationServiceFault> ex)
{
    Console.WriteLine(ex.Message);
}
```

### Missing Required Parameter

```csharp
var customApiId = Guid.NewGuid();
var customApi = new Entity("customapi")
{
    Id = customApiId,
    ["uniquename"] = "sample_RequiresInput",
    ["isenabled"] = true
};

var requiredParam = new Entity("customapirequestparameter")
{
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "RequiredValue",
    ["isoptional"] = false // Required
};

context.Initialize(new[] { customApi, requiredParam });

var request = new OrganizationRequest("sample_RequiresInput");
// Missing RequiredValue parameter

// Throws: Required parameter 'RequiredValue' is missing
try
{
    service.Execute(request);
}
catch (FaultException<OrganizationServiceFault> ex)
{
    Console.WriteLine(ex.Message);
}
```

## Advanced Scenarios

### Complex Business Logic

```csharp
var customApiId = Guid.NewGuid();

// Custom API: Calculate shipping costs
var customApi = new Entity("customapi")
{
    Id = customApiId,
    ["uniquename"] = "sample_CalculateShipping",
    ["displayname"] = "Calculate Shipping",
    ["bindingtype"] = new OptionSetValue(0),
    ["isfunction"] = true,
    ["isenabled"] = true
};

// Input parameters
var weightParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "Weight",
    ["type"] = new OptionSetValue(2), // Decimal
    ["isoptional"] = false
};

var destinationParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "DestinationZip",
    ["type"] = new OptionSetValue(10), // String
    ["isoptional"] = false
};

var expressParam = new Entity("customapirequestparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "ExpressShipping",
    ["type"] = new OptionSetValue(0), // Boolean
    ["isoptional"] = true
};

// Output parameters
var costOutput = new Entity("customapiresponseparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "ShippingCost",
    ["type"] = new OptionSetValue(8) // Money
};

var daysOutput = new Entity("customapiresponseparameter")
{
    Id = Guid.NewGuid(),
    ["customapiid"] = new EntityReference("customapi", customApiId),
    ["uniquename"] = "EstimatedDays",
    ["type"] = new OptionSetValue(7) // Integer
};

context.Initialize(new[] { 
    customApi, weightParam, destinationParam, expressParam,
    costOutput, daysOutput 
});

// Execute
var request = new OrganizationRequest("sample_CalculateShipping");
request.Parameters["Weight"] = 5.5m;
request.Parameters["DestinationZip"] = "98052";
request.Parameters["ExpressShipping"] = true;

var response = service.Execute(request);
var cost = response.Results["ShippingCost"];
var days = response.Results["EstimatedDays"];
```

### Integration with Business Process

```csharp
// Custom API that updates multiple related records
var customApi = new Entity("customapi")
{
    ["uniquename"] = "sample_CloseOpportunityWithTasks",
    ["bindingtype"] = new OptionSetValue(1),
    ["boundentitylogicalname"] = "opportunity",
    ["isfunction"] = false,
    ["isenabled"] = true
};

// This would typically be implemented in a plugin
// In tests, you can verify the API is called correctly
var request = new OrganizationRequest("sample_CloseOpportunityWithTasks");
request.Parameters["Target"] = opportunityRef;
request.Parameters["CloseReason"] = "Won";

var response = service.Execute(request);
```

## Best Practices

1. **Use Descriptive Names**: Name your Custom APIs clearly (e.g., `sample_CalculateDiscount`)
2. **Document Parameters**: Use meaningful display names and descriptions
3. **Validate Input**: Mark parameters as required when they're essential
4. **Consider Security**: Use `executeprivilegename` to restrict access when needed
5. **Test Thoroughly**: Test with various input combinations and edge cases
6. **Choose Binding Type Wisely**: Use entity-bound for entity-specific operations
7. **Functions vs Actions**: Use functions for read operations, actions for writes

## Common Use Cases

### Data Validation

```csharp
// Custom API to validate account data
var request = new OrganizationRequest("sample_ValidateAccount");
request.Parameters["AccountId"] = accountId;

var response = service.Execute(request);
var isValid = (bool)response.Results["IsValid"];
var errors = (string[])response.Results["ValidationErrors"];
```

### Complex Calculations

```csharp
// Calculate complex pricing with discounts
var request = new OrganizationRequest("sample_CalculateFinalPrice");
request.Parameters["BasePrice"] = 100.00m;
request.Parameters["DiscountPercent"] = 15;
request.Parameters["CustomerTier"] = new OptionSetValue(2);

var response = service.Execute(request);
var finalPrice = response.Results["FinalPrice"];
```

### Batch Operations

```csharp
// Process multiple records in a single call
var request = new OrganizationRequest("sample_BulkUpdateStatus");
request.Parameters["RecordIds"] = new Guid[] { id1, id2, id3 };
request.Parameters["NewStatus"] = new OptionSetValue(1);

var response = service.Execute(request);
var successCount = (int)response.Results["SuccessCount"];
```

## Comparison with Custom Actions

| Feature | Custom API | Custom Action |
|---------|------------|---------------|
| Recommendation | ✅ Recommended | ⚠️ Legacy |
| Binding Types | Global, Entity-bound, Entity Collection | Global, Entity-bound |
| Parameter Types | 13 types | Limited types |
| Power Platform Integration | ✅ Full support | ⚠️ Limited |
| Definition | Code/API | Workflow designer |
| Performance | Better | Slower |

## Related Documentation

- [Microsoft Custom API Documentation](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/custom-api)
- [Custom API Tables](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/customapi-tables)
- [Custom Actions](custom-actions.md) (Coming soon)
- [Message Executors](../Fake4DataverseCore/README.md#message-executors)

## Implementation Details

- **Files**: 
  - `Fake4DataverseCore/src/Fake4Dataverse.Core/FakeMessageExecutors/CustomApiExecutor.cs`
  - `Fake4DataverseAbstractions/src/Fake4Dataverse.Abstractions/FakeMessageExecutors/OrganizationRequestExecutors.cs`
  - `Fake4DataverseCore/src/Fake4Dataverse.Core/Middleware/Messages/MiddlewareBuilderExtensions.Messages.cs`
- **Tests**: `Fake4DataverseCore/tests/Fake4Dataverse.Core.Tests/FakeContextTests/CustomApiTests/`
- **Feature Parity**: Matches FakeXrmEasy v2+ behavior
