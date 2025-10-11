using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Represents the definition of a business rule in Dataverse.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule
    /// "Business rules provide a simple interface to implement and maintain fast-changing and commonly used rules.
    /// Business rules are defined visually, and run on both the client and server, providing a consistent experience across platforms."
    /// 
    /// Business rules use IF-THEN logic to validate data and execute actions based on conditions.
    /// They can run server-side (all API calls), client-side (forms), or both, depending on the scope.
    /// </summary>
    public class BusinessRuleDefinition
    {
        /// <summary>
        /// Gets or sets the unique name of the business rule.
        /// Used to identify and reference the rule.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#create-a-business-rule
        /// "Give your business rule a descriptive name"
        /// 
        /// Example: "ValidateCreditLimit", "RequirePhoneForHighValueLeads"
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Gets or sets the description of what this business rule does.
        /// Used for documentation and understanding the rule's purpose.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#best-practices
        /// "Provide clear descriptions to help others understand the rule's purpose"
        /// </summary>
        public string Description { get; set; }
        
        /// <summary>
        /// Gets or sets the logical name of the entity this rule applies to.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#scope
        /// "Business rules are created for a specific table (entity)"
        /// 
        /// Example: "account", "contact", "opportunity"
        /// </summary>
        public string EntityLogicalName { get; set; }
        
        /// <summary>
        /// Gets or sets the scope where this rule executes.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#set-business-rule-scope
        /// "The scope determines where the business rule runs: Entity (server-side), All Forms (client and server), or a specific form"
        /// 
        /// - Entity: Server-side only (API calls, workflows, plugins)
        /// - All: Both client-side forms and server-side
        /// - Form: Specific form only (client-side)
        /// </summary>
        public BusinessRuleScope Scope { get; set; }
        
        /// <summary>
        /// Gets or sets when this rule is triggered.
        /// Can be a combination of triggers using bitwise flags.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#when-rules-run
        /// "Business rules run when the form loads, when field values change, and when records are saved"
        /// 
        /// Example: OnCreate | OnUpdate means the rule runs for both create and update operations.
        /// </summary>
        public BusinessRuleTrigger Trigger { get; set; }
        
        /// <summary>
        /// Gets or sets whether this rule is active.
        /// Inactive rules are not executed.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#activate-or-deactivate
        /// "Business rules must be activated before they take effect. Deactivate rules to temporarily disable them."
        /// </summary>
        public bool IsActive { get; set; }
        
        /// <summary>
        /// Gets the list of conditions that must be met for actions to execute.
        /// All conditions must evaluate to true (AND logic by default).
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#add-conditions
        /// "Add one or more conditions to determine when the business rule actions should be applied"
        /// 
        /// Multiple conditions are combined with AND logic. For OR logic, create multiple business rules.
        /// </summary>
        public List<BusinessRuleCondition> Conditions { get; set; }
        
        /// <summary>
        /// Gets the list of actions to execute when all conditions are met.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#add-actions
        /// "Add one or more actions that should be performed when the conditions are true"
        /// 
        /// Actions are executed in the order they are defined.
        /// Server-scope rules can only execute server-side compatible actions (SetFieldValue, ShowErrorMessage, etc.)
        /// </summary>
        public List<BusinessRuleAction> Actions { get; set; }
        
        /// <summary>
        /// Gets or sets the list of actions to execute when conditions are NOT met (ELSE branch).
        /// Optional - only used if you need to perform actions when conditions fail.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#else-actions
        /// "You can define actions to take when conditions are not met using the Else branch"
        /// 
        /// Else actions are useful for toggling field visibility, clearing values, etc.
        /// </summary>
        public List<BusinessRuleAction> ElseActions { get; set; }
        
        /// <summary>
        /// Gets or sets whether to use AND or OR logic for combining conditions.
        /// Default is true (AND logic - all conditions must be true).
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#condition-logic
        /// "By default, all conditions must be true (AND). You can change to OR logic where any condition being true triggers the actions."
        /// 
        /// - True (default): All conditions must be true (AND logic)
        /// - False: At least one condition must be true (OR logic)
        /// </summary>
        public bool UseAndLogic { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessRuleDefinition"/> class.
        /// </summary>
        public BusinessRuleDefinition()
        {
            Conditions = new List<BusinessRuleCondition>();
            Actions = new List<BusinessRuleAction>();
            ElseActions = new List<BusinessRuleAction>();
            IsActive = true;
            UseAndLogic = true;
            Scope = BusinessRuleScope.All;
            Trigger = BusinessRuleTrigger.OnCreate | BusinessRuleTrigger.OnUpdate;
        }
        
        /// <summary>
        /// Validates that the business rule definition is properly configured.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#validation
        /// "Business rules must have at least one action to be valid"
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the rule is not properly configured</exception>
        public void Validate()
        {
            if (string.IsNullOrEmpty(Name))
            {
                throw new InvalidOperationException("Business rule must have a Name");
            }
            
            if (string.IsNullOrEmpty(EntityLogicalName))
            {
                throw new InvalidOperationException($"Business rule '{Name}' must specify an EntityLogicalName");
            }
            
            if (!Actions.Any() && !ElseActions.Any())
            {
                throw new InvalidOperationException($"Business rule '{Name}' must have at least one action");
            }
            
            // Validate conditions
            foreach (var condition in Conditions)
            {
                if (string.IsNullOrEmpty(condition.FieldName))
                {
                    throw new InvalidOperationException($"Business rule '{Name}' has a condition without a FieldName");
                }
            }
            
            // Validate actions
            foreach (var action in Actions.Concat(ElseActions))
            {
                if (string.IsNullOrEmpty(action.FieldName) && 
                    action.ActionType != BusinessRuleActionType.ShowErrorMessage &&
                    action.ActionType != BusinessRuleActionType.SetBusinessRecommendation)
                {
                    throw new InvalidOperationException($"Business rule '{Name}' has an action without a FieldName");
                }
            }
        }
        
        /// <summary>
        /// Determines whether this business rule should execute for the given trigger.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#triggers
        /// "Business rules can be configured to run on specific events like Create, Update, or field changes"
        /// </summary>
        /// <param name="trigger">The trigger to check</param>
        /// <returns>True if the rule should execute for this trigger</returns>
        public bool ShouldExecuteForTrigger(BusinessRuleTrigger trigger)
        {
            return IsActive && (Trigger & trigger) == trigger;
        }
        
        /// <summary>
        /// Determines whether this business rule should execute for the given scope.
        /// Server-side execution should only use Entity or All scope rules.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#scope
        /// "Entity scope rules run on the server for all operations. Form scope rules only run in the client."
        /// </summary>
        /// <param name="isServerSide">Whether this is server-side execution</param>
        /// <returns>True if the rule should execute in this context</returns>
        public bool ShouldExecuteForContext(bool isServerSide)
        {
            if (!isServerSide)
            {
                // Client-side (forms) can execute all scopes
                return IsActive;
            }
            
            // Server-side should only execute Entity or All scope rules
            return IsActive && (Scope == BusinessRuleScope.Entity || Scope == BusinessRuleScope.All);
        }
    }
}
