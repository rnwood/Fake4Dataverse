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
        /// Gets or sets the number of records to skip for ListRecords action (paging support).
        /// Used with $skip query option in OData.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#paging
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Gets or sets whether to include the total count of records for ListRecords action.
        /// When true, returns @odata.count in the response.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#count
        /// </summary>
        public bool IncludeTotalCount { get; set; }

        /// <summary>
        /// Gets or sets the expand expression for ListRecords action.
        /// Used to retrieve related entities using navigation properties.
        /// Example: "primarycontactid($select=fullname,emailaddress1)"
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#expand-navigation-properties
        /// </summary>
        public string Expand { get; set; }

        /// <summary>
        /// Gets or sets the paging cookie for ListRecords continuation.
        /// Used to retrieve the next page of results.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api#paging-with-fetchxml
        /// </summary>
        public string PagingCookie { get; set; }

        /// <summary>
        /// Gets or sets the file/image column name for UploadFile/DownloadFile actions.
        /// Example: "entityimage" for contact photo, or custom file column name.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-attributes
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Gets or sets the file content as byte array for UploadFile action.
        /// This represents the binary content of the file to be uploaded.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-attributes#upload-file-data
        /// </summary>
        public byte[] FileContent { get; set; }

        /// <summary>
        /// Gets or sets the file name for UploadFile/DownloadFile actions.
        /// Example: "profile_photo.jpg", "document.pdf"
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/file-attributes
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets action parameters (implements IFlowAction.Parameters)
        /// </summary>
        public IDictionary<string, object> Parameters { get; set; }
    }
}
