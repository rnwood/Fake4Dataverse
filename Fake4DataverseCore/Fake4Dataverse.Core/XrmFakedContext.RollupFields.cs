using Fake4Dataverse.Abstractions;
using Fake4Dataverse.RollupFields;
using Microsoft.Xrm.Sdk;
using System;

namespace Fake4Dataverse
{
    /// <summary>
    /// Extends XrmFakedContext with rollup field support.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
    /// "Define rollup columns to aggregate values - Create columns that automatically calculate values by aggregating 
    /// values from related child records."
    /// </summary>
    public partial class XrmFakedContext : IXrmFakedContext
    {
        /// <summary>
        /// Gets the rollup field evaluator for this context.
        /// </summary>
        public RollupFieldEvaluator RollupFieldEvaluator
        {
            get
            {
                if (!HasProperty<RollupFieldEvaluator>())
                {
                    SetProperty(new RollupFieldEvaluator(this));
                }
                return GetProperty<RollupFieldEvaluator>();
            }
        }

        /// <summary>
        /// Evaluates rollup fields for an entity when related records change.
        /// This is called automatically during entity create/update/delete operations that affect relationships.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "Rollup columns are calculated asynchronously by scheduled system jobs that run in the background"
        /// 
        /// In Fake4Dataverse, rollup fields can be evaluated on-demand or automatically when related records change.
        /// </summary>
        /// <param name="entity">The entity to evaluate rollup fields for</param>
        internal void EvaluateRollupFieldsForEntity(Entity entity)
        {
            if (entity == null)
                return;

            try
            {
                RollupFieldEvaluator.EvaluateRollupFields(entity);
            }
            catch (Exception ex)
            {
                // Log the error but don't fail the operation
                // In real Dataverse, rollup field errors don't prevent operations
                System.Diagnostics.Debug.WriteLine($"Error evaluating rollup fields: {ex.Message}");
            }
        }

        /// <summary>
        /// Triggers rollup field recalculation for entities that may be affected by a related record change.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/maker/data-platform/define-rollup-fields
        /// "When you create, update, or delete a record, the rollup columns on related records are recalculated"
        /// 
        /// This method finds all entities that have rollup fields referencing the changed entity
        /// and triggers their recalculation.
        /// </summary>
        /// <param name="changedEntity">The entity that was created/updated/deleted</param>
        internal void TriggerRollupRecalculationForRelatedEntities(Entity changedEntity)
        {
            if (changedEntity == null)
                return;

            // Find all rollup fields that reference this entity's type as the related entity
            // and trigger recalculation for the parent entities
            
            // For each lookup field in the changed entity, find the parent record and recalculate
            foreach (var attribute in changedEntity.Attributes)
            {
                if (attribute.Value is EntityReference entityRef && entityRef.Id != Guid.Empty)
                {
                    try
                    {
                        // Check if the parent entity has any rollup fields
                        RollupFieldEvaluator.TriggerRollupCalculation(entityRef.LogicalName, entityRef.Id);
                    }
                    catch (Exception ex)
                    {
                        // Log but continue processing other relationships
                        System.Diagnostics.Debug.WriteLine($"Error triggering rollup calculation: {ex.Message}");
                    }
                }
            }
        }
    }
}
