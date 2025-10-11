using System;
using System.Collections.Generic;

namespace Fake4Dataverse.CalculatedFields
{
    /// <summary>
    /// Represents the definition of a calculated field in Dataverse.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
    /// "Define calculated columns - Create columns that automatically calculate their values based on other column values"
    /// 
    /// Calculated fields (columns) are fields whose values are automatically computed based on a formula.
    /// The formula can reference other fields in the same table or related tables.
    /// </summary>
    public class CalculatedFieldDefinition
    {
        /// <summary>
        /// Gets or sets the logical name of the entity containing the calculated field.
        /// </summary>
        public string EntityLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the calculated field attribute.
        /// </summary>
        public string AttributeLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the formula used to calculate the field value.
        /// 
        /// The formula uses Dataverse calculated field syntax with field references in square brackets.
        /// Example: "[quantity] * [unit_price]" or "CONCAT([firstname], ' ', [lastname])"
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields#functions-syntax
        /// Supported functions include: CONCAT, DIFFINDAYS, DIFFINHOURS, DIFFINMINUTES, DIFFINMONTHS, DIFFINWEEKS, DIFFINYEARS,
        /// ADDHOURS, ADDDAYS, ADDWEEKS, ADDMONTHS, ADDYEARS, SUBTRACTHOURS, SUBTRACTDAYS, SUBTRACTWEEKS, SUBTRACTMONTHS, SUBTRACTYEARS,
        /// TRIMLEFT, TRIMRIGHT, and logical operators AND/OR
        /// </summary>
        public string Formula { get; set; }

        /// <summary>
        /// Gets or sets the data type of the calculated field result.
        /// Common types: String, Integer, Decimal, DateTime, Boolean, Money
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/types-of-fields
        /// Field types documentation describes available column types in Dataverse
        /// </summary>
        public Type ResultType { get; set; }

        /// <summary>
        /// Gets or sets the list of field names that this calculated field depends on.
        /// Used to detect circular dependencies and determine recalculation triggers.
        /// </summary>
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalculatedFieldDefinition"/> class.
        /// </summary>
        public CalculatedFieldDefinition()
        {
            Dependencies = new List<string>();
        }
    }
}
