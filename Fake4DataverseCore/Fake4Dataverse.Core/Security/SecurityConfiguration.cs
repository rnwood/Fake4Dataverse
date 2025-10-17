using System;
using Fake4Dataverse.Abstractions.Security;

namespace Fake4Dataverse.Security
{
    /// <summary>
    /// Default implementation of ISecurityConfiguration.
    /// Provides configuration options for the Dataverse security model.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
    /// Security is disabled by default for backward compatibility.
    /// </summary>
    public class SecurityConfiguration : ISecurityConfiguration
    {
        /// <summary>
        /// Well-known GUID for the System Administrator role in Dataverse.
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
        /// This is a standard GUID used across all Dataverse environments.
        /// </summary>
        public static readonly Guid DefaultSystemAdministratorRoleId = new Guid("C52D9CA4-3D13-43E7-9C23-D6C6F5FDD425");

        /// <summary>
        /// Creates a new SecurityConfiguration with security disabled by default.
        /// This ensures backward compatibility with existing code.
        /// </summary>
        public SecurityConfiguration()
        {
            SecurityEnabled = false;
            UseModernBusinessUnits = false;
            SystemAdministratorRoleId = DefaultSystemAdministratorRoleId;
            AutoGrantSystemAdministratorPrivileges = true;
            EnforcePrivilegeDepth = false;
            EnforceRecordLevelSecurity = false;
            EnforceFieldLevelSecurity = false;
        }

        /// <inheritdoc/>
        public bool SecurityEnabled { get; set; }

        /// <inheritdoc/>
        public bool UseModernBusinessUnits { get; set; }

        /// <inheritdoc/>
        public Guid SystemAdministratorRoleId { get; set; }

        /// <inheritdoc/>
        public bool AutoGrantSystemAdministratorPrivileges { get; set; }

        /// <inheritdoc/>
        public bool EnforcePrivilegeDepth { get; set; }

        /// <inheritdoc/>
        public bool EnforceRecordLevelSecurity { get; set; }

        /// <inheritdoc/>
        public bool EnforceFieldLevelSecurity { get; set; }

        /// <summary>
        /// Creates a SecurityConfiguration with security fully enabled.
        /// All security checks and enforcement options are turned on.
        /// </summary>
        /// <returns>A SecurityConfiguration with all security features enabled</returns>
        public static SecurityConfiguration CreateFullySecured()
        {
            return new SecurityConfiguration
            {
                SecurityEnabled = true,
                UseModernBusinessUnits = false,
                SystemAdministratorRoleId = DefaultSystemAdministratorRoleId,
                AutoGrantSystemAdministratorPrivileges = true,
                EnforcePrivilegeDepth = true,
                EnforceRecordLevelSecurity = true,
                EnforceFieldLevelSecurity = true
            };
        }

        /// <summary>
        /// Creates a SecurityConfiguration with basic security enabled.
        /// Only role-based security is enabled, without depth or record-level checks.
        /// </summary>
        /// <returns>A SecurityConfiguration with basic security features enabled</returns>
        public static SecurityConfiguration CreateBasicSecurity()
        {
            return new SecurityConfiguration
            {
                SecurityEnabled = true,
                UseModernBusinessUnits = false,
                SystemAdministratorRoleId = DefaultSystemAdministratorRoleId,
                AutoGrantSystemAdministratorPrivileges = true,
                EnforcePrivilegeDepth = false,
                EnforceRecordLevelSecurity = false,
                EnforceFieldLevelSecurity = false
            };
        }
    }
}
