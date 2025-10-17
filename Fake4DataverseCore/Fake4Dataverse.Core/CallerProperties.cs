using System;
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse
{
    public class CallerProperties : ICallerProperties
    {
        public EntityReference CallerId { get; set; }
        public EntityReference BusinessUnitId { get; set; }
        
        /// <summary>
        /// Gets or sets the ID of the user to impersonate when making requests.
        /// When set, this user's identity is used for security checks and audit fields,
        /// while CallerId represents the actual calling user.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
        /// The impersonating user must have the prvActOnBehalfOfAnotherUser privilege.
        /// </summary>
        public EntityReference ImpersonatedUserId { get; set; }

        public CallerProperties() 
        {
            CallerId = new EntityReference("systemuser", Guid.NewGuid());
            BusinessUnitId = new EntityReference("businessunit", Guid.NewGuid());
        }
        
        /// <summary>
        /// Gets the effective user ID for operations.
        /// Returns ImpersonatedUserId if impersonation is active, otherwise CallerId.
        /// This is the user identity used for security checks, audit fields, and ownership.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/impersonate-another-user-web-api
        /// When impersonating, operations are performed as if the impersonated user made them.
        /// </summary>
        public EntityReference GetEffectiveUser()
        {
            return ImpersonatedUserId ?? CallerId;
        }
    }
}
