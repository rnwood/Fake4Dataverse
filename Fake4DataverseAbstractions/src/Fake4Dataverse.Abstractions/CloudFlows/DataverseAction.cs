using System;
using System.Collections.Generic;
using Fake4Dataverse.Abstractions.CloudFlows.Enums;

namespace Fake4Dataverse.Abstractions.CloudFlows
{
    /// <summary>
    /// Represents a Dataverse connector action in a Cloud Flow.
    /// Reference: https://learn.microsoft.com/en-us/connectors/commondataserviceforapps/
    /// 
    /// Dataverse actions perform operations on Dataverse records (Create, Update, Delete, etc.).
    /// These actions execute against the fake context in tests.
    /// </summary>
    public class DataverseAction : IFlowAction
    {
        public DataverseAction()
        {
            ActionType = "Dataverse";
            Parameters = new Dictionary<string, object>();
            Attributes = new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets or sets the action type. Always "Dataverse" for this action.
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the action name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the specific Dataverse action to perform
        /// </summary>
        public DataverseActionType DataverseActionType { get; set; }

        /// <summary>
        /// Gets or sets the logical name of the entity to operate on.
        /// Example: "account", "contact", "opportunity"
        /// </summary>
        public string EntityLogicalName { get; set; }

        /// <summary>
        /// Gets or sets the ID of the entity to operate on (for Update, Delete, Retrieve actions)
        /// </summary>
        public Guid? EntityId { get; set; }

        /// <summary>
        /// Gets or sets the attributes/fields to set on the entity (for Create, Update actions)
        /// </summary>
        public IDictionary<string, object> Attributes { get; set; }

        /// <summary>
        /// Gets or sets the filter criteria for ListRecords action.
        /// Example: "statecode eq 0 and estimatedvalue gt 100000"
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
        /// </summary>
        public string Filter { get; set; }

        /// <summary>
        /// Gets or sets the order by expression for ListRecords action.
        /// Example: "createdon desc"
        /// </summary>
        public string OrderBy { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of records to return for ListRecords action
        /// </summary>
        public int? Top { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
