using System;
using System.Collections.Generic;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Interface for simulating Cloud Flows (Power Automate flows) in tests.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/overview-cloud
    /// 
    /// Cloud Flows enable automation of business processes triggered by Dataverse events
    /// and integration with external systems. This simulator allows testing of:
    /// - Flow trigger conditions
    /// - Dataverse connector actions
    /// - Custom connector actions (via extensibility)
    /// - Flow execution results and verification
    /// </summary>
    public interface ICloudFlowSimulator
    {
        /// <summary>
        /// Registers a Cloud Flow definition to be simulated.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/getting-started
        /// 
        /// Registered flows will automatically trigger when matching Dataverse operations occur
        /// in the fake context (Create, Update, Delete, etc.).
        /// </summary>
        /// <param name="flowDefinition">The flow definition containing trigger and actions</param>
        void RegisterFlow(ICloudFlowDefinition flowDefinition);

        /// <summary>
        /// Registers a Cloud Flow from an exported JSON definition.
        /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
        /// 
        /// Allows importing real Cloud Flow definitions exported from Power Automate,
        /// enabling realistic simulation that matches production behavior.
        /// </summary>
        /// <param name="flowJson">JSON representation of the Cloud Flow definition</param>
        void RegisterFlowFromJson(string flowJson);

        /// <summary>
        /// Registers multiple Cloud Flows at once
        /// </summary>
        /// <param name="flowDefinitions">Collection of flow definitions to register</param>
        void RegisterFlows(IEnumerable<ICloudFlowDefinition> flowDefinitions);

        /// <summary>
        /// Unregisters a previously registered Cloud Flow
        /// </summary>
        /// <param name="flowName">The unique name of the flow to unregister</param>
        void UnregisterFlow(string flowName);

        /// <summary>
        /// Clears all registered Cloud Flows
        /// </summary>
        void ClearAllFlows();

        /// <summary>
        /// Manually triggers a Cloud Flow with specific inputs.
        /// Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
        /// 
        /// Useful for testing flow logic with controlled inputs without triggering
        /// an actual Dataverse operation.
        /// </summary>
        /// <param name="flowName">The unique name of the flow to trigger</param>
        /// <param name="triggerInputs">The inputs to provide to the flow trigger</param>
        /// <returns>Execution result containing outputs and status</returns>
        IFlowExecutionResult SimulateTrigger(string flowName, Dictionary<string, object> triggerInputs);

        /// <summary>
        /// Registers a connector action handler for non-Dataverse connectors.
        /// Reference: https://learn.microsoft.com/en-us/connectors/
        /// 
        /// Allows test writers to provide custom logic for handling connector actions
        /// (Office 365, SharePoint, Teams, custom APIs, etc.) in flows.
        /// </summary>
        /// <param name="connectorType">The connector type identifier (e.g., "Office365", "SharePoint")</param>
        /// <param name="handler">The handler implementation for this connector type</param>
        void RegisterConnectorActionHandler(string connectorType, IConnectorActionHandler handler);

        /// <summary>
        /// Gets the registered connector action handler for a specific connector type
        /// </summary>
        /// <param name="connectorType">The connector type identifier</param>
        /// <returns>The registered handler, or null if not registered</returns>
        IConnectorActionHandler GetConnectorHandler(string connectorType);

        /// <summary>
        /// Asserts that a flow was triggered at least once
        /// </summary>
        /// <param name="flowName">The unique name of the flow</param>
        /// <exception cref="InvalidOperationException">Thrown if the flow was not triggered</exception>
        void AssertFlowTriggered(string flowName);

        /// <summary>
        /// Asserts that a flow was NOT triggered
        /// </summary>
        /// <param name="flowName">The unique name of the flow</param>
        /// <exception cref="InvalidOperationException">Thrown if the flow was triggered</exception>
        void AssertFlowNotTriggered(string flowName);

        /// <summary>
        /// Gets the number of times a flow was executed
        /// </summary>
        /// <param name="flowName">The unique name of the flow</param>
        /// <returns>Execution count</returns>
        int GetFlowExecutionCount(string flowName);

        /// <summary>
        /// Gets all execution results for a specific flow
        /// </summary>
        /// <param name="flowName">The unique name of the flow</param>
        /// <returns>List of execution results</returns>
        IReadOnlyList<IFlowExecutionResult> GetFlowExecutionResults(string flowName);

        /// <summary>
        /// Gets all registered flow names
        /// </summary>
        /// <returns>Collection of registered flow names</returns>
        IReadOnlyList<string> GetRegisteredFlowNames();

        /// <summary>
        /// Clears execution history for all flows
        /// </summary>
        void ClearExecutionHistory();
    }
}
