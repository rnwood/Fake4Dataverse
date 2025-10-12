namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Defines the scope where a business rule executes.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
    /// "Business rules provide a simple interface to implement and maintain fast-changing and commonly used rules.
    /// They can be applied to a table (entity) or a form depending on the scope selected."
    /// 
    /// Business rules can run on the client-side (forms), server-side (table/entity), or both.
    /// The scope determines where and when the business rule logic is evaluated.
    /// </summary>
    public enum BusinessRuleScope
    {
        /// <summary>
        /// Rule applies to all forms and server-side operations.
        /// This is the most comprehensive scope and ensures the rule is enforced everywhere.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-business-rule-scope
        /// "All Forms and Server: The rule runs when the record is created or updated in any form or through the Web API"
        /// </summary>
        All = 0,
        
        /// <summary>
        /// Rule applies only to server-side operations (API calls, plugins, workflows).
        /// Client-side forms do not evaluate this rule.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-business-rule-scope
        /// "Entity: The rule runs only when the record is created or updated through the Web API or in a workflow"
        /// </summary>
        Entity = 1,
        
        /// <summary>
        /// Rule applies only to a specific form.
        /// Server-side operations do not evaluate this rule.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-business-rule-scope
        /// "Specific form: The rule runs only when working with a specific form"
        /// </summary>
        Form = 2
    }
}
