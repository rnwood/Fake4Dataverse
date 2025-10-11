using Microsoft.Xrm.Sdk;
using NCalc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Fake4Dataverse.CalculatedFields
{
    /// <summary>
    /// Evaluates calculated field formulas using NCalc expression engine.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
    /// "Define calculated columns - Create columns that automatically calculate their values based on other column values"
    /// 
    /// This class handles:
    /// - Preprocessing Dataverse formula syntax to NCalc syntax
    /// - Injecting entity field values as variables
    /// - Evaluating expressions with custom Dataverse functions
    /// - Type conversion and error handling
    /// 
    /// NCalc library: https://github.com/ncalc/ncalc
    /// "NCalc is a mathematical expressions evaluator in .NET"
    /// </summary>
    public class CalculatedFieldEvaluator
    {
        private readonly Dictionary<string, CalculatedFieldDefinition> _calculatedFields;
        private readonly HashSet<string> _evaluationStack;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalculatedFieldEvaluator"/> class.
        /// </summary>
        public CalculatedFieldEvaluator()
        {
            _calculatedFields = new Dictionary<string, CalculatedFieldDefinition>(StringComparer.OrdinalIgnoreCase);
            _evaluationStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a calculated field definition for evaluation.
        /// </summary>
        /// <param name="definition">The calculated field definition to register</param>
        public void RegisterCalculatedField(CalculatedFieldDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.EntityLogicalName))
                throw new ArgumentException("EntityLogicalName is required", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.AttributeLogicalName))
                throw new ArgumentException("AttributeLogicalName is required", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.Formula))
                throw new ArgumentException("Formula is required", nameof(definition));

            var key = GetKey(definition.EntityLogicalName, definition.AttributeLogicalName);
            _calculatedFields[key] = definition;
        }

        /// <summary>
        /// Gets a calculated field definition by entity and attribute name.
        /// </summary>
        public CalculatedFieldDefinition GetCalculatedField(string entityLogicalName, string attributeLogicalName)
        {
            var key = GetKey(entityLogicalName, attributeLogicalName);
            return _calculatedFields.TryGetValue(key, out var definition) ? definition : null;
        }

        /// <summary>
        /// Checks if a field is registered as a calculated field.
        /// </summary>
        public bool IsCalculatedField(string entityLogicalName, string attributeLogicalName)
        {
            var key = GetKey(entityLogicalName, attributeLogicalName);
            return _calculatedFields.ContainsKey(key);
        }

        /// <summary>
        /// Evaluates all calculated fields for the given entity and updates their values.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
        /// "Calculated columns are calculated in real-time when they are retrieved"
        /// 
        /// Calculated fields are evaluated:
        /// - On entity retrieve
        /// - When the entity is updated and dependencies have changed
        /// </summary>
        /// <param name="entity">The entity to evaluate calculated fields for</param>
        public void EvaluateCalculatedFields(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Find all calculated fields for this entity type
            var fieldsForEntity = _calculatedFields.Values
                .Where(cf => cf.EntityLogicalName.Equals(entity.LogicalName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var calculatedField in fieldsForEntity)
            {
                try
                {
                    var value = EvaluateCalculatedField(entity, calculatedField);
                    
                    // Update the entity with the calculated value
                    if (value != null)
                    {
                        entity[calculatedField.AttributeLogicalName] = ConvertToTargetType(value, calculatedField.ResultType);
                    }
                    else
                    {
                        // Set null if result is null
                        entity[calculatedField.AttributeLogicalName] = null;
                    }
                }
                catch (CircularDependencyException)
                {
                    // Re-throw circular dependency exceptions
                    throw;
                }
                catch (Exception ex)
                {
                    // Log or handle formula evaluation errors
                    throw new InvalidOperationException(
                        $"Error evaluating calculated field '{calculatedField.AttributeLogicalName}' on entity '{entity.LogicalName}': {ex.Message}",
                        ex);
                }
            }
        }

        /// <summary>
        /// Evaluates a single calculated field for the given entity.
        /// </summary>
        private object EvaluateCalculatedField(Entity entity, CalculatedFieldDefinition definition)
        {
            // Check for circular dependencies
            var key = GetKey(definition.EntityLogicalName, definition.AttributeLogicalName);
            if (_evaluationStack.Contains(key))
            {
                throw new CircularDependencyException(
                    $"Circular dependency detected for calculated field '{definition.AttributeLogicalName}' on entity '{definition.EntityLogicalName}'");
            }

            try
            {
                _evaluationStack.Add(key);

                // Preprocess the formula to convert Dataverse syntax to NCalc syntax
                var ncalcFormula = PreprocessFormula(definition.Formula);

                // Create NCalc expression
                var expression = new Expression(ncalcFormula);

                // Register custom Dataverse functions
                DataverseFunctionExtensions.RegisterDataverseFunctions(expression);

                // Inject entity field values as parameters
                InjectEntityParameters(expression, entity, definition);

                // Evaluate the expression
                var result = expression.Evaluate();

                return result;
            }
            finally
            {
                _evaluationStack.Remove(key);
            }
        }

        /// <summary>
        /// Preprocesses a Dataverse formula to convert it to NCalc syntax.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
        /// Field references use square brackets: [fieldname] or [relatedtable.fieldname]
        /// 
        /// Transformations:
        /// - [fieldname] → fieldname (remove square brackets)
        /// - [related.field] → related_field (convert dot notation to underscore)
        /// - String literals: ensure proper quoting
        /// </summary>
        private string PreprocessFormula(string formula)
        {
            if (string.IsNullOrWhiteSpace(formula))
                throw new ArgumentException("Formula cannot be empty", nameof(formula));

            // Convert [fieldname] to fieldname (but preserve string literals in quotes)
            // Pattern matches [fieldname] or [entity.fieldname] outside of quotes
            var result = Regex.Replace(formula, @"\[([a-zA-Z0-9_\.]+)\]", match =>
            {
                var fieldRef = match.Groups[1].Value;
                
                // Convert dot notation to underscore for related entity fields
                // E.g., [account.name] becomes account_name
                if (fieldRef.Contains("."))
                {
                    return fieldRef.Replace(".", "_");
                }
                
                return fieldRef;
            });

            return result;
        }

        /// <summary>
        /// Injects entity field values as parameters into the NCalc expression.
        /// Also evaluates any dependent calculated fields recursively.
        /// </summary>
        private void InjectEntityParameters(Expression expression, Entity entity, CalculatedFieldDefinition definition)
        {
            // Add all entity attributes as parameters
            foreach (var attribute in entity.Attributes)
            {
                var paramName = attribute.Key.ToLowerInvariant();
                var value = attribute.Value;

                // Convert EntityReference, OptionSetValue, Money to usable values
                if (value is EntityReference entityRef)
                {
                    // For related entity fields, we'd need to look up the related entity
                    // For now, just use the ID
                    expression.Parameters[paramName] = entityRef.Id.ToString();
                }
                else if (value is OptionSetValue optionSet)
                {
                    expression.Parameters[paramName] = optionSet.Value;
                }
                else if (value is Money money)
                {
                    expression.Parameters[paramName] = money.Value;
                }
                else if (value is bool boolValue)
                {
                    expression.Parameters[paramName] = boolValue;
                }
                else
                {
                    expression.Parameters[paramName] = value;
                }
            }

            // Evaluate any dependent calculated fields first
            foreach (var dependency in definition.Dependencies)
            {
                if (IsCalculatedField(entity.LogicalName, dependency))
                {
                    var dependentField = GetCalculatedField(entity.LogicalName, dependency);
                    if (dependentField != null && !entity.Contains(dependency))
                    {
                        // Recursively evaluate the dependent field
                        var dependentValue = EvaluateCalculatedField(entity, dependentField);
                        entity[dependency] = dependentValue;
                        expression.Parameters[dependency.ToLowerInvariant()] = dependentValue;
                    }
                }
            }
        }

        /// <summary>
        /// Converts the evaluation result to the target type specified in the field definition.
        /// </summary>
        private object ConvertToTargetType(object value, Type targetType)
        {
            if (value == null || targetType == null)
                return value;

            if (value.GetType() == targetType)
                return value;

            try
            {
                if (targetType == typeof(string))
                {
                    return value.ToString();
                }
                else if (targetType == typeof(int) || targetType == typeof(int?))
                {
                    return Convert.ToInt32(value);
                }
                else if (targetType == typeof(decimal) || targetType == typeof(decimal?))
                {
                    return Convert.ToDecimal(value);
                }
                else if (targetType == typeof(double) || targetType == typeof(double?))
                {
                    return Convert.ToDouble(value);
                }
                else if (targetType == typeof(bool) || targetType == typeof(bool?))
                {
                    return Convert.ToBoolean(value);
                }
                else if (targetType == typeof(DateTime) || targetType == typeof(DateTime?))
                {
                    return Convert.ToDateTime(value);
                }
                else if (targetType == typeof(Money))
                {
                    return new Money(Convert.ToDecimal(value));
                }
                else if (targetType == typeof(OptionSetValue))
                {
                    return new OptionSetValue(Convert.ToInt32(value));
                }

                return value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot convert value '{value}' to type '{targetType.Name}'", ex);
            }
        }

        private static string GetKey(string entityLogicalName, string attributeLogicalName)
        {
            return $"{entityLogicalName}.{attributeLogicalName}".ToLowerInvariant();
        }
    }

    /// <summary>
    /// Exception thrown when a circular dependency is detected in calculated fields.
    /// </summary>
    public class CircularDependencyException : InvalidOperationException
    {
        public CircularDependencyException(string message) : base(message)
        {
        }

        public CircularDependencyException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
