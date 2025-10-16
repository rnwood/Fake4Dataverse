# Xrm JavaScript API Implementation

This document describes the Xrm JavaScript API implementation for form scripts in Fake4Dataverse.

## Overview

The Xrm JavaScript API provides a way to interact with forms, fields, and data in Dynamics 365 / Dataverse model-driven apps. Form scripts (JavaScript files stored as WebResources) can use this API to customize form behavior, validate data, and control the user interface.

**Reference:** [Xrm Client API Reference](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference)

## Implementation

The implementation is split across two files:

- `app/lib/xrm-api-types.ts` - TypeScript interface definitions
- `app/lib/xrm-api.ts` - Implementation of the Xrm API

## Supported APIs

### Xrm.Page (Legacy API)

While `Xrm.Page` is deprecated in favor of `FormContext`, it's still widely used in existing code. Our implementation supports both.

```javascript
// Get attribute value
var accountName = Xrm.Page.getAttribute("name");
var value = accountName.getValue();

// Set attribute value
accountName.setValue("New Account Name");

// Get control
var control = Xrm.Page.getControl("name");
control.setDisabled(true);
```

**Reference:** [Xrm.Page](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-page)

### FormContext

The modern way to access form data and UI.

```javascript
function OnLoad(executionContext) {
    var formContext = executionContext.getFormContext();
    
    // Access entity data
    var entityName = formContext.data.entity.getEntityName();
    var entityId = formContext.data.entity.getId();
    
    // Get attribute
    var attr = formContext.getAttribute("name");
    attr.setValue("Test");
    
    // Add onChange handler
    attr.addOnChange(function() {
        console.log("Value changed:", attr.getValue());
    });
}
```

**Reference:** [FormContext](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/formcontext)

### Xrm.Utility

Utility functions for displaying dialogs and progress indicators.

```javascript
// Alert dialog
Xrm.Utility.alertDialog("This is a message", function() {
    console.log("Dialog closed");
});

// Confirm dialog
Xrm.Utility.confirmDialog("Are you sure?", 
    function() { console.log("Yes"); },
    function() { console.log("No"); }
);

// Progress indicator
Xrm.Utility.showProgressIndicator("Processing...");
setTimeout(function() {
    Xrm.Utility.closeProgressIndicator();
}, 2000);
```

**Reference:** [Xrm.Utility](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-utility)

### Xrm.WebApi

CRUD operations using the Dataverse Web API.

```javascript
// Retrieve a record
Xrm.WebApi.retrieveRecord("account", accountId).then(
    function(result) {
        console.log("Account:", result.name);
    },
    function(error) {
        console.error("Error:", error);
    }
);

// Create a record
var data = {
    name: "Test Account",
    telephone1: "555-1234"
};

Xrm.WebApi.createRecord("account", data).then(
    function(result) {
        console.log("Created:", result.id);
    }
);

// Update a record
var updates = { telephone1: "555-5678" };
Xrm.WebApi.updateRecord("account", accountId, updates);

// Delete a record
Xrm.WebApi.deleteRecord("account", accountId);
```

**Reference:** [Xrm.WebApi](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-webapi)

### Xrm.Navigation

Navigation and dialog functions.

```javascript
// Open alert dialog
Xrm.Navigation.openAlertDialog({
    text: "This is an alert",
    title: "Alert"
});

// Open confirm dialog
Xrm.Navigation.openConfirmDialog({
    text: "Are you sure?",
    title: "Confirm"
}).then(function(result) {
    if (result.confirmed) {
        console.log("User confirmed");
    }
});

// Open URL
Xrm.Navigation.openUrl("https://example.com", {
    newWindow: true
});
```

**Reference:** [Xrm.Navigation](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/xrm-navigation)

## Attributes

Attributes represent form fields.

```javascript
var attribute = formContext.getAttribute("name");

// Get/Set value
var value = attribute.getValue();
attribute.setValue("New Value");

// Required level
attribute.setRequiredLevel("required"); // "none", "required", "recommended"
var level = attribute.getRequiredLevel();

// onChange handlers
function myHandler() {
    console.log("Value changed!");
}
attribute.addOnChange(myHandler);
attribute.removeOnChange(myHandler);
attribute.fireOnChange(); // Manually trigger
```

**Reference:** [Attributes](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/attributes)

## Controls

Controls represent UI elements on the form.

```javascript
var control = formContext.getControl("name");

// Visibility
control.setVisible(true);
var isVisible = control.getVisible();

// Disabled state
control.setDisabled(true);
var isDisabled = control.getDisabled();

// Label
control.setLabel("New Label");
var label = control.getLabel();

// Focus
control.setFocus();

// Get associated attribute
var attribute = control.getAttribute();
```

**Reference:** [Controls](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/controls)

## Form Events

### OnLoad Event

Executed when the form loads.

```javascript
function OnLoad(executionContext) {
    var formContext = executionContext.getFormContext();
    
    // Initialize form
    var name = formContext.getAttribute("name");
    if (!name.getValue()) {
        name.setValue("New Record");
    }
    
    // Show notification
    formContext.ui.setFormNotification(
        "Form loaded successfully",
        "INFO",
        "load_notification"
    );
}
```

**FormXML Configuration:**
```xml
<events>
  <event name="onload" application="true" active="true">
    <Handler functionName="OnLoad" 
             libraryName="your_script.js" 
             handlerUniqueId="{guid}"
             enabled="true"
             parameters=""
             passExecutionContext="true" />
  </event>
</events>
```

**Reference:** [Form OnLoad](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/events/form-onload)

### OnSave Event

Executed when the form is saved.

```javascript
function OnSave(executionContext) {
    var formContext = executionContext.getFormContext();
    
    // Validate before save
    var name = formContext.getAttribute("name");
    if (!name.getValue()) {
        executionContext.getEventArgs().preventDefault();
        Xrm.Navigation.openAlertDialog({
            text: "Name is required"
        });
        return false;
    }
    
    return true;
}
```

**FormXML Configuration:**
```xml
<event name="onsave" application="true" active="true">
  <Handler functionName="OnSave" 
           libraryName="your_script.js" 
           handlerUniqueId="{guid}"
           enabled="true"
           passExecutionContext="true" />
</event>
```

**Reference:** [Form OnSave](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/events/form-onsave)

### OnChange Event

Executed when a field value changes.

```javascript
function OnNameChange(executionContext) {
    var formContext = executionContext.getFormContext();
    var name = formContext.getAttribute("name");
    var value = name.getValue();
    
    // Validate length
    if (value && value.length < 3) {
        formContext.ui.setFormNotification(
            "Name should be at least 3 characters",
            "WARNING",
            "name_validation"
        );
    } else {
        formContext.ui.clearFormNotification("name_validation");
    }
}
```

**FormXML Configuration:**
```xml
<event name="onchange" application="true" active="true" attribute="name">
  <Handler functionName="OnNameChange" 
           libraryName="your_script.js" 
           handlerUniqueId="{guid}"
           enabled="true"
           passExecutionContext="true" />
</event>
```

**Reference:** [Attribute OnChange](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/events/attribute-onchange)

## Form Notifications

Display messages on the form.

```javascript
// Set notification
formContext.ui.setFormNotification(
    "This is a message",
    "INFO",        // "ERROR", "WARNING", or "INFO"
    "unique_id"
);

// Clear notification
formContext.ui.clearFormNotification("unique_id");
```

## WebResources

JavaScript files are stored as WebResource entities.

### Creating a WebResource

```csharp
var scriptContent = @"
function OnLoad(executionContext) {
    console.log('Form loaded');
}
";

var base64Content = Convert.ToBase64String(
    System.Text.Encoding.UTF8.GetBytes(scriptContent)
);

var webResource = new Entity("webresource")
{
    Id = Guid.NewGuid(),
    ["name"] = "your_script.js",
    ["displayname"] = "Your Script",
    ["webresourcetype"] = 3, // JavaScript
    ["content"] = base64Content
};
service.Create(webResource);
```

**Reference:** [WebResource Entity](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource)

### Referencing in FormXML

```xml
<form>
  <formLibraries>
    <Library name="your_script.js" libraryUniqueId="{guid}" />
  </formLibraries>
  <!-- events... -->
</form>
```

## Complete Example

Here's a complete example from the MdaInitializer:

```javascript
function OnLoad(executionContext) {
    console.log('Account form OnLoad event triggered');
    
    var formContext = executionContext.getFormContext();
    
    // Get the account name
    var accountName = formContext.getAttribute('name');
    if (accountName) {
        console.log('Account Name:', accountName.getValue());
        
        // Add onChange handler
        accountName.addOnChange(function() {
            console.log('Account name changed to:', accountName.getValue());
        });
    }
    
    // Example: Control UI
    var accountNumberControl = formContext.getControl('accountnumber');
    if (accountNumberControl) {
        accountNumberControl.setDisabled(false);
    }
    
    // Example: Show notification
    formContext.ui.setFormNotification(
        'Welcome to the Account form!', 
        'INFO', 
        'welcome_notification'
    );
    
    // Clear notification after 3 seconds
    setTimeout(function() {
        formContext.ui.clearFormNotification('welcome_notification');
    }, 3000);
}

function OnSave(executionContext) {
    console.log('Account form OnSave event triggered');
    
    var formContext = executionContext.getFormContext();
    
    // Validate account name
    var accountName = formContext.getAttribute('name');
    if (accountName && !accountName.getValue()) {
        executionContext.getEventArgs().preventDefault();
        Xrm.Navigation.openAlertDialog({
            text: 'Account Name is required',
            title: 'Validation Error'
        });
        return false;
    }
    
    return true;
}

function ValidateAccountName(executionContext) {
    var formContext = executionContext.getFormContext();
    var accountName = formContext.getAttribute('name');
    
    if (accountName) {
        var value = accountName.getValue();
        if (value && value.length < 3) {
            formContext.ui.setFormNotification(
                'Account name should be at least 3 characters', 
                'WARNING', 
                'name_validation'
            );
        } else {
            formContext.ui.clearFormNotification('name_validation');
        }
    }
}
```

## Limitations

Current limitations in the implementation:

1. **Tabs and Sections**: Tab/section manipulation not yet implemented
2. **Lookup Controls**: Lookup fields not yet supported
3. **OptionSet Controls**: OptionSet manipulation not yet implemented
4. **Subgrids**: Subgrid API not yet implemented
5. **Business Rules**: Business rules engine not implemented
6. **Form Ribbon**: Command bar customization not implemented

## Testing

The Xrm API is automatically initialized when a form loads. To test:

1. Create a WebResource with your JavaScript code
2. Reference it in the SystemForm's FormXML
3. Define event handlers in FormXML
4. Load the form - scripts will execute automatically

## Debugging

Form scripts can be debugged using browser developer tools:

1. Open Developer Tools (F12)
2. Load the form
3. Check Console for log messages
4. Set breakpoints in your scripts
5. Use `console.log()` for debugging

## References

- [Xrm Client API Reference](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference)
- [FormContext](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/formcontext)
- [Form Events](https://learn.microsoft.com/en-us/power-apps/developer/model-driven-apps/clientapi/reference/events)
- [WebResource Entity](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/webresource)
