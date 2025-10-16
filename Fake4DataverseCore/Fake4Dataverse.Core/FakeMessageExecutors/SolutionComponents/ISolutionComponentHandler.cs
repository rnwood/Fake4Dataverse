using System.IO.Compression;
using Fake4Dataverse.Abstractions;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.FakeMessageExecutors.SolutionComponents
{
    /// <summary>
    /// Interface for handling specific solution component types during import/export.
    /// Each component type (Entity, SavedQuery, SystemForm, etc.) has its own handler.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
    /// </summary>
    public interface ISolutionComponentHandler
    {
        /// <summary>
        /// Gets the component type code this handler supports.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
        /// </summary>
        int ComponentType { get; }

        /// <summary>
        /// Gets the name of the component type for error messages.
        /// </summary>
        string ComponentTypeName { get; }

        /// <summary>
        /// Imports a component from the solution ZIP archive.
        /// Uses CRUD operations on tables to create/update component data.
        /// </summary>
        /// <param name="zipArchive">The solution ZIP archive</param>
        /// <param name="solution">The solution entity being imported</param>
        /// <param name="ctx">The faked context</param>
        /// <param name="service">The organization service for CRUD operations</param>
        void ImportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service);

        /// <summary>
        /// Exports a component to the solution ZIP archive.
        /// Uses CRUD operations to read component data from tables.
        /// </summary>
        /// <param name="zipArchive">The solution ZIP archive being created</param>
        /// <param name="solution">The solution entity being exported</param>
        /// <param name="ctx">The faked context</param>
        /// <param name="service">The organization service for CRUD operations</param>
        void ExportComponent(ZipArchive zipArchive, Entity solution, IXrmFakedContext ctx, IOrganizationService service);
    }
}
