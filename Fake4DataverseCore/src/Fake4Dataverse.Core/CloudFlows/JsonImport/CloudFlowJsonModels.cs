using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fake4Dataverse.CloudFlows.JsonImport
{
    /// <summary>
    /// Root model for an exported Cloud Flow JSON definition.
    /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language
    /// 
    /// Cloud Flows are exported with a workflow definition that follows the Logic Apps schema.
    /// This model represents the structure of the exported JSON.
    /// </summary>
    internal class CloudFlowJsonRoot
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("properties")]
        public CloudFlowProperties Properties { get; set; }
    }

    /// <summary>
    /// Properties section of the exported Cloud Flow JSON.
    /// Contains the display name, state, and the workflow definition.
    /// </summary>
    internal class CloudFlowProperties
    {
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }

        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("definition")]
        public WorkflowDefinition Definition { get; set; }
    }

    /// <summary>
    /// Workflow definition containing the schema, triggers, and actions.
    /// Reference: https://learn.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language-schema-reference
    /// </summary>
    internal class WorkflowDefinition
    {
        [JsonPropertyName("$schema")]
        public string Schema { get; set; }

        [JsonPropertyName("contentVersion")]
        public string ContentVersion { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; }

        [JsonPropertyName("triggers")]
        public Dictionary<string, TriggerDefinition> Triggers { get; set; }

        [JsonPropertyName("actions")]
        public Dictionary<string, ActionDefinition> Actions { get; set; }
    }

    /// <summary>
    /// Trigger definition in the workflow.
    /// Reference: https://learn.microsoft.com/en-us/power-automate/triggers-introduction
    /// </summary>
    internal class TriggerDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inputs")]
        public TriggerInputs Inputs { get; set; }
    }

    /// <summary>
    /// Inputs for a trigger, typically containing connection info and parameters.
    /// For Dataverse triggers, this includes the entity name, message type, and scope.
    /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
    /// </summary>
    internal class TriggerInputs
    {
        [JsonPropertyName("host")]
        public HostDefinition Host { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    /// <summary>
    /// Action definition in the workflow.
    /// Actions execute sequentially or in parallel depending on runAfter dependencies.
    /// </summary>
    internal class ActionDefinition
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inputs")]
        public ActionInputs Inputs { get; set; }

        [JsonPropertyName("runAfter")]
        public Dictionary<string, List<string>> RunAfter { get; set; }
    }

    /// <summary>
    /// Inputs for an action, containing connection info and operation parameters.
    /// For Dataverse actions, this includes the operation ID (CreateRecord, UpdateRecord, etc.)
    /// and the parameters (entity name, attributes, etc.).
    /// </summary>
    internal class ActionInputs
    {
        [JsonPropertyName("host")]
        public HostDefinition Host { get; set; }

        [JsonPropertyName("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    /// <summary>
    /// Host definition for a trigger or action.
    /// Contains connection name, operation ID, and API ID.
    /// </summary>
    internal class HostDefinition
    {
        [JsonPropertyName("connectionName")]
        public string ConnectionName { get; set; }

        [JsonPropertyName("operationId")]
        public string OperationId { get; set; }

        [JsonPropertyName("apiId")]
        public string ApiId { get; set; }
    }
}
