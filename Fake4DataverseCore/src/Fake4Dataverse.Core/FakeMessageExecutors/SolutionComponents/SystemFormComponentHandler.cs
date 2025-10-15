using System.IO.Compression;
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.FakeMessageExecutors.SolutionComponents
{
    /// <summary>
    /// Handles import/export of SystemForm components (Component Type 60).
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
    /// 
    /// SystemForms are stored in the "systemform" table.
    /// This handler processes form definitions and uses CRUD operations to manage them.
    /// </summary>
    public class SystemFormComponentHandler : ISolutionComponentHandler
    {
        public int ComponentType => 60; // SystemForm

        public string ComponentTypeName => "SystemForm";

        public void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // TODO: Implement SystemForm import
            // This would extract SystemForms folder from ZIP
            // Parse systemform XML files
            // Use CRUD operations to create/update systemform records
        }

        public void ExportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service)
        {
            // TODO: Implement SystemForm export
            // Query systemform table via CRUD
            // Generate XML files and add to ZIP
        }
    }
}
