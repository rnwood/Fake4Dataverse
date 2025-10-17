using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Abstractions
{
    public interface ICallerProperties
    {
        EntityReference CallerId { get; set; }
        EntityReference BusinessUnitId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the user to impersonate when making requests.
        /// When set, this user's identity is used for security checks and audit fields,
        /// while CallerId represents the actual calling user.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
        /// </summary>
        EntityReference ImpersonatedUserId { get; set; }
    }
}
