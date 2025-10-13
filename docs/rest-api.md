# REST/OData API Endpoints

Fake4Dataverse Service provides full REST/OData v4.0 endpoints compatible with the Microsoft Dataverse Web API.

**Reference:** [Microsoft Dataverse Web API Overview](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview)

## Overview

The REST API endpoints provide an alternative to SOAP/Organization Service for interacting with Dataverse entities. These endpoints use the OData v4.0 protocol and support advanced query capabilities.

**Base URL:** `http://localhost:5000/api/data/v9.2`

## Features

✅ **Full CRUD Operations**
- Create entities (POST)
- Retrieve entities by ID (GET)
- Update entities (PATCH)
- Delete entities (DELETE)
- List/query entities (GET with query options)

✅ **Advanced OData Query Support** via Microsoft.AspNetCore.OData v9.4.0
- `$select`: Choose specific columns
- `$filter`: Filter with complex OData expressions
- `$orderby`: Sort results
- `$top`: Limit results
- `$skip`: Pagination offset
- `$count`: Include total count
- `$expand`: Include related entities

✅ **OData Compliance**
- OData v4.0 protocol
- Standard OData annotations (@odata.context, @odata.etag, @odata.nextLink)
- Proper HTTP status codes
- OData error responses

## Endpoints

### List Entities

Get a collection of entities with optional query parameters.

**Endpoint:** `GET /api/data/v9.2/{entityPluralName}`

**Query Options:**
- `$select` - Comma-separated list of columns
- `$filter` - OData filter expression
- `$orderby` - Sort expression with optional 'desc'
- `$top` - Maximum records to return
- `$skip` - Number of records to skip
- `$count` - Include total count (true/false)

**Example:**
```bash
# Get all accounts
curl http://localhost:5000/api/data/v9.2/accounts

# Get accounts with specific columns
curl http://localhost:5000/api/data/v9.2/accounts?\$select=name,revenue

# Filter accounts by revenue
curl "http://localhost:5000/api/data/v9.2/accounts?\$filter=revenue gt 100000"

# Sort and paginate
curl "http://localhost:5000/api/data/v9.2/accounts?\$orderby=createdon desc&\$top=10&\$skip=20"

# Complex filter with logical operators
curl "http://localhost:5000/api/data/v9.2/accounts?\$filter=revenue gt 100000 and contains(name,'Corp')"
```

**Response:**
```json
{
  "@odata.context": "#Microsoft.Dynamics.CRM.account",
  "value": [
    {
      "accountid": "guid-value",
      "name": "Contoso Ltd",
      "revenue": 500000.00,
      "numberofemployees": 250
    }
  ]
}
```

### Retrieve Entity by ID

Get a single entity by its ID.

**Endpoint:** `GET /api/data/v9.2/{entityPluralName}({id})`

**Example:**
```bash
curl http://localhost:5000/api/data/v9.2/accounts(12345678-1234-1234-1234-123456789012)
```

**Response:**
```json
{
  "@odata.context": "#Microsoft.Dynamics.CRM.account/$entity",
  "accountid": "12345678-1234-1234-1234-123456789012",
  "name": "Contoso Ltd",
  "revenue": 500000.00,
  "numberofemployees": 250,
  "_primarycontactid_value": "abcd1234-...",
  "_primarycontactid_value@OData.Community.Display.V1.FormattedValue": "John Doe"
}
```

### Create Entity

Create a new entity record.

**Endpoint:** `POST /api/data/v9.2/{entityPluralName}`

**Headers:**
- `Content-Type: application/json`
- `Accept: application/json`

**Example:**
```bash
curl -X POST http://localhost:5000/api/data/v9.2/accounts \
  -H "Content-Type: application/json" \
  -d '{
    "name": "New Account",
    "revenue": 150000.00,
    "numberofemployees": 75
  }'
```

**Response:** `201 Created`
```json
{
  "id": "guid-of-created-entity"
}
```

**Headers:**
```
OData-EntityId: http://localhost:5000/api/data/v9.2/accounts(guid-of-created-entity)
```

### Update Entity

Update an existing entity (partial update).

**Endpoint:** `PATCH /api/data/v9.2/{entityPluralName}({id})`

**Headers:**
- `Content-Type: application/json`

**Example:**
```bash
curl -X PATCH http://localhost:5000/api/data/v9.2/accounts(12345678-...) \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Updated Name",
    "revenue": 200000.00
  }'
```

**Response:** `204 No Content`

### Delete Entity

Delete an entity record.

**Endpoint:** `DELETE /api/data/v9.2/{entityPluralName}({id})`

**Example:**
```bash
curl -X DELETE http://localhost:5000/api/data/v9.2/accounts(12345678-...)
```

**Response:** `204 No Content`

## OData Filter Expressions

The `$filter` query option supports the full OData v4.0 expression syntax via Microsoft.AspNetCore.OData.

**Reference:** [OData Query Options](https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/query-options)

### Comparison Operators

- `eq` - Equal
- `ne` - Not equal
- `gt` - Greater than
- `lt` - Less than
- `ge` - Greater than or equal
- `le` - Less than or equal

**Example:**
```bash
$filter=revenue gt 100000
$filter=statecode eq 0
$filter=createdon ge 2024-01-01
```

### Logical Operators

- `and` - Logical AND
- `or` - Logical OR
- `not` - Logical NOT

**Example:**
```bash
$filter=revenue gt 100000 and numberofemployees lt 500
$filter=industry eq 'Technology' or industry eq 'Finance'
$filter=not (statecode eq 1)
```

### String Functions

- `contains(field, 'value')` - Contains substring
- `startswith(field, 'value')` - Starts with
- `endswith(field, 'value')` - Ends with

**Example:**
```bash
$filter=contains(name, 'Corp')
$filter=startswith(name, 'Contoso')
$filter=endswith(emailaddress, '@example.com')
```

### Complex Expressions

Combine multiple conditions and functions:

```bash
$filter=revenue gt 100000 and (contains(name, 'Corp') or contains(name, 'Inc'))
$filter=statecode eq 0 and createdon ge 2024-01-01 and revenue lt 1000000
```

## Data Type Conversion

The REST API automatically converts between OData JSON formats and Dataverse SDK types:

**Reference:** [Web API Types and Operations](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations)

| Dataverse Type | OData JSON Format | Example |
|----------------|-------------------|---------|
| OptionSet | Integer | `"statecode": 0` |
| Money | Decimal | `"revenue": 100000.50` |
| EntityReference | Lookup value | `"_primarycontactid_value": "guid"` |
| DateTime | ISO 8601 string | `"createdon": "2024-01-15T10:30:45Z"` |
| Boolean | Lowercase | `"donotemail": true` |
| Guid | String | `"accountid": "guid-string"` |

## Error Responses

Errors follow the OData error format:

**Example:**
```json
{
  "error": {
    "code": "0x80040217",
    "message": "Entity with id guid not found",
    "innererror": {
      "message": "Detailed error message",
      "type": "InvalidOperationException",
      "stacktrace": "..."
    }
  }
}
```

**HTTP Status Codes:**
- `200 OK` - Successful GET
- `201 Created` - Successful POST
- `204 No Content` - Successful PATCH/DELETE
- `404 Not Found` - Entity not found
- `500 Internal Server Error` - Server error

## Using with Power Automate

The REST API is compatible with the "Dataverse" connector in Power Automate, which uses the same OData Web API protocol.

**Reference:** [Dataverse Connector](https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/)

## Using with JavaScript/TypeScript

```typescript
// Example using fetch API
const baseUrl = 'http://localhost:5000/api/data/v9.2';

// List accounts with filter
const response = await fetch(
  `${baseUrl}/accounts?$filter=revenue gt 100000&$select=name,revenue`,
  {
    headers: {
      'Accept': 'application/json',
      'OData-MaxVersion': '4.0',
      'OData-Version': '4.0'
    }
  }
);

const data = await response.json();
console.log(data.value); // Array of accounts

// Create a new account
const createResponse = await fetch(`${baseUrl}/accounts`, {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Accept': 'application/json'
  },
  body: JSON.stringify({
    name: 'New Account',
    revenue: 250000,
    numberofemployees: 100
  })
});

const created = await createResponse.json();
console.log('Created ID:', created.id);
```

## Using with C# HttpClient

```csharp
using System.Net.Http;
using System.Net.Http.Json;

var client = new HttpClient
{
    BaseAddress = new Uri("http://localhost:5000/api/data/v9.2")
};

// Add OData headers
client.DefaultRequestHeaders.Add("OData-MaxVersion", "4.0");
client.DefaultRequestHeaders.Add("OData-Version", "4.0");

// Query accounts
var response = await client.GetAsync("/accounts?$filter=revenue gt 100000");
var content = await response.Content.ReadAsStringAsync();

// Create account
var account = new Dictionary<string, object>
{
    ["name"] = "Test Account",
    ["revenue"] = 150000m
};

var createResponse = await client.PostAsJsonAsync("/accounts", account);
```

## Differences from Dataverse Web API

Fake4Dataverse REST API aims for compatibility with the Microsoft Dataverse Web API, but there are some differences:

**What's Supported:**
- ✅ Full CRUD operations
- ✅ OData query options ($select, $filter, $orderby, $top, $skip, $count)
- ✅ Complex filter expressions via Microsoft.AspNetCore.OData
- ✅ Data type conversions
- ✅ OData error responses

**What's Not Yet Implemented:**
- ❌ $metadata endpoint
- ❌ $batch requests
- ❌ Custom actions and functions
- ❌ Delta queries
- ❌ Optimistic concurrency (If-Match headers)
- ❌ Entity set navigation properties

These features may be added in future versions.

## See Also

- [SOAP/WCF Organization Service Endpoints](./service.md)
- [Cloud Flow Simulation](./usage/cloud-flows.md)
- [Microsoft Dataverse Web API](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview)
- [OData v4.0 Protocol](https://www.odata.org/documentation/)
