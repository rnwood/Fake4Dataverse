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
        /// - $count: Include total count
        /// 
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-metadata-name-metadataid
        /// EntityDefinitions is the Web API name for EntityMetadata.
        /// </summary>
        [HttpGet("EntityDefinitions")]
        [EnableQuery]
        [Produces("application/json")]
        public IQueryable<EntityMetadata> ListEntityDefinitions()
        {
            try
            {
                // Get all entity metadata
                return _context.CreateMetadataQuery();
            }
            catch (Exception ex)
            {
                // For IQueryable, we can't return IActionResult directly, so throw or handle differently
                throw new InvalidOperationException($"Error listing entity metadata: {ex.Message}", ex);
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
        [EnableQuery]
        [Produces("application/json")]
        public IActionResult GetEntityDefinition(Guid id)
        {
            try
            {
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

                return Ok(entityMetadata);
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
        [EnableQuery]
        [Produces("application/json")]
        public IActionResult GetEntityDefinitionByLogicalName(string logicalName)
        {
            try
            {
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

                return Ok(entityMetadata);
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
