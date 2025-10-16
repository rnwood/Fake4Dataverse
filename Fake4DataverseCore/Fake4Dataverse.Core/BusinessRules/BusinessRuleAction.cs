using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Represents an action to be executed when business rule conditions are met.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#actions
    /// "Actions are the operations that are performed when the business rule conditions are true.
    /// You can define multiple actions for a single business rule."
    /// 
    /// Actions modify entity field values, visibility, or validation state when conditions are met.
    /// Common actions include setting field values, showing/hiding fields, and displaying errors.
    /// </summary>
    public class BusinessRuleAction
    {
        /// <summary>
        /// Gets or sets the type of action to perform.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#action-types
        /// "Business rules support multiple action types including Set Field Value, Show Error Message,
        /// Set Business Required, Show or Hide Field, Enable or Disable Field, Lock or Unlock Field,
        /// and Set Business Recommendation"
        /// </summary>
        public BusinessRuleActionType ActionType { get; set; }
        
        /// <summary>
        /// Gets or sets the logical name of the field this action affects.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#selecting-fields
        /// "Specify which field the action should apply to"
        /// 
        /// Example: "statuscode", "creditlimit", "lastname"
        /// </summary>
        public string FieldName { get; set; }
        
        /// <summary>
        /// Gets or sets the value to use for the action.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#action-values
        /// "Specify the value to set, or the message to display"
        /// 
        /// - For SetFieldValue: The value to set (can be static or from another field)
        /// - For ShowErrorMessage: The error message text
        /// - For SetBusinessRecommendation: The recommendation message text
        /// - For SetBusinessRequired: "Required", "Recommended", or "None"
        /// - For ShowHideField: true to show, false to hide
        /// - For EnableDisableField: true to enable, false to disable
        /// - For LockUnlockField: true to lock, false to unlock
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Gets or sets the message to display (for error or recommendation actions).
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#error-messages
        /// "Provide a clear, actionable error message that helps users understand what they need to do"
        /// 
        /// Used with ShowErrorMessage and SetBusinessRecommendation actions.
        /// The message should be user-friendly and explain how to resolve the issue.
        /// </summary>
        public string Message { get; set; }
        
        /// <summary>
        /// Executes this action on the given entity.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#action-execution
        /// "Actions are executed in the order they are defined when all conditions are met"
        /// 
        /// This method applies the action's effect to the entity based on the action type.
        /// For server-side rules (Entity scope), only actions that modify data are executed.
        /// Form-only actions (show/hide, enable/disable) are tracked but not applied server-side.
        /// </summary>
        /// <param name="entity">The entity to apply the action to</param>
        /// <param name="result">The result object to record action execution</param>
        public void Execute(Entity entity, BusinessRuleExecutionResult result)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }
            
            switch (ActionType)
            {
                case BusinessRuleActionType.SetFieldValue:
                    ExecuteSetFieldValue(entity);
                    result.RecordAction(this, $"Set {FieldName} to {Value}");
                    break;
                    
                case BusinessRuleActionType.ClearFieldValue:
                    ExecuteClearFieldValue(entity);
                    result.RecordAction(this, $"Cleared {FieldName}");
                    break;
                    
                case BusinessRuleActionType.SetDefaultValue:
                    ExecuteSetDefaultValue(entity);
                    result.RecordAction(this, $"Set default value for {FieldName}");
                    break;
                    
                case BusinessRuleActionType.ShowErrorMessage:
                    ExecuteShowErrorMessage(result);
                    break;
                    
                case BusinessRuleActionType.SetBusinessRecommendation:
                    ExecuteSetBusinessRecommendation(result);
                    break;
                    
                case BusinessRuleActionType.ShowHideField:
                case BusinessRuleActionType.EnableDisableField:
                case BusinessRuleActionType.LockUnlockField:
                case BusinessRuleActionType.SetBusinessRequired:
                    // These are form-level actions that don't modify the entity directly
                    // They are recorded in the result for client-side processing
                    result.RecordFormAction(this);
                    break;
                    
                default:
                    throw new NotSupportedException($"Action type {ActionType} is not supported");
            }
        }
        
        private void ExecuteSetFieldValue(Entity entity)
        {
            if (string.IsNullOrEmpty(FieldName))
            {
                throw new InvalidOperationException("FieldName must be specified for SetFieldValue action");
            }
            
            entity[FieldName] = Value;
        }
        
        private void ExecuteClearFieldValue(Entity entity)
        {
            if (string.IsNullOrEmpty(FieldName))
            {
                throw new InvalidOperationException("FieldName must be specified for ClearFieldValue action");
            }
            
            if (entity.Contains(FieldName))
            {
                entity[FieldName] = null;
            }
        }
        
        private void ExecuteSetDefaultValue(Entity entity)
        {
            if (string.IsNullOrEmpty(FieldName))
            {
                throw new InvalidOperationException("FieldName must be specified for SetDefaultValue action");
            }
            
            // Only set if field is empty/null
            if (!entity.Contains(FieldName) || entity[FieldName] == null)
            {
                entity[FieldName] = Value;
            }
        }
        
        private void ExecuteShowErrorMessage(BusinessRuleExecutionResult result)
        {
            if (string.IsNullOrEmpty(Message))
            {
                throw new InvalidOperationException("Message must be specified for ShowErrorMessage action");
            }
            
            result.AddError(FieldName, Message);
        }
        
        private void ExecuteSetBusinessRecommendation(BusinessRuleExecutionResult result)
        {
            if (string.IsNullOrEmpty(Message))
            {
                throw new InvalidOperationException("Message must be specified for SetBusinessRecommendation action");
            }
            
            result.AddRecommendation(FieldName, Message);
        }
    }
}
