using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fake4Dataverse.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Service.Controllers
{
    /// <summary>
    /// Controller for the $metadata endpoint that generates OData EDMX/CSDL.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-service-documents
    /// 
    /// The $metadata endpoint returns the Entity Data Model (EDM) that describes the structure
    /// of the Dataverse organization's data. This is returned as EDMX (Entity Data Model XML)
    /// using CSDL (Conceptual Schema Definition Language) version 4.0.
    /// 
    /// Reference: http://docs.oasis-open.org/odata/odata-csdl-xml/v4.01/odata-csdl-xml-v4.01.html
    /// CSDL defines the schema for OData services including entity types, properties, and relationships.
    /// </summary>
    [ApiController]
    [Route("api/data/v9.2")]
    public class ODataMetadataController : ControllerBase
    {
        private readonly IXrmFakedContext _context;

        public ODataMetadataController(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Returns the OData service metadata document (EDMX/CSDL).
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-service-documents
        /// 
        /// GET /api/data/v9.2/$metadata
        /// 
        /// The metadata document describes all entity types, complex types, enumerations,
        /// actions, and functions available in the service. It uses CSDL 4.0 format.
        /// 
        /// Reference: http://docs.oasis-open.org/odata/odata/v4.0/errata03/os/complete/part3-csdl/odata-v4.0-errata03-os-part3-csdl-complete.html
        /// CSDL (Conceptual Schema Definition Language) is an XML-based language that describes
        /// the Entity Data Model (EDM) exposed as an OData service.
        /// </summary>
        [HttpGet("$metadata")]
        [Produces("application/xml")]
        public IActionResult GetMetadata()
        {
            try
            {
                var edmx = GenerateEdmx();
                return Content(edmx, "application/xml");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = new
                    {
                        code = "0x80040217",
                        message = $"Error generating metadata: {ex.Message}"
                    }
                });
            }
        }

        /// <summary>
        /// Generates EDMX/CSDL document for the OData service.
        /// Reference: http://docs.oasis-open.org/odata/odata-csdl-xml/v4.01/odata-csdl-xml-v4.01.html
        /// 
        /// The EDMX document structure:
        /// - Edmx root element with OData version
        /// - DataServices containing Schema elements
        /// - Schema with namespace Microsoft.Dynamics.CRM
        /// - EntityType elements for each entity
        /// - EntityContainer with EntitySet elements
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-service-documents#csdl-metadata-document
        /// The Dataverse Web API uses the Microsoft.Dynamics.CRM namespace for all types.
        /// </summary>
        private string GenerateEdmx()
        {
            var sb = new StringBuilder();
            
            // EDMX root with OData v4.0 declarations
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">");
            sb.AppendLine("  <edmx:DataServices>");
            sb.AppendLine("    <Schema Namespace=\"Microsoft.Dynamics.CRM\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">");
            
            // Get all entity metadata
            var entityMetadataList = _context.CreateMetadataQuery().ToList();
            
            // Generate EntityType definitions
            foreach (var entityMetadata in entityMetadataList)
            {
                GenerateEntityType(sb, entityMetadata);
            }
            
            // Generate EntityContainer
            sb.AppendLine("      <EntityContainer Name=\"Container\">");
            
            foreach (var entityMetadata in entityMetadataList)
            {
                var entitySetName = entityMetadata.EntitySetName ?? (entityMetadata.LogicalName + "s");
                var entityTypeName = entityMetadata.SchemaName ?? entityMetadata.LogicalName;
                
                sb.AppendLine($"        <EntitySet Name=\"{entitySetName}\" EntityType=\"Microsoft.Dynamics.CRM.{entityTypeName}\" />");
            }
            
            sb.AppendLine("      </EntityContainer>");
            sb.AppendLine("    </Schema>");
            sb.AppendLine("  </edmx:DataServices>");
            sb.AppendLine("</edmx:Edmx>");
            
            return sb.ToString();
        }

        /// <summary>
        /// Generates an EntityType element in CSDL for an EntityMetadata.
        /// Reference: http://docs.oasis-open.org/odata/odata-csdl-xml/v4.01/odata-csdl-xml-v4.01.html#sec_EntityType
        /// 
        /// EntityType structure:
        /// - Key element with PropertyRef to primary key
        /// - Property elements for each attribute
        /// - NavigationProperty elements for relationships
        /// 
        /// Each Property has:
        /// - Name: Attribute logical name
        /// - Type: EDM type (Edm.String, Edm.Guid, Edm.Int32, etc.)
        /// - Nullable: Whether the attribute is required
        /// </summary>
        private void GenerateEntityType(StringBuilder sb, EntityMetadata entityMetadata)
        {
            var entityTypeName = entityMetadata.SchemaName ?? entityMetadata.LogicalName;
            
            sb.AppendLine($"      <EntityType Name=\"{entityTypeName}\">");
            
            // Key (Primary ID)
            if (!string.IsNullOrEmpty(entityMetadata.PrimaryIdAttribute))
            {
                sb.AppendLine("        <Key>");
                sb.AppendLine($"          <PropertyRef Name=\"{entityMetadata.PrimaryIdAttribute}\" />");
                sb.AppendLine("        </Key>");
            }
            
            // Properties (Attributes)
            if (entityMetadata.Attributes != null)
            {
                foreach (var attribute in entityMetadata.Attributes)
                {
                    GenerateProperty(sb, attribute);
                }
            }
            
            sb.AppendLine("      </EntityType>");
        }

        /// <summary>
        /// Generates a Property element for an AttributeMetadata.
        /// Reference: http://docs.oasis-open.org/odata/odata-csdl-xml/v4.01/odata-csdl-xml-v4.01.html#sec_Property
        /// 
        /// Maps Dataverse attribute types to EDM primitive types:
        /// - String → Edm.String
        /// - Integer → Edm.Int32
        /// - DateTime → Edm.DateTimeOffset
        /// - Boolean → Edm.Boolean
        /// - Decimal/Money → Edm.Decimal
        /// - Lookup → Edm.Guid
        /// - Picklist → Edm.Int32
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-properties
        /// The Web API uses standard EDM types for all Dataverse attribute types.
        /// </summary>
        private void GenerateProperty(StringBuilder sb, AttributeMetadata attribute)
        {
            var propertyName = attribute.LogicalName;
            var edmType = MapAttributeTypeToEdmType(attribute);
            var nullable = attribute.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired;
            
            sb.Append($"        <Property Name=\"{propertyName}\" Type=\"{edmType}\"");
            
            if (!nullable)
            {
                sb.Append(" Nullable=\"false\"");
            }
            
            // Add MaxLength for string attributes
            if (attribute is StringAttributeMetadata stringAttr && stringAttr.MaxLength.HasValue)
            {
                sb.Append($" MaxLength=\"{stringAttr.MaxLength.Value}\"");
            }
            
            // Add Precision and Scale for decimal attributes
            if (attribute is DecimalAttributeMetadata decimalAttr)
            {
                if (decimalAttr.Precision.HasValue)
                {
                    sb.Append($" Precision=\"{decimalAttr.Precision.Value}\"");
                }
            }
            
            sb.AppendLine(" />");
        }

        /// <summary>
        /// Maps Dataverse AttributeType to EDM primitive type.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-properties
        /// 
        /// Common mappings:
        /// - String/Memo → Edm.String
        /// - Integer/BigInt → Edm.Int32 or Edm.Int64
        /// - DateTime → Edm.DateTimeOffset
        /// - Boolean/TwoOptions → Edm.Boolean
        /// - Decimal/Money → Edm.Decimal
        /// - Double → Edm.Double
        /// - Uniqueidentifier/Lookup/Owner/Customer → Edm.Guid
        /// - Picklist/State/Status → Edm.Int32
        /// 
        /// Reference: http://docs.oasis-open.org/odata/odata-csdl-xml/v4.01/odata-csdl-xml-v4.01.html#sec_PrimitiveTypes
        /// EDM defines a set of primitive types that represent simple values.
        /// </summary>
        private string MapAttributeTypeToEdmType(AttributeMetadata attribute)
        {
            if (attribute is StringAttributeMetadata || attribute is MemoAttributeMetadata)
                return "Edm.String";
            
            if (attribute is IntegerAttributeMetadata)
                return "Edm.Int32";
            
            if (attribute is BigIntAttributeMetadata)
                return "Edm.Int64";
            
            if (attribute is DateTimeAttributeMetadata)
                return "Edm.DateTimeOffset";
            
            if (attribute is BooleanAttributeMetadata)
                return "Edm.Boolean";
            
            if (attribute is DecimalAttributeMetadata || attribute is MoneyAttributeMetadata)
                return "Edm.Decimal";
            
            if (attribute is DoubleAttributeMetadata)
                return "Edm.Double";
            
            if (attribute is UniqueIdentifierAttributeMetadata || 
                attribute is LookupAttributeMetadata)
                return "Edm.Guid";
            
            if (attribute is PicklistAttributeMetadata || 
                attribute is StateAttributeMetadata || 
                attribute is StatusAttributeMetadata)
                return "Edm.Int32";
            
            // Default to string for unknown types
            return "Edm.String";
        }
    }
}
