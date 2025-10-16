using System;

namespace Fake4Dataverse.Abstractions.Security
{
    /// <summary>
    /// Configuration options for the Dataverse security model.
    /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security
    /// 
    /// The Dataverse security model controls access to data through a combination of:
    /// - Security roles (collections of privileges)
    /// - Business units (organizational hierarchy)
    /// - Record-level access (ownership and sharing)
    /// - Field-level security (column-level permissions)
    /// </summary>
    public interface ISecurityConfiguration
    {
        /// <summary>
        /// Gets or sets whether security enforcement is enabled.
        /// When false (default), all operations are allowed regardless of security settings.
        /// When true, operations are checked against the caller's privileges and access rights.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges
        /// </summary>
        bool SecurityEnabled { get; set; }

        /// <summary>
        /// Gets or sets whether modern business units are enabled.
        /// Modern business units use matrix-based security that allows users to have different
        /// levels of access in different business units.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/wp-security-cds
        /// When false: Traditional hierarchy-based security (user belongs to one business unit)
        /// When true: Matrix-based security (user can have access across multiple business units)
        /// 
        /// Default is false for backward compatibility.
        /// </summary>
        bool UseModernBusinessUnits { get; set; }

        /// <summary>
        /// Gets or sets the System Administrator role ID.
        /// The System Administrator role grants all privileges and bypasses security checks.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/database-security
        /// This is a well-known GUID in Dataverse: {C52D9CA4-3D13-43E7-9C23-D6C6F5FDD425}
        /// 
        /// If not set, the framework will use the default System Administrator GUID.
        /// </summary>
        Guid SystemAdministratorRoleId { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically grant System Administrator privileges
        /// to users with the System Administrator role.
        /// 
        /// When true (default), users assigned the System Administrator role bypass
        /// all security checks and have full access to all records and operations.
        /// </summary>
        bool AutoGrantSystemAdministratorPrivileges { get; set; }

        /// <summary>
        /// Gets or sets whether to enforce privilege depth checks.
        /// Privilege depth determines the level of access: Basic (user-owned), Local (business unit),
        /// Deep (business unit and child business units), or Global (organization-wide).
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-platform/admin/security-roles-privileges#privilege-and-role-matrix
        /// 
        /// When false (default), depth checks are not enforced.
        /// When true, operations check the privilege depth assigned to the role.
        /// </summary>
        bool EnforcePrivilegeDepth { get; set; }

        /// <summary>
        /// Gets or sets whether to enforce record-level security (ownership and sharing).
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/security-sharing-assigning
        /// 
        /// When false (default), ownership and sharing are tracked but not enforced.
        /// When true, users can only access records they own, records shared with them,
        /// or records they have access to through their role privileges.
        /// </summary>
        bool EnforceRecordLevelSecurity { get; set; }

        /// <summary>
        /// Gets or sets whether to enforce field-level security.
        /// Field-level security allows specific fields to be secured independently from the entity.
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/field-security-entities
        /// 
        /// When false (default), field-level security is not enforced.
        /// When true, secured fields require explicit field-level permissions.
        /// </summary>
        bool EnforceFieldLevelSecurity { get; set; }
    }
}
