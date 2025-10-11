using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace Fake4Dataverse.BusinessRules
{
    /// <summary>
    /// Represents a condition in a business rule that determines when actions should execute.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#conditions
    /// "Conditions determine when business rule actions are applied. You can create conditions that check field values
    /// and perform actions when those conditions are met."
    /// 
    /// Business rule conditions use IF-THEN logic to evaluate field values and trigger actions.
    /// Multiple conditions can be combined using AND/OR operators.
    /// </summary>
    public class BusinessRuleCondition
    {
        /// <summary>
        /// Gets or sets the logical name of the field to evaluate.
        /// This is the attribute that the condition will check.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#add-conditions
        /// "Select the field you want to set a condition for"
        /// 
        /// Example: "statuscode", "creditlimit", "lastname"
        /// </summary>
        public string FieldName { get; set; }
        
        /// <summary>
        /// Gets or sets the operator used to compare the field value.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#operators
        /// "Available operators include: Equals, Does not equal, Contains, Does not contain, Is null, Is not null,
        /// Greater than, Less than, Greater than or equal to, Less than or equal to"
        /// 
        /// Common operators: Equals, NotEquals, GreaterThan, LessThan, Contains, DoesNotContain, IsNull, IsNotNull
        /// </summary>
        public ConditionOperator Operator { get; set; }
        
        /// <summary>
        /// Gets or sets the value to compare against.
        /// Can be a static value or a reference to another field.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#condition-values
        /// "You can compare a field to a specific value or to the value of another field"
        /// 
        /// For field references, use the field name.
        /// For static values, use the appropriate type (string, int, decimal, DateTime, etc.)
        /// </summary>
        public object Value { get; set; }
        
        /// <summary>
        /// Gets or sets whether this condition should negate the result (NOT operator).
        /// When true, the condition passes when it would normally fail.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#condition-logic
        /// "You can use NOT logic to invert condition results"
        /// 
        /// Example: If FieldName Equals Value is false, but Negate is true, the condition passes.
        /// </summary>
        public bool Negate { get; set; }
        
        /// <summary>
        /// Evaluates whether this condition is met by the given entity.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-create-business-rule#how-conditions-are-evaluated
        /// "Conditions are evaluated when the record is created, updated, or when field values change.
        /// All conditions in a business rule must be true for the actions to execute."
        /// 
        /// This method checks if the entity's field value matches the condition based on the operator.
        /// Returns true if the condition is met, false otherwise.
        /// </summary>
        /// <param name="entity">The entity to evaluate</param>
        /// <returns>True if the condition is met, false otherwise</returns>
        public bool Evaluate(Entity entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }
            
            if (string.IsNullOrEmpty(FieldName))
            {
                throw new InvalidOperationException("FieldName must be specified for business rule condition");
            }
            
            // Get the field value from the entity
            object fieldValue = entity.Contains(FieldName) ? entity[FieldName] : null;
            
            bool result = EvaluateOperator(fieldValue, Value, Operator);
            
            // Apply negation if specified
            return Negate ? !result : result;
        }
        
        /// <summary>
        /// Evaluates the condition based on the operator.
        /// Internal method that performs the actual comparison logic.
        /// </summary>
        private bool EvaluateOperator(object fieldValue, object compareValue, ConditionOperator op)
        {
            // Handle null checks first
            if (op == ConditionOperator.Null)
            {
                return fieldValue == null;
            }
            
            if (op == ConditionOperator.NotNull)
            {
                return fieldValue != null;
            }
            
            // If field value is null and we're not checking for null, condition fails
            if (fieldValue == null)
            {
                return false;
            }
            
            // Handle different operators
            switch (op)
            {
                case ConditionOperator.Equal:
                    return AreEqual(fieldValue, compareValue);
                    
                case ConditionOperator.NotEqual:
                    return !AreEqual(fieldValue, compareValue);
                    
                case ConditionOperator.GreaterThan:
                    return IsGreaterThan(fieldValue, compareValue);
                    
                case ConditionOperator.GreaterEqual:
                    return IsGreaterThanOrEqual(fieldValue, compareValue);
                    
                case ConditionOperator.LessThan:
                    return IsLessThan(fieldValue, compareValue);
                    
                case ConditionOperator.LessEqual:
                    return IsLessThanOrEqual(fieldValue, compareValue);
                    
                case ConditionOperator.Like:
                case ConditionOperator.BeginsWith:
                case ConditionOperator.Contains:
                case ConditionOperator.EndsWith:
                    return ContainsCheck(fieldValue, compareValue, op);
                    
                case ConditionOperator.NotLike:
                case ConditionOperator.DoesNotBeginWith:
                case ConditionOperator.DoesNotContain:
                case ConditionOperator.DoesNotEndWith:
                    return !ContainsCheck(fieldValue, compareValue, op);
                    
                default:
                    throw new NotSupportedException($"Operator {op} is not supported in business rules");
            }
        }
        
        private bool AreEqual(object fieldValue, object compareValue)
        {
            if (fieldValue == null && compareValue == null) return true;
            if (fieldValue == null || compareValue == null) return false;
            
            // Handle OptionSetValue
            if (fieldValue is OptionSetValue osv)
            {
                fieldValue = osv.Value;
            }
            
            // Handle Money
            if (fieldValue is Money money)
            {
                fieldValue = money.Value;
            }
            
            // Handle EntityReference
            if (fieldValue is EntityReference er && compareValue is Guid guid)
            {
                return er.Id == guid;
            }
            
            // Try direct equality
            if (fieldValue.Equals(compareValue))
            {
                return true;
            }
            
            // Try converting types
            try
            {
                var convertedValue = Convert.ChangeType(compareValue, fieldValue.GetType());
                return fieldValue.Equals(convertedValue);
            }
            catch
            {
                return false;
            }
        }
        
        private bool IsGreaterThan(object fieldValue, object compareValue)
        {
            if (fieldValue is IComparable comparable && compareValue != null)
            {
                var convertedValue = Convert.ChangeType(compareValue, fieldValue.GetType());
                return comparable.CompareTo(convertedValue) > 0;
            }
            return false;
        }
        
        private bool IsGreaterThanOrEqual(object fieldValue, object compareValue)
        {
            if (fieldValue is IComparable comparable && compareValue != null)
            {
                var convertedValue = Convert.ChangeType(compareValue, fieldValue.GetType());
                return comparable.CompareTo(convertedValue) >= 0;
            }
            return false;
        }
        
        private bool IsLessThan(object fieldValue, object compareValue)
        {
            if (fieldValue is IComparable comparable && compareValue != null)
            {
                var convertedValue = Convert.ChangeType(compareValue, fieldValue.GetType());
                return comparable.CompareTo(convertedValue) < 0;
            }
            return false;
        }
        
        private bool IsLessThanOrEqual(object fieldValue, object compareValue)
        {
            if (fieldValue is IComparable comparable && compareValue != null)
            {
                var convertedValue = Convert.ChangeType(compareValue, fieldValue.GetType());
                return comparable.CompareTo(convertedValue) <= 0;
            }
            return false;
        }
        
        private bool ContainsCheck(object fieldValue, object compareValue, ConditionOperator op)
        {
            if (compareValue == null) return false;
            
            string fieldStr = fieldValue?.ToString() ?? string.Empty;
            string compareStr = compareValue.ToString();
            
            // Case-insensitive comparison
            fieldStr = fieldStr.ToLowerInvariant();
            compareStr = compareStr.ToLowerInvariant();
            
            switch (op)
            {
                case ConditionOperator.Contains:
                case ConditionOperator.DoesNotContain:
                    return fieldStr.Contains(compareStr);
                    
                case ConditionOperator.BeginsWith:
                case ConditionOperator.DoesNotBeginWith:
                    return fieldStr.StartsWith(compareStr);
                    
                case ConditionOperator.EndsWith:
                case ConditionOperator.DoesNotEndWith:
                    return fieldStr.EndsWith(compareStr);
                    
                case ConditionOperator.Like:
                case ConditionOperator.NotLike:
                    // Simple LIKE implementation - treat % as wildcard
                    compareStr = compareStr.Replace("%", ".*");
                    return System.Text.RegularExpressions.Regex.IsMatch(fieldStr, $"^{compareStr}$");
                    
                default:
                    return false;
            }
        }
    }
}
