namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Defines when a business rule is triggered during record operations.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
    /// "Business rules run when the form loads and when field values change."
    /// 
    /// Business rules can be triggered by different events depending on their configuration:
    /// - On record create (before save)
    /// - On record update (before save)
    /// - On field value change (real-time as user types or when field loses focus)
    /// - On form load (when the form opens)
    /// </summary>
    public enum BusinessRuleTrigger
    {
        /// <summary>
        /// Rule executes when a new record is created.
        /// Runs during the Create operation before the record is saved.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
        /// "Business rules can validate data and set field values when a record is created"
        /// </summary>
        OnCreate = 1,
        
        /// <summary>
        /// Rule executes when an existing record is updated.
        /// Runs during the Update operation before changes are saved.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
        /// "Business rules can validate data and set field values when a record is updated"
        /// </summary>
        OnUpdate = 2,
        
        /// <summary>
        /// Rule executes when a specific field value changes.
        /// For forms, this happens as the user modifies field values.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#conditions
        /// "You can set conditions based on field values and execute actions when those conditions are met"
        /// </summary>
        OnChange = 4,
        
        /// <summary>
        /// Rule executes when a form is loaded (form scope only).
        /// Used to set initial field values, visibility, and requirements.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
        /// "Business rules run automatically when a form loads"
        /// </summary>
        OnLoad = 8
    }
}
