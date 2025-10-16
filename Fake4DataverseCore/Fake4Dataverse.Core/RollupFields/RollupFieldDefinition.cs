using System;
using System.Collections.Generic;

namespace Fake4Dataverse.RollupFields
{
    /// <summary>
    /// Represents the definition of a rollup field in Dataverse.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "Define rollup columns to aggregate values - Create columns that automatically calculate values by aggregating 
    /// values from related child records. These calculated values can be viewed by users and used in reports."
    /// 
    /// Rollup fields aggregate data from related child records using functions like SUM, COUNT, MIN, MAX, and AVG.
    /// They are evaluated asynchronously in Dataverse but can be triggered on-demand for testing.
    /// </summary>
    public class RollupFieldDefinition
    {
        /// <summary>
        /// Gets or sets the logical name of the entity containing the rollup field.
        /// </summary>
        public string EntityLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the rollup field attribute.
        /// </summary>
        public string AttributeLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the name of the relationship to traverse to find related records.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "You select the relationship that defines the related records to aggregate"
        /// 
        /// Example: "contact_customer_accounts" for contacts related to an account
        /// </summary>
        public string RelationshipName { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the related entity containing the records to aggregate.
        /// 
        /// Example: "contact" when aggregating contact records for an account
        /// </summary>
        public string RelatedEntityLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the attribute in the related entity to aggregate.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "Select the attribute in the related table that you want to aggregate"
        /// 
        /// Example: "annualincome" to sum income across contacts
        /// </summary>
        public string AggregateAttributeLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the aggregate function to use.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// Supported functions:
        /// - SUM: Totals the values of the attribute in the related records
        /// - COUNT: Counts all related records
        /// - MIN: Returns the minimum value
        /// - MAX: Returns the maximum value
        /// - AVG: Calculates the average value
        /// </summary>
        public RollupAggregateFunction AggregateFunction { get; set; }

        /// <summary>
        /// Gets or sets the data type of the rollup field result.
        /// Typically decimal for SUM/AVG/MIN/MAX, integer for COUNT.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields
        /// Field types documentation describes available column types in Dataverse
        /// </summary>
        public Type ResultType { get; set; }

        /// <summary>
        /// Gets or sets an optional filter to apply to related records before aggregation.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "You can optionally specify filters to restrict which records are included"
        /// 
        /// The filter is a predicate function that takes an entity and returns true if it should be included.
        /// Example: entity => entity.GetAttributeValue<OptionSetValue>("statecode")?.Value == 0 (active only)
        /// </summary>
        public Func<Microsoft.Xrm.Sdk.Entity, bool> Filter { get; set; }

        /// <summary>
        /// Gets or sets the state filter to apply (active records, inactive records, or both).
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "Use filters to specify whether to include only active records, only inactive records, or all records"
        /// </summary>
        public RollupStateFilter StateFilter { get; set; }

        /// <summary>
        /// Gets or sets whether this is a hierarchical rollup.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "For tables that have a hierarchical relationship, you can aggregate data from all child records in the hierarchy"
        /// 
        /// When true, aggregates values from the entire hierarchy (self-referencing relationships).
        /// Example: Account rollup that includes all child accounts
        /// </summary>
        public bool IsHierarchical { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RollupFieldDefinition"/> class.
        /// </summary>
        public RollupFieldDefinition()
        {
            StateFilter = RollupStateFilter.Active;
            IsHierarchical = false;
        }
    }

    /// <summary>
    /// Defines the aggregate functions available for rollup fields.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "Available aggregate functions include SUM, COUNT, MIN, MAX, and AVG"
    /// </summary>
    public enum RollupAggregateFunction
    {
        /// <summary>
        /// Totals the values of the attribute in the related records.
        /// Applicable to: Integer, Decimal, Currency (Money)
        /// </summary>
        Sum,

        /// <summary>
        /// Counts all related records (includes records with null values).
        /// Returns: Integer
        /// </summary>
        Count,

        /// <summary>
        /// Returns the minimum value of the attribute in the related records.
        /// Applicable to: Integer, Decimal, Currency (Money), Date/Time
        /// </summary>
        Min,

        /// <summary>
        /// Returns the maximum value of the attribute in the related records.
        /// Applicable to: Integer, Decimal, Currency (Money), Date/Time
        /// </summary>
        Max,

        /// <summary>
        /// Calculates the average value of the attribute in the related records.
        /// Applicable to: Integer, Decimal, Currency (Money)
        /// Returns: Decimal
        /// </summary>
        Avg
    }

    /// <summary>
    /// Defines which records to include based on state.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "Use filters to specify whether to include only active records, only inactive records, or all records"
    /// </summary>
    public enum RollupStateFilter
    {
        /// <summary>
        /// Include only active records (statecode = 0).
        /// This is the default behavior in Dataverse.
        /// </summary>
        Active,

        /// <summary>
        /// Include only inactive records (statecode = 1).
        /// </summary>
        Inactive,

        /// <summary>
        /// Include all records regardless of state.
        /// </summary>
        All
    }
}
