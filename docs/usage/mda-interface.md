# Model-Driven App (MDA) Interface

**Issue:** [#116](https://github.com/rnwood/Fake4Dataverse/issues/116)

## Overview

The Fake4DataverseService includes a web-based Model-Driven App (MDA) interface that provides a visual way to interact with the fake Dataverse instance. This is particularly useful for:
- Manual testing and exploration of test data
- Debugging issues during test development
- Demonstrating test scenarios to stakeholders
- Validating impersonation and security configurations

## Accessing the MDA

When Fake4DataverseService is running, the MDA is accessible at:

```
http://localhost:{port}/mda/
```

Default: `http://localhost:5000/mda/`

## Key Features

### 1. User Management and Impersonation

The MDA header displays the current user context:

- **User Avatar**: Shows the currently active user (or impersonated user)
- **User Switcher**: Click the avatar to switch between users
- **Impersonation Indicator**: Visual indicator when impersonating another user
- **Real-time Update**: Operations immediately reflect the selected user context

#### Using the User Switcher

1. Click the user avatar in the top-right corner
2. Select a user from the dropdown list
3. All subsequent operations are performed as that user
4. The interface refreshes to show data visible to the selected user

#### Impersonation Workflow

When you switch users in the MDA:
1. The MDA sets the `MSCRMCallerID` HTTP header on all requests
2. The service applies impersonation for the selected user
3. Security checks use the impersonated user's permissions
4. Created/modified records show the correct audit trail

### 2. Entity Data Browsing

- View all entities in the fake instance
- Browse records with filtering and sorting
- View entity metadata and relationships
- Inspect field values including lookups and option sets

### 3. Record Operations

- **Create**: Add new records through forms
- **Update**: Modify existing records
- **Delete**: Remove records
- **Associate**: Link records via relationships

All operations respect the current user's security permissions.

### 4. Security Visualization

- See which records are visible to the current user
- Test security role configurations
- Verify business unit hierarchies
- Validate sharing permissions

## Setup and Configuration

### Starting the Service with MDA

```bash
# Start with default settings
fake4dataverse start --port 5000

# Custom configuration
fake4dataverse start --port 8080 --host 0.0.0.0
```

The MDA is automatically available at the `/mda/` endpoint.

### Creating Initial Users

When the service starts, it automatically creates a System Administrator user:

```csharp
// Automatically created on service startup
var systemAdmin = new Entity("systemuser")
{
    Id = Guid.NewGuid(),
    ["fullname"] = "System Administrator",
    ["businessunitid"] = rootBusinessUnit.ToEntityReference()
};
```

You can create additional users via:
1. The MDA interface itself
2. SOAP/OData API calls
3. Initialization scripts

### Configuring User List

The user switcher displays all systemuser records in the instance. To add users:

```csharp
// Via OData API
POST http://localhost:5000/api/data/v9.2/systemusers
Content-Type: application/json

{
    "fullname": "Test User",
    "businessunitid@odata.bind": "/businessunits({businessunitid})"
}
```

Or through the MDA's user management interface.

## Example Scenarios

### Scenario 1: Testing Security Roles

```
1. Start Fake4DataverseService
2. Open MDA in browser
3. Create two users: "Admin" and "Sales Rep"
4. Assign different security roles to each
5. Create some account records as Admin
6. Switch to Sales Rep user
7. Observe which records are visible
8. Try creating/updating records
9. Verify security enforcement
```

### Scenario 2: Impersonation Testing

```
1. Create Admin user with System Administrator role
2. Create Target user with limited permissions
3. Switch to Admin user in MDA
4. The backend automatically sets impersonation
5. Create an account record
6. Inspect the record to see:
   - createdby = Admin
   - createdonbehalfof = (not set, since not impersonating)
7. In your application, set impersonation headers
8. Create another record
9. Inspect to see both users in audit fields
```

### Scenario 3: Multi-User Workflows

```
1. Create users for different roles (Sales, Manager, Admin)
2. Switch between users to simulate workflow
3. Sales creates an opportunity
4. Manager reviews and updates
5. Admin performs administrative tasks
6. Verify audit trail shows correct user chain
```

## Technical Implementation

### Architecture

The MDA is a Next.js application embedded in the Fake4DataverseService:

```
┌─────────────────────────────────────┐
│        Browser (MDA UI)             │
│      (Next.js + React)              │
└────────────┬────────────────────────┘
             │ HTTP + MSCRMCallerID header
             │
┌────────────▼────────────────────────┐
│    Fake4DataverseService            │
│  ┌──────────────────────────────┐   │
│  │  Impersonation Middleware    │   │
│  │  (Extract MSCRMCallerID)     │   │
│  └──────────┬───────────────────┘   │
│             │                        │
│  ┌──────────▼───────────────────┐   │
│  │  OData/SOAP Endpoints        │   │
│  └──────────┬───────────────────┘   │
└─────────────┼───────────────────────┘
              │
   ┌──────────▼───────────────────┐
   │    Fake4DataverseCore        │
   │    (In-Memory Context)       │
   └──────────────────────────────┘
```

### HTTP Header Format

When a user is selected in the MDA, all requests include:

```http
GET /api/data/v9.2/accounts HTTP/1.1
Host: localhost:5000
MSCRMCallerID: {userId}
```

The middleware extracts this header and sets `CallerProperties.ImpersonatedUserId`.

### State Management

The MDA maintains user selection in:
1. Browser session storage
2. React context
3. HTTP headers on every request

Switching users:
1. Updates React state
2. Stores selection in session
3. Adds header to all subsequent requests
4. Refreshes data view

## Development and Testing

### Running MDA in Development

```bash
cd Fake4DataverseService/Fake4Dataverse.Service/mda-app

# Install dependencies
npm ci

# Run development server
npm run dev

# Run alongside service
dotnet run --project ../Fake4Dataverse.Service.csproj
```

### MDA Tests

```bash
# Unit tests
npm test

# E2E tests
npm run test:e2e
```

### Building for Production

```bash
# Build Next.js app
npm run build

# The build is embedded in the service package
dotnet pack
```

## Troubleshooting

### MDA Not Loading

**Problem:** Navigating to `/mda/` shows 404 or blank page

**Solutions:**
- Ensure Fake4DataverseService is running
- Check the correct port
- Verify the MDA app was built: `mda-app/out/` should exist
- Check console for JavaScript errors

### User Switcher Empty

**Problem:** No users appear in the switcher dropdown

**Solutions:**
- Create at least one systemuser record
- Verify systemuser entity is not filtered
- Check browser console for API errors
- Ensure OData endpoint is accessible

### Impersonation Not Working

**Problem:** Operations don't reflect the selected user

**Solutions:**
- Verify the MSCRMCallerID header is being sent (check browser dev tools)
- Ensure impersonation middleware is registered in service
- Check security is enabled: `SecurityConfiguration.SecurityEnabled = true`
- Verify user has necessary privileges

### Permission Errors

**Problem:** Operations fail with permission errors

**Solutions:**
- Verify the selected user has appropriate security roles
- Check business unit hierarchy
- Ensure privileges are assigned correctly
- Use System Administrator for debugging

## API Reference

### Getting Available Users

```http
GET /api/data/v9.2/systemusers?$select=systemuserid,fullname
```

### Setting Current User

```http
GET /api/data/v9.2/accounts
MSCRMCallerID: {userId}
```

### Getting Current User Context

```http
POST /api/data/v9.2/WhoAmI
```

Response includes the effective user when impersonating.

## Best Practices

1. **Create Meaningful Users**: Use descriptive names like "Admin User", "Sales Rep", "Manager"

2. **Assign Proper Roles**: Give each test user appropriate security roles for realistic testing

3. **Test Permission Boundaries**: Switch between users to verify security enforcement

4. **Clear Data Between Tests**: Use the MDA to inspect data state between test runs

5. **Document User Scenarios**: Maintain a list of test users and their purposes

## See Also

- [Impersonation](./impersonation.md) - Core impersonation concepts and API
- [Security Model](./security-model.md) - Dataverse security implementation
- [Fake4DataverseService](../service.md) - Service architecture and setup
- [REST API](../rest-api.md) - OData endpoint reference
