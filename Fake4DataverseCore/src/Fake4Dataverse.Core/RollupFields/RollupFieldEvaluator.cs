using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Fake4Dataverse.RollupFields
{
    /// <summary>
    /// Evaluates rollup field values by aggregating data from related child records.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "Define rollup columns to aggregate values - Create columns that automatically calculate values by aggregating 
    /// values from related child records."
    /// 
    /// This class handles:
    /// - Traversing relationships to find related records
    /// - Applying filters and state filters
    /// - Executing aggregate functions (SUM, COUNT, MIN, MAX, AVG)
    /// - Handling hierarchical rollups
    /// - Type conversion and error handling
    /// </summary>
    public class RollupFieldEvaluator
    {
        private readonly Dictionary<string, RollupFieldDefinition> _rollupFields;
        private readonly XrmFakedContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollupFieldEvaluator"/> class.
        /// </summary>
        /// <param name="context">The faked context to use for querying related records</param>
        public RollupFieldEvaluator(XrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _rollupFields = new Dictionary<string, RollupFieldDefinition>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Registers a rollup field definition for evaluation.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "You define a rollup column by specifying the source column, the relationship, and the aggregate function"
        /// </summary>
        /// <param name="definition">The rollup field definition to register</param>
        public void RegisterRollupField(RollupFieldDefinition definition)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.EntityLogicalName))
                throw new ArgumentException("EntityLogicalName is required", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.AttributeLogicalName))
                throw new ArgumentException("AttributeLogicalName is required", nameof(definition));

            if (string.IsNullOrWhiteSpace(definition.RelatedEntityLogicalName))
                throw new ArgumentException("RelatedEntityLogicalName is required", nameof(definition));

            // AggregateAttributeLogicalName is optional for COUNT function
            if (definition.AggregateFunction != RollupAggregateFunction.Count &&
                string.IsNullOrWhiteSpace(definition.AggregateAttributeLogicalName))
            {
                throw new ArgumentException("AggregateAttributeLogicalName is required for non-COUNT aggregate functions", nameof(definition));
            }

            var key = GetKey(definition.EntityLogicalName, definition.AttributeLogicalName);
            _rollupFields[key] = definition;
        }

        /// <summary>
        /// Gets a rollup field definition by entity and attribute name.
        /// </summary>
        public RollupFieldDefinition GetRollupField(string entityLogicalName, string attributeLogicalName)
        {
            var key = GetKey(entityLogicalName, attributeLogicalName);
            return _rollupFields.TryGetValue(key, out var definition) ? definition : null;
        }

        /// <summary>
        /// Checks if a field is registered as a rollup field.
        /// </summary>
        public bool IsRollupField(string entityLogicalName, string attributeLogicalName)
        {
            var key = GetKey(entityLogicalName, attributeLogicalName);
            return _rollupFields.ContainsKey(key);
        }

        /// <summary>
        /// Evaluates all rollup fields for the given entity and updates their values.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "Rollup columns are calculated asynchronously by scheduled system jobs"
        /// 
        /// In Fake4Dataverse, rollup fields can be triggered on-demand for testing.
        /// </summary>
        /// <param name="entity">The entity to evaluate rollup fields for</param>
        public void EvaluateRollupFields(Entity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            // Find all rollup fields for this entity type
            var fieldsForEntity = _rollupFields.Values
                .Where(rf => rf.EntityLogicalName.Equals(entity.LogicalName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var rollupField in fieldsForEntity)
            {
                try
                {
                    var value = EvaluateRollupField(entity, rollupField);
                    
                    // Update the entity with the rollup value
                    if (value != null)
                    {
                        entity[rollupField.AttributeLogicalName] = ConvertToTargetType(value, rollupField.ResultType);
                    }
                    else
                    {
                        // Set null if no related records or result is null
                        entity[rollupField.AttributeLogicalName] = null;
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Error evaluating rollup field '{rollupField.AttributeLogicalName}' on entity '{entity.LogicalName}': {ex.Message}",
                        ex);
                }
            }
        }

        /// <summary>
        /// Triggers rollup calculation for a specific entity record.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "You can manually trigger an immediate calculation of rollup columns using the CalculateRollupField message"
        /// 
        /// This is the equivalent of the CalculateRollupField message in Dataverse.
        /// </summary>
        /// <param name="entityLogicalName">The logical name of the entity</param>
        /// <param name="recordId">The ID of the record to calculate rollup fields for</param>
        public void TriggerRollupCalculation(string entityLogicalName, Guid recordId)
        {
            if (string.IsNullOrWhiteSpace(entityLogicalName))
                throw new ArgumentException("entityLogicalName is required", nameof(entityLogicalName));

            if (recordId == Guid.Empty)
                throw new ArgumentException("recordId cannot be empty", nameof(recordId));

            // Retrieve the entity from context
            var entity = _context.Data.ContainsKey(entityLogicalName) &&
                        _context.Data[entityLogicalName].ContainsKey(recordId)
                ? _context.Data[entityLogicalName][recordId]
                : null;

            if (entity == null)
                throw new InvalidOperationException($"Entity '{entityLogicalName}' with ID '{recordId}' not found");

            // Evaluate all rollup fields for this entity
            EvaluateRollupFields(entity);
        }

        /// <summary>
        /// Evaluates a single rollup field for the given entity.
        /// </summary>
        private object EvaluateRollupField(Entity entity, RollupFieldDefinition definition)
        {
            // Get related records
            var relatedRecords = GetRelatedRecords(entity, definition);

            // Apply filters
            relatedRecords = ApplyFilters(relatedRecords, definition);

            // Execute aggregate function
            return ExecuteAggregateFunction(relatedRecords, definition);
        }

        /// <summary>
        /// Gets related records based on the relationship definition.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
        /// "Navigate collection-valued navigation properties"
        /// </summary>
        private List<Entity> GetRelatedRecords(Entity entity, RollupFieldDefinition definition)
        {
            var relatedRecords = new List<Entity>();

            if (!_context.Data.ContainsKey(definition.RelatedEntityLogicalName))
                return relatedRecords;

            // Find all related records
            // We need to find records where the lookup field points to our entity
            var allRelatedEntities = _context.Data[definition.RelatedEntityLogicalName].Values;

            foreach (var relatedEntity in allRelatedEntities)
            {
                // Check if this entity has a relationship to our parent entity
                if (IsRelatedTo(relatedEntity, entity, definition))
                {
                    relatedRecords.Add(relatedEntity);
                }
            }

            // Handle hierarchical rollups if needed
            if (definition.IsHierarchical)
            {
                relatedRecords.AddRange(GetHierarchicalRecords(entity, definition));
            }

            return relatedRecords;
        }

        /// <summary>
        /// Checks if a related entity is related to the parent entity.
        /// </summary>
        private bool IsRelatedTo(Entity relatedEntity, Entity parentEntity, RollupFieldDefinition definition)
        {
            // Look for a lookup field that references the parent entity
            foreach (var attribute in relatedEntity.Attributes)
            {
                if (attribute.Value is EntityReference entityRef)
                {
                    if (entityRef.LogicalName.Equals(parentEntity.LogicalName, StringComparison.OrdinalIgnoreCase) &&
                        entityRef.Id == parentEntity.Id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Gets records in a hierarchical relationship.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "For tables that have a hierarchical relationship, you can aggregate data from all child records in the hierarchy"
        /// </summary>
        private List<Entity> GetHierarchicalRecords(Entity entity, RollupFieldDefinition definition)
        {
            var hierarchicalRecords = new List<Entity>();

            // This is a simplified implementation
            // In a real scenario, we'd need to traverse the hierarchy recursively
            // For now, this is a placeholder for future enhancement

            return hierarchicalRecords;
        }

        /// <summary>
        /// Applies filters to the related records.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "You can optionally specify filters to restrict which records are included"
        /// </summary>
        private List<Entity> ApplyFilters(List<Entity> records, RollupFieldDefinition definition)
        {
            var filteredRecords = records;

            // Apply state filter
            if (definition.StateFilter != RollupStateFilter.All)
            {
                filteredRecords = filteredRecords.Where(r =>
                {
                    var stateCode = r.GetAttributeValue<OptionSetValue>("statecode");
                    if (stateCode == null)
                        return definition.StateFilter == RollupStateFilter.Active; // Default to active

                    return definition.StateFilter == RollupStateFilter.Active
                        ? stateCode.Value == 0
                        : stateCode.Value == 1;
                }).ToList();
            }

            // Apply custom filter if provided
            if (definition.Filter != null)
            {
                filteredRecords = filteredRecords.Where(definition.Filter).ToList();
            }

            return filteredRecords;
        }

        /// <summary>
        /// Executes the aggregate function on the filtered records.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "Available aggregate functions include SUM, COUNT, MIN, MAX, and AVG"
        /// </summary>
        private object ExecuteAggregateFunction(List<Entity> records, RollupFieldDefinition definition)
        {
            switch (definition.AggregateFunction)
            {
                case RollupAggregateFunction.Count:
                    return ExecuteCount(records);

                case RollupAggregateFunction.Sum:
                    return ExecuteSum(records, definition.AggregateAttributeLogicalName);

                case RollupAggregateFunction.Min:
                    return ExecuteMin(records, definition.AggregateAttributeLogicalName);

                case RollupAggregateFunction.Max:
                    return ExecuteMax(records, definition.AggregateAttributeLogicalName);

                case RollupAggregateFunction.Avg:
                    return ExecuteAvg(records, definition.AggregateAttributeLogicalName);

                default:
                    throw new NotSupportedException($"Aggregate function '{definition.AggregateFunction}' is not supported");
            }
        }

        /// <summary>
        /// Counts all related records.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "COUNT: Counts all related records"
        /// </summary>
        private int ExecuteCount(List<Entity> records)
        {
            return records.Count;
        }

        /// <summary>
        /// Sums the values of the attribute in the related records.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "SUM: Totals the values of the attribute in the related records"
        /// </summary>
        private object ExecuteSum(List<Entity> records, string attributeName)
        {
            var values = GetAttributeValues(records, attributeName);

            if (values.Count == 0)
                return null;

            var firstValue = values.First();

            if (firstValue is int || firstValue is int?)
            {
                return values.Cast<int?>().Where(v => v.HasValue).Sum(v => v.Value);
            }
            else if (firstValue is decimal || firstValue is decimal?)
            {
                return values.Cast<decimal?>().Where(v => v.HasValue).Sum(v => v.Value);
            }
            else if (firstValue is Money)
            {
                return new Money(values.Cast<Money>().Where(v => v != null).Sum(v => v.Value));
            }
            else if (firstValue is double || firstValue is double?)
            {
                return values.Cast<double?>().Where(v => v.HasValue).Sum(v => v.Value);
            }

            throw new InvalidOperationException($"SUM operation not supported for type '{firstValue?.GetType().Name}'");
        }

        /// <summary>
        /// Returns the minimum value of the attribute in the related records.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "MIN: Returns the minimum value"
        /// </summary>
        private object ExecuteMin(List<Entity> records, string attributeName)
        {
            var values = GetAttributeValues(records, attributeName);

            if (values.Count == 0)
                return null;

            var firstValue = values.First();

            if (firstValue is int || firstValue is int?)
            {
                return values.Cast<int?>().Where(v => v.HasValue).Min(v => v.Value);
            }
            else if (firstValue is decimal || firstValue is decimal?)
            {
                return values.Cast<decimal?>().Where(v => v.HasValue).Min(v => v.Value);
            }
            else if (firstValue is Money)
            {
                return new Money(values.Cast<Money>().Where(v => v != null).Min(v => v.Value));
            }
            else if (firstValue is DateTime || firstValue is DateTime?)
            {
                return values.Cast<DateTime?>().Where(v => v.HasValue).Min(v => v.Value);
            }
            else if (firstValue is double || firstValue is double?)
            {
                return values.Cast<double?>().Where(v => v.HasValue).Min(v => v.Value);
            }

            throw new InvalidOperationException($"MIN operation not supported for type '{firstValue?.GetType().Name}'");
        }

        /// <summary>
        /// Returns the maximum value of the attribute in the related records.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "MAX: Returns the maximum value"
        /// </summary>
        private object ExecuteMax(List<Entity> records, string attributeName)
        {
            var values = GetAttributeValues(records, attributeName);

            if (values.Count == 0)
                return null;

            var firstValue = values.First();

            if (firstValue is int || firstValue is int?)
            {
                return values.Cast<int?>().Where(v => v.HasValue).Max(v => v.Value);
            }
            else if (firstValue is decimal || firstValue is decimal?)
            {
                return values.Cast<decimal?>().Where(v => v.HasValue).Max(v => v.Value);
            }
            else if (firstValue is Money)
            {
                return new Money(values.Cast<Money>().Where(v => v != null).Max(v => v.Value));
            }
            else if (firstValue is DateTime || firstValue is DateTime?)
            {
                return values.Cast<DateTime?>().Where(v => v.HasValue).Max(v => v.Value);
            }
            else if (firstValue is double || firstValue is double?)
            {
                return values.Cast<double?>().Where(v => v.HasValue).Max(v => v.Value);
            }

            throw new InvalidOperationException($"MAX operation not supported for type '{firstValue?.GetType().Name}'");
        }

        /// <summary>
        /// Calculates the average value of the attribute in the related records.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "AVG: Calculates the average value"
        /// </summary>
        private object ExecuteAvg(List<Entity> records, string attributeName)
        {
            var values = GetAttributeValues(records, attributeName);

            if (values.Count == 0)
                return null;

            var firstValue = values.First();

            if (firstValue is int || firstValue is int?)
            {
                return values.Cast<int?>().Where(v => v.HasValue).Average(v => v.Value);
            }
            else if (firstValue is decimal || firstValue is decimal?)
            {
                return values.Cast<decimal?>().Where(v => v.HasValue).Average(v => v.Value);
            }
            else if (firstValue is Money)
            {
                return new Money(values.Cast<Money>().Where(v => v != null).Average(v => v.Value));
            }
            else if (firstValue is double || firstValue is double?)
            {
                return values.Cast<double?>().Where(v => v.HasValue).Average(v => v.Value);
            }

            throw new InvalidOperationException($"AVG operation not supported for type '{firstValue?.GetType().Name}'");
        }

        /// <summary>
        /// Gets all non-null values for an attribute from a list of records.
        /// </summary>
        private List<object> GetAttributeValues(List<Entity> records, string attributeName)
        {
            return records
                .Where(r => r.Contains(attributeName) && r[attributeName] != null)
                .Select(r => r[attributeName])
                .ToList();
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
                if (targetType == typeof(int) || targetType == typeof(int?))
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
                else if (targetType == typeof(Money))
                {
                    if (value is Money)
                        return value;
                    return new Money(Convert.ToDecimal(value));
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
}
