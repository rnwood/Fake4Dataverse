namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Defines the types of actions that can be performed by business rules.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#actions
    /// "Business rules support various actions including setting field values, showing or hiding fields,
    /// enabling or disabling fields, validating data, and creating business recommendations."
    /// 
    /// Actions are executed when the business rule conditions are met.
    /// Each action type modifies different aspects of the record or form behavior.
    /// </summary>
    public enum BusinessRuleActionType
    {
        /// <summary>
        /// Sets a field to a specific value.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-field-value
        /// "Set Field Value: Set a column to a specific value, clear a column value, or set a column value based on another column"
        /// 
        /// This action can set static values or copy values from other fields.
        /// Commonly used to auto-populate fields based on business logic.
        /// </summary>
        SetFieldValue = 1,
        
        /// <summary>
        /// Shows or hides a field on the form.
        /// Server-side rules cannot use this action.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#show-or-hide-field
        /// "Show or Hide Field: Control field visibility based on conditions"
        /// 
        /// Used to create dynamic forms that show/hide fields based on user selections.
        /// Only applicable to form scope rules.
        /// </summary>
        ShowHideField = 2,
        
        /// <summary>
        /// Enables or disables a field on the form.
        /// Disabled fields appear grayed out and users cannot edit them.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#enable-or-disable-field
        /// "Enable or Disable Field: Make a field read-only or editable based on conditions"
        /// 
        /// Commonly used to lock fields after certain conditions are met.
        /// Only applicable to form scope rules.
        /// </summary>
        EnableDisableField = 3,
        
        /// <summary>
        /// Sets the business required level of a field.
        /// Can make fields required, recommended, or optional.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-business-required
        /// "Set Business Required: Make a field required, recommended, or optional"
        /// 
        /// Business Required level: Required (must have value), Recommended (visual indicator but not enforced), None (optional)
        /// This overrides the field's default requirement level.
        /// </summary>
        SetBusinessRequired = 4,
        
        /// <summary>
        /// Shows an error message to the user and prevents saving.
        /// Used for data validation.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#show-error-message
        /// "Show Error Message: Display a custom error message and prevent the record from being saved"
        /// 
        /// Error messages appear next to the field or at the top of the form.
        /// The save operation is blocked until the error condition is resolved.
        /// </summary>
        ShowErrorMessage = 5,
        
        /// <summary>
        /// Creates a business recommendation for the user.
        /// Recommendations are suggestions that don't block saving.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-business-recommendation
        /// "Set Business Recommendation: Provide guidance to users without blocking them"
        /// 
        /// Recommendations appear as information messages to guide users.
        /// Unlike errors, they don't prevent the record from being saved.
        /// </summary>
        SetBusinessRecommendation = 6,
        
        /// <summary>
        /// Locks or unlocks a field to prevent or allow editing.
        /// Similar to Enable/Disable but specifically for locking after certain conditions.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#lock-or-unlock-field
        /// "Lock or Unlock Field: Permanently lock a field to prevent any changes"
        /// 
        /// Locked fields cannot be edited even by users with appropriate permissions.
        /// Commonly used for auditing or compliance requirements.
        /// </summary>
        LockUnlockField = 7,
        
        /// <summary>
        /// Sets a default value for a field.
        /// Only applies when the field is empty.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-default-value
        /// "Set Default Value: Provide a default value for a field when the record is created"
        /// 
        /// Default values are applied during record creation or when the field is empty.
        /// Different from SetFieldValue which always overwrites the current value.
        /// </summary>
        SetDefaultValue = 8,
        
        /// <summary>
        /// Clears the value of a field.
        /// Sets the field to null or empty.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#clear-field-value
        /// "Clear Field Value: Remove the value from a field"
        /// 
        /// Used to reset fields when conditions change.
        /// Commonly used with conditional logic to clear dependent fields.
        /// </summary>
        ClearFieldValue = 9
    }
}
