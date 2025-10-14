using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Xrm.Sdk.Metadata;

namespace Fake4Dataverse.Service.Controllers
{
    /// <summary>
    /// OData v4.0 REST API controller for Dataverse metadata operations.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
    /// 
    /// The Dataverse Web API provides RESTful access to metadata using OData v4.0 protocol.
    /// This controller implements querying of EntityMetadata (EntityDefinitions in the API):
    /// - GET /api/data/v9.2/EntityDefinitions - List all entity metadata
    /// - GET /api/data/v9.2/EntityDefinitions({id}) - Retrieve specific entity metadata
    /// - Supports $select, $filter, $orderby, $top, $skip, $expand query options
    /// - Supports $expand=Attributes to navigate to attribute metadata
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-metadata-name-metadataid
    /// EntityDefinitions corresponds to EntityMetadata in the SDK.
    /// </summary>
    [ApiController]
    [Route("api/data/v9.2")]
    public class MetadataController : ODataController
    {
        private readonly IXrmFakedContext _context;

        public MetadataController(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// List all EntityDefinitions with OData query options.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        /// 
        /// GET /api/data/v9.2/EntityDefinitions
        /// 
        /// Supports OData v4.0 query options:
        /// - $select: Choose specific properties (e.g., LogicalName, DisplayName)
        /// - $filter: Filter entity metadata (e.g., $filter=LogicalName eq 'account')
        /// - $orderby: Sort results
        /// - $top: Limit number of results
        /// - $skip: Skip records for pagination
        /// - $expand: Include related metadata (e.g., $expand=Attributes)
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-metadata-name-metadataid
        /// EntityDefinitions is the Web API name for EntityMetadata.
        /// </summary>
        [HttpGet("EntityDefinitions")]
        [Produces("application/json")]
        public IActionResult ListEntityDefinitions()
        {
            try
            {
                // Get OData query parameters from request
                var selectParam = Request.Query["$select"].ToString();
                var filterParam = Request.Query["$filter"].ToString();
                var topParam = Request.Query["$top"].ToString();
                var skipParam = Request.Query["$skip"].ToString();
                var expandParam = Request.Query["$expand"].ToString();
                var countParam = Request.Query["$count"].ToString();

                // Get all entity metadata
                var entityMetadataList = _context.CreateMetadataQuery().ToList();

                // Apply $filter if specified
                if (!string.IsNullOrEmpty(filterParam))
                {
                    entityMetadataList = ApplyMetadataFilter(entityMetadataList, filterParam);
                }

                // Convert to OData format
                var odataEntities = entityMetadataList
                    .Select(em => ConvertEntityMetadataToOData(em, selectParam, expandParam))
                    .ToList();

                // Store original count before paging
                var totalCount = odataEntities.Count;

                // Apply $skip
                if (int.TryParse(skipParam, out var skip) && skip > 0)
                {
                    odataEntities = odataEntities.Skip(skip).ToList();
                }

                // Apply $top
                if (int.TryParse(topParam, out var top) && top > 0)
                {
                    odataEntities = odataEntities.Take(top).ToList();
                }

                // Prepare response
                var response = new Dictionary<string, object>
                {
                    ["@odata.context"] = "$metadata#EntityDefinitions",
                    ["value"] = odataEntities
                };

                // Add count if requested
                if (countParam.Equals("true", StringComparison.OrdinalIgnoreCase))
                {
                    response["@odata.count"] = totalCount;
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = CreateMetadataErrorResponse(
                    "0x80040217",
                    $"Error listing entity metadata: {ex.Message}",
                    ex);
                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// Retrieve a single EntityDefinition by MetadataId.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-metadata-name-metadataid
        /// 
        /// GET /api/data/v9.2/EntityDefinitions({id})
        /// GET /api/data/v9.2/EntityDefinitions(LogicalName='account')
        /// 
        /// Each EntityMetadata has a unique MetadataId (GUID) that can be used to retrieve it.
        /// You can also retrieve by LogicalName using the alternate key syntax.
        /// </summary>
        [HttpGet("EntityDefinitions({id})")]
        [Produces("application/json")]
        public IActionResult GetEntityDefinition(Guid id)
        {
            try
            {
                var expandParam = Request.Query["$expand"].ToString();
                var selectParam = Request.Query["$select"].ToString();

                // Find entity metadata by MetadataId
                var entityMetadata = _context.CreateMetadataQuery()
                    .FirstOrDefault(em => em.MetadataId == id);

                if (entityMetadata == null)
                {
                    var errorResponse = CreateMetadataErrorResponse(
                        "0x80040217",
                        $"EntityDefinition with MetadataId {id} not found",
                        null);
                    return NotFound(errorResponse);
                }

                var odataEntity = ConvertEntityMetadataToOData(entityMetadata, selectParam, expandParam);
                
                // Add OData context for single entity
                var result = new Dictionary<string, object>
                {
                    ["@odata.context"] = $"$metadata#EntityDefinitions/$entity"
                };
                
                foreach (var kvp in odataEntity)
                {
                    result[kvp.Key] = kvp.Value;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorResponse = CreateMetadataErrorResponse(
                    "0x80040217",
                    $"Error retrieving entity metadata: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Retrieve EntityDefinition by LogicalName using alternate key syntax.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-metadata-name-metadataid
        /// 
        /// GET /api/data/v9.2/EntityDefinitions(LogicalName='account')
        /// 
        /// The Web API supports alternate key syntax to retrieve entities by natural keys.
        /// For EntityMetadata, LogicalName is a commonly used alternate key.
        /// </summary>
        [HttpGet("EntityDefinitions(LogicalName='{logicalName}')")]
        [Produces("application/json")]
        public IActionResult GetEntityDefinitionByLogicalName(string logicalName)
        {
            try
            {
                var expandParam = Request.Query["$expand"].ToString();
                var selectParam = Request.Query["$select"].ToString();

                // Find entity metadata by LogicalName
                var entityMetadata = _context.GetEntityMetadataByName(logicalName);

                if (entityMetadata == null)
                {
                    var errorResponse = CreateMetadataErrorResponse(
                        "0x80040217",
                        $"EntityDefinition with LogicalName '{logicalName}' not found",
                        null);
                    return NotFound(errorResponse);
                }

                var odataEntity = ConvertEntityMetadataToOData(entityMetadata, selectParam, expandParam);
                
                // Add OData context for single entity
                var result = new Dictionary<string, object>
                {
                    ["@odata.context"] = $"$metadata#EntityDefinitions/$entity"
                };
                
                foreach (var kvp in odataEntity)
                {
                    result[kvp.Key] = kvp.Value;
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                var errorResponse = CreateMetadataErrorResponse(
                    "0x80040217",
                    $"Error retrieving entity metadata: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Converts EntityMetadata to OData JSON format.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        /// 
        /// OData metadata representation includes:
        /// - MetadataId: Unique identifier for the metadata
        /// - LogicalName: The entity's logical name (e.g., "account")
        /// - SchemaName: The entity's schema name (e.g., "Account")
        /// - EntitySetName: The OData entity set name (e.g., "accounts")
        /// - DisplayName: Localized display name
        /// - PrimaryIdAttribute: Name of the primary key attribute
        /// - PrimaryNameAttribute: Name of the primary name attribute
        /// - Attributes: Collection of attribute metadata (included with $expand=Attributes)
        /// </summary>
        private Dictionary<string, object> ConvertEntityMetadataToOData(
            EntityMetadata entityMetadata, 
            string selectParam, 
            string expandParam)
        {
            if (entityMetadata == null)
                return null;

            var result = new Dictionary<string, object>();

            // Determine which properties to include
            var selectedProperties = string.IsNullOrEmpty(selectParam)
                ? null
                : selectParam.Split(',').Select(p => p.Trim()).ToList();

            // Helper to check if property should be included
            bool ShouldInclude(string propertyName)
            {
                return selectedProperties == null || selectedProperties.Contains(propertyName, StringComparer.OrdinalIgnoreCase);
            }

            // Add core metadata properties
            if (ShouldInclude("MetadataId"))
                result["MetadataId"] = entityMetadata.MetadataId ?? Guid.Empty;

            if (ShouldInclude("LogicalName"))
                result["LogicalName"] = entityMetadata.LogicalName;

            if (ShouldInclude("SchemaName"))
                result["SchemaName"] = entityMetadata.SchemaName;

            if (ShouldInclude("EntitySetName") && entityMetadata.EntitySetName != null)
                result["EntitySetName"] = entityMetadata.EntitySetName;

            if (ShouldInclude("DisplayName") && entityMetadata.DisplayName?.UserLocalizedLabel?.Label != null)
                result["DisplayName"] = new Dictionary<string, object>
                {
                    ["UserLocalizedLabel"] = new Dictionary<string, object>
                    {
                        ["Label"] = entityMetadata.DisplayName.UserLocalizedLabel.Label
                    }
                };

            if (ShouldInclude("DisplayCollectionName") && entityMetadata.DisplayCollectionName?.UserLocalizedLabel?.Label != null)
                result["DisplayCollectionName"] = new Dictionary<string, object>
                {
                    ["UserLocalizedLabel"] = new Dictionary<string, object>
                    {
                        ["Label"] = entityMetadata.DisplayCollectionName.UserLocalizedLabel.Label
                    }
                };

            if (ShouldInclude("Description") && entityMetadata.Description?.UserLocalizedLabel?.Label != null)
                result["Description"] = new Dictionary<string, object>
                {
                    ["UserLocalizedLabel"] = new Dictionary<string, object>
                    {
                        ["Label"] = entityMetadata.Description.UserLocalizedLabel.Label
                    }
                };

            if (ShouldInclude("PrimaryIdAttribute"))
                result["PrimaryIdAttribute"] = entityMetadata.PrimaryIdAttribute;

            if (ShouldInclude("PrimaryNameAttribute"))
                result["PrimaryNameAttribute"] = entityMetadata.PrimaryNameAttribute;

            if (ShouldInclude("ObjectTypeCode"))
                result["ObjectTypeCode"] = entityMetadata.ObjectTypeCode;

            if (ShouldInclude("IsActivity"))
                result["IsActivity"] = entityMetadata.IsActivity;

            if (ShouldInclude("IsCustomEntity"))
                result["IsCustomEntity"] = entityMetadata.IsCustomEntity;

            if (ShouldInclude("IsValidForAdvancedFind"))
                result["IsValidForAdvancedFind"] = entityMetadata.IsValidForAdvancedFind;

            // Handle $expand=Attributes
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
            // The Attributes navigation property provides access to all attribute metadata for the entity
            if (!string.IsNullOrEmpty(expandParam) && 
                expandParam.Contains("Attributes", StringComparison.OrdinalIgnoreCase))
            {
                if (entityMetadata.Attributes != null)
                {
                    result["Attributes"] = entityMetadata.Attributes
                        .Select(attr => ConvertAttributeMetadataToOData(attr))
                        .ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Converts AttributeMetadata to OData JSON format.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        /// 
        /// AttributeMetadata represents individual attributes (fields) of an entity:
        /// - MetadataId: Unique identifier
        /// - LogicalName: Attribute logical name
        /// - SchemaName: Attribute schema name
        /// - AttributeType: Type of attribute (String, Integer, Boolean, Lookup, etc.)
        /// - DisplayName: Localized display name
        /// - RequiredLevel: Whether the attribute is required
        /// - MaxLength: For string attributes, the maximum length
        /// </summary>
        private Dictionary<string, object> ConvertAttributeMetadataToOData(AttributeMetadata attributeMetadata)
        {
            if (attributeMetadata == null)
                return null;

            var result = new Dictionary<string, object>
            {
                ["MetadataId"] = attributeMetadata.MetadataId ?? Guid.Empty,
                ["LogicalName"] = attributeMetadata.LogicalName,
                ["SchemaName"] = attributeMetadata.SchemaName,
                ["AttributeType"] = attributeMetadata.AttributeType?.ToString()
            };

            if (attributeMetadata.DisplayName?.UserLocalizedLabel?.Label != null)
            {
                result["DisplayName"] = new Dictionary<string, object>
                {
                    ["UserLocalizedLabel"] = new Dictionary<string, object>
                    {
                        ["Label"] = attributeMetadata.DisplayName.UserLocalizedLabel.Label
                    }
                };
            }

            if (attributeMetadata.Description?.UserLocalizedLabel?.Label != null)
            {
                result["Description"] = new Dictionary<string, object>
                {
                    ["UserLocalizedLabel"] = new Dictionary<string, object>
                    {
                        ["Label"] = attributeMetadata.Description.UserLocalizedLabel.Label
                    }
                };
            }

            if (attributeMetadata.RequiredLevel != null)
            {
                result["RequiredLevel"] = new Dictionary<string, object>
                {
                    ["Value"] = attributeMetadata.RequiredLevel.Value.ToString()
                };
            }

            // Add type-specific properties
            if (attributeMetadata is StringAttributeMetadata stringAttr)
            {
                result["MaxLength"] = stringAttr.MaxLength;
            }
            else if (attributeMetadata is IntegerAttributeMetadata intAttr)
            {
                result["MinValue"] = intAttr.MinValue;
                result["MaxValue"] = intAttr.MaxValue;
            }
            else if (attributeMetadata is DecimalAttributeMetadata decimalAttr)
            {
                result["MinValue"] = decimalAttr.MinValue;
                result["MaxValue"] = decimalAttr.MaxValue;
                result["Precision"] = decimalAttr.Precision;
            }

            return result;
        }

        /// <summary>
        /// Applies OData $filter to entity metadata list.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-metadata-web-api
        /// 
        /// Supports basic filter expressions:
        /// - LogicalName eq 'account'
        /// - IsCustomEntity eq true
        /// - ObjectTypeCode gt 10000
        /// 
        /// This is a simple implementation. For full OData filter support,
        /// consider using Microsoft.AspNetCore.OData's built-in query parsing.
        /// </summary>
        private List<EntityMetadata> ApplyMetadataFilter(List<EntityMetadata> entities, string filterParam)
        {
            // Simple filter implementation - supports basic equality checks
            // Format: PropertyName eq 'value' or PropertyName eq true/false
            
            if (filterParam.Contains("LogicalName eq", StringComparison.OrdinalIgnoreCase))
            {
                var logicalName = ExtractFilterValue(filterParam, "LogicalName eq");
                return entities.Where(e => e.LogicalName == logicalName).ToList();
            }
            
            if (filterParam.Contains("SchemaName eq", StringComparison.OrdinalIgnoreCase))
            {
                var schemaName = ExtractFilterValue(filterParam, "SchemaName eq");
                return entities.Where(e => e.SchemaName == schemaName).ToList();
            }

            if (filterParam.Contains("IsCustomEntity eq", StringComparison.OrdinalIgnoreCase))
            {
                var value = ExtractFilterValue(filterParam, "IsCustomEntity eq");
                var boolValue = bool.Parse(value);
                return entities.Where(e => e.IsCustomEntity.HasValue && e.IsCustomEntity.Value == boolValue).ToList();
            }

            if (filterParam.Contains("IsActivity eq", StringComparison.OrdinalIgnoreCase))
            {
                var value = ExtractFilterValue(filterParam, "IsActivity eq");
                var boolValue = bool.Parse(value);
                return entities.Where(e => e.IsActivity.HasValue && e.IsActivity.Value == boolValue).ToList();
            }

            // Return all if filter not recognized
            return entities;
        }

        /// <summary>
        /// Extracts the value from a simple OData filter expression.
        /// </summary>
        private string ExtractFilterValue(string filter, string propertyPrefix)
        {
            var startIndex = filter.IndexOf(propertyPrefix, StringComparison.OrdinalIgnoreCase) + propertyPrefix.Length;
            var value = filter.Substring(startIndex).Trim();
            
            // Remove quotes if present
            value = value.Trim('\'', '"');
            
            return value;
        }

        /// <summary>
        /// Creates an OData error response for metadata operations.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/compose-http-requests-handle-errors
        /// 
        /// OData error format includes:
        /// - error.code: Error code
        /// - error.message: Human-readable error message
        /// </summary>
        private Dictionary<string, object> CreateMetadataErrorResponse(string code, string message, Exception ex)
        {
            var error = new Dictionary<string, object>
            {
                ["code"] = code,
                ["message"] = message
            };

            if (ex != null)
            {
                error["innererror"] = new Dictionary<string, object>
                {
                    ["message"] = ex.Message,
                    ["type"] = ex.GetType().FullName,
                    ["stacktrace"] = ex.StackTrace
                };
            }

            return new Dictionary<string, object>
            {
                ["error"] = error
            };
        }
    }
}
