using Fake4Dataverse.Abstractions;
using Fake4Dataverse.CalculatedFields;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse
{
    /// <summary>
    /// Extends XrmFakedContext with calculated field support.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
    /// "Define calculated columns - Create columns that automatically calculate their values based on other column values"
    /// </summary>
    public partial class XrmFakedContext : IXrmFakedContext
    {
        /// <summary>
        /// Gets the calculated field evaluator for this context.
        /// </summary>
        public CalculatedFieldEvaluator CalculatedFieldEvaluator
        {
            get
            {
                if (!HasProperty<CalculatedFieldEvaluator>())
                {
                    SetProperty(new CalculatedFieldEvaluator());
                }
                return GetProperty<CalculatedFieldEvaluator>();
            }
        }

        /// <summary>
        /// Evaluates calculated fields for an entity.
        /// This is called automatically during entity retrieve and update operations.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-calculated-fields
        /// "Calculated columns are calculated in real-time when they are retrieved"
        /// 
        /// Calculated fields are evaluated:
        /// - When the entity is retrieved from the database
        /// - When the entity is updated and dependencies have changed
        /// </summary>
        /// <param name="entity">The entity to evaluate calculated fields for</param>
        internal void EvaluateCalculatedFieldsForEntity(Entity entity)
        {
            if (entity == null)
                return;

            try
            {
                CalculatedFieldEvaluator.EvaluateCalculatedFields(entity);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the operation
                // In real Dataverse, calculated field errors don't prevent retrieval
                System.Diagnostics.Debug.WriteLine($"Error evaluating calculated fields: {ex.Message}");
            }
        }
    }
}
