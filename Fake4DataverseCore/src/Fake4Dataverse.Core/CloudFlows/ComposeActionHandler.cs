using System;
using System.Collections.Generic;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.CloudFlows;
using Fake4Dataverse.CloudFlows.Expressions;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Built-in handler for Compose actions.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/data-operations#use-the-compose-action
    /// 
    /// Compose actions evaluate expressions and return the result as their output.
    /// This allows for data transformation and composition within flows.
    /// </summary>
    public class ComposeActionHandler : IConnectorActionHandler
    {
        public string ConnectorType => "Compose";

        public bool CanHandle(IFlowAction action)
        {
            return action != null && action.ActionType == "Compose";
        }

        public IDictionary<string, object> Execute(
            IFlowAction action, 
            IXrmFakedContext context, 
            IFlowExecutionContext flowContext)
        {
            if (!(action is ComposeAction composeAction))
            {
                throw new ArgumentException("Action must be of type ComposeAction", nameof(action));
            }

            // Create expression evaluator for this flow execution context
            var expressionEvaluator = new ExpressionEvaluator(flowContext);

            // Evaluate the inputs recursively
            var result = EvaluateInputsRecursively(composeAction.Inputs, expressionEvaluator);

            // Return the composed value as the output
            return new Dictionary<string, object>
            {
                ["value"] = result
            };
        }

        /// <summary>
        /// Recursively evaluates expressions in inputs (handles nested dictionaries and arrays)
        /// </summary>
        private object EvaluateInputsRecursively(object input, ExpressionEvaluator evaluator)
        {
            if (input == null)
            {
                return null;
            }

            // Evaluate dictionaries recursively
            if (input is IDictionary<string, object> dict)
            {
                var result = new Dictionary<string, object>();
                foreach (var kvp in dict)
                {
                    result[kvp.Key] = EvaluateInputsRecursively(kvp.Value, evaluator);
                }
                return result;
            }

            // Evaluate arrays recursively
            if (input is System.Collections.IList list)
            {
                var result = new List<object>();
                foreach (var item in list)
                {
                    result.Add(EvaluateInputsRecursively(item, evaluator));
                }
                return result;
            }

            // Evaluate the value
            return evaluator.Evaluate(input);
        }
    }
}
