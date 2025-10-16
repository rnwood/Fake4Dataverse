using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Interface for handling connector actions in Cloud Flows.
    /// Reference: https://learn.microsoft.com/en-us/connectors/
    /// 
    /// Connector action handlers provide the logic for executing actions
    /// from specific connectors (Office 365, SharePoint, Teams, custom APIs, etc.).
    /// Test writers can implement this interface to mock connector behavior.
    /// </summary>
    public interface IConnectorActionHandler
    {
        /// <summary>
        /// Gets the connector type this handler supports (e.g., "Office365", "SharePoint").
        /// This should match the ConnectorType used in flow action definitions.
        /// </summary>
        string ConnectorType { get; }

        /// <summary>
        /// Determines whether this handler can execute the specified action
        /// </summary>
        /// <param name="action">The action to check</param>
        /// <returns>True if this handler can execute the action</returns>
        bool CanHandle(IFlowAction action);

        /// <summary>
        /// Executes a connector action and returns the result.
        /// Reference: https://learn.microsoft.com/en-us/connectors/connector-reference/connector-reference-standard-connectors
        /// 
        /// This method should:
        /// - Process the action parameters
        /// - Perform the required operation (or mock it in tests)
        /// - Return the action outputs
        /// - Throw an exception if the action fails
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="context">The fake context for accessing Dataverse data if needed</param>
        /// <param name="flowContext">Context containing trigger inputs and previous action outputs</param>
        /// <returns>Dictionary of output values from the action</returns>
        IDictionary<string, object> Execute(IFlowAction action, IXrmFakedContext context, 
            IFlowExecutionContext flowContext);
    }
}
