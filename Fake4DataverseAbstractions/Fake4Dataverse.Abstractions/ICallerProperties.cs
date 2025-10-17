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
        
        /// <summary>
        /// Gets the effective user for operations.
        /// Returns ImpersonatedUserId if impersonation is active, otherwise CallerId.
        /// This is the user identity used for security checks, audit fields, and ownership.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
        /// When impersonating, operations are performed as if the impersonated user made them.
        /// </summary>
        EntityReference GetEffectiveUser();
    }
}
