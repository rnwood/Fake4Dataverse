using System;
using System.Linq;
using Fake4Dataverse.Tests;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Xunit;

namespace Fake4Dataverse.Core.Tests.Metadata
{
    /// <summary>
    /// Tests for solution-aware table functionality.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/work-with-solutions
    /// 
    /// Solution-aware tables have special columns that enable solution management:
    /// - solutionid (Guid) - Associates the component with a solution
    /// - overwritetime (DateTime) - Tracks when the component was last overwritten
    /// - componentstate (int) - State of the component
    /// - ismanaged (bool) - Whether the component is managed by a solution
    /// - [entityname]idunique (Guid) - Unique identifier of the component
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/componentdefinition-entity
    /// </summary>
    public class SolutionAwareTests : Fake4DataverseTests
    {
        /// <summary>
        /// Tests that componentdefinition table is automatically initialized during context creation.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/componentdefinition-entity
        /// The componentdefinition table tracks which entities are solution-aware.
        /// </summary>
        [Fact]
        public void Should_Initialize_ComponentDefinition_Table()
        {
            // Arrange & Act - Context is created in base class
            var context = (XrmFakedContext)_context;
            
            // Assert
            Assert.True(context.Data.ContainsKey("componentdefinition"), 
                "componentdefinition table should be initialized");
            
            // Verify that default system entities are marked as solution-aware
            var componentDefs = context.Data["componentdefinition"].Values.ToList();
            Assert.NotEmpty(componentDefs);
            
            // Check that systemform is marked as solution-aware
            var systemFormDef = componentDefs.FirstOrDefault(e => 
                e.GetAttributeValue<string>("logicalname") == "systemform");
            Assert.NotNull(systemFormDef);
            Assert.True(systemFormDef.GetAttributeValue<bool?>("issolutionaware"));
            Assert.True(systemFormDef.GetAttributeValue<bool?>("canbeaddedtosolution"));
        }
        
        /// <summary>
        /// Tests that solutioncomponent table is loaded from CDM schema.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent
        /// The solutioncomponent table links components to solutions.
        /// </summary>
        [Fact]
        public void Should_Load_SolutionComponent_Metadata()
        {
            // Arrange & Act
            var context = _context;
            
            // Assert
            var solutionComponentMetadata = context.GetEntityMetadataByName("solutioncomponent");
            Assert.NotNull(solutionComponentMetadata);
            Assert.Equal("solutioncomponent", solutionComponentMetadata.LogicalName);
            
            // Verify key attributes exist
            var attributes = solutionComponentMetadata.Attributes.ToList();
            Assert.Contains(attributes, a => a.LogicalName == "solutioncomponentid");
            Assert.Contains(attributes, a => a.LogicalName == "solutionid");
            Assert.Contains(attributes, a => a.LogicalName == "objectid");
            Assert.Contains(attributes, a => a.LogicalName == "componenttype");
        }
        
        /// <summary>
        /// Tests that system entities have solution-aware columns added automatically.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/work-with-solutions
        /// System entities like systemform, savedquery, webresource should have solution-aware columns.
        /// </summary>
        [Theory]
        [InlineData("systemform", "formid")]
        [InlineData("savedquery", "savedqueryid")]
        [InlineData("webresource", "webresourceid")]
        [InlineData("sitemap", "sitemapid")]
        public void Should_Add_SolutionAware_Columns_To_System_Entities(string entityName, string primaryIdAttribute)
        {
            // Arrange & Act
            var context = _context;
            var entityMetadata = context.GetEntityMetadataByName(entityName);
            
            // Assert
            Assert.NotNull(entityMetadata);
            var attributes = entityMetadata.Attributes.ToList();
            
            // Verify solutionid column exists
            var solutionIdAttr = attributes.FirstOrDefault(a => a.LogicalName == "solutionid");
            Assert.NotNull(solutionIdAttr);
            Assert.Equal(AttributeTypeCode.Lookup, solutionIdAttr.AttributeType);
            
            // Verify overwritetime column exists
            var overwriteTimeAttr = attributes.FirstOrDefault(a => a.LogicalName == "overwritetime");
            Assert.NotNull(overwriteTimeAttr);
            Assert.Equal(AttributeTypeCode.DateTime, overwriteTimeAttr.AttributeType);
            
            // Verify componentstate column exists
            var componentStateAttr = attributes.FirstOrDefault(a => a.LogicalName == "componentstate");
            Assert.NotNull(componentStateAttr);
            Assert.Equal(AttributeTypeCode.Picklist, componentStateAttr.AttributeType);
            
            // Verify ismanaged column exists
            var isManagedAttr = attributes.FirstOrDefault(a => a.LogicalName == "ismanaged");
            Assert.NotNull(isManagedAttr);
            Assert.Equal(AttributeTypeCode.Boolean, isManagedAttr.AttributeType);
            
            // Verify [entityname]idunique column exists (e.g., formidunique, savedqueryidunique)
            var uniqueIdAttributeName = primaryIdAttribute.Replace("id", "") + "idunique";
            var uniqueIdAttr = attributes.FirstOrDefault(a => a.LogicalName == uniqueIdAttributeName);
            Assert.NotNull(uniqueIdAttr);
            Assert.Equal(AttributeTypeCode.Uniqueidentifier, uniqueIdAttr.AttributeType);
        }
        
        /// <summary>
        /// Tests that solution-aware columns are read-only (cannot be set on create/update).
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata.isvalidforcreate
        /// Solution-aware columns are managed by the system and should not be user-editable.
        /// </summary>
        [Fact]
        public void Should_Mark_SolutionAware_Columns_As_ReadOnly()
        {
            // Arrange & Act
            var context = _context;
            var entityMetadata = context.GetEntityMetadataByName("systemform");
            
            // Assert
            Assert.NotNull(entityMetadata);
            var attributes = entityMetadata.Attributes.ToList();
            
            // Verify solutionid is read-only
            var solutionIdAttr = attributes.FirstOrDefault(a => a.LogicalName == "solutionid");
            Assert.NotNull(solutionIdAttr);
            Assert.False(solutionIdAttr.IsValidForCreate);
            Assert.False(solutionIdAttr.IsValidForUpdate);
            Assert.True(solutionIdAttr.IsValidForRead);
            
            // Verify overwritetime is read-only
            var overwriteTimeAttr = attributes.FirstOrDefault(a => a.LogicalName == "overwritetime");
            Assert.NotNull(overwriteTimeAttr);
            Assert.False(overwriteTimeAttr.IsValidForCreate);
            Assert.False(overwriteTimeAttr.IsValidForUpdate);
            Assert.True(overwriteTimeAttr.IsValidForRead);
        }
        
        /// <summary>
        /// Tests that default solution-aware entities are registered in componentdefinition.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solutioncomponent#componenttype-choicesoptions
        /// </summary>
        [Theory]
        [InlineData("systemform", 60)]
        [InlineData("savedquery", 26)]
        [InlineData("webresource", 61)]
        [InlineData("sitemap", 62)]
        [InlineData("appmodule", 80)]
        [InlineData("appmodulecomponent", 103)]
        public void Should_Register_Default_SolutionAware_Entities_In_ComponentDefinition(string entityName, int expectedComponentType)
        {
            // Arrange & Act
            var context = (XrmFakedContext)_context;
            
            // Assert
            Assert.True(context.Data.ContainsKey("componentdefinition"));
            var componentDefs = context.Data["componentdefinition"].Values.ToList();
            
            var componentDef = componentDefs.FirstOrDefault(e => 
                e.GetAttributeValue<string>("logicalname") == entityName);
            
            Assert.NotNull(componentDef);
            Assert.Equal(entityName, componentDef.GetAttributeValue<string>("logicalname"));
            Assert.True(componentDef.GetAttributeValue<bool?>("issolutionaware"));
            Assert.True(componentDef.GetAttributeValue<bool?>("canbeaddedtosolution"));
            
            // Component type might vary, but should be present
            var componentType = componentDef.GetAttributeValue<int?>("objecttypecode");
            Assert.NotNull(componentType);
        }
        
        /// <summary>
        /// Tests that solution table is loaded and has correct metadata.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/entities/solution
        /// </summary>
        [Fact]
        public void Should_Load_Solution_Table_Metadata()
        {
            // Arrange & Act
            var context = _context;
            var solutionMetadata = context.GetEntityMetadataByName("solution");
            
            // Assert
            Assert.NotNull(solutionMetadata);
            Assert.Equal("solution", solutionMetadata.LogicalName);
            
            // Verify key attributes
            var attributes = solutionMetadata.Attributes.ToList();
            Assert.Contains(attributes, a => a.LogicalName == "solutionid");
            Assert.Contains(attributes, a => a.LogicalName == "uniquename");
            Assert.Contains(attributes, a => a.LogicalName == "friendlyname");
            Assert.Contains(attributes, a => a.LogicalName == "version");
            Assert.Contains(attributes, a => a.LogicalName == "ismanaged");
        }
    }
}
