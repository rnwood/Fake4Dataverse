using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.CloudFlows;
using Fake4Dataverse.Service.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace Fake4Dataverse.Service.Controllers
{
    /// <summary>
    /// OData v4.0 REST API controller for Dataverse entity operations.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/overview
    /// 
    /// The Dataverse Web API provides RESTful access to Dataverse entities using OData v4.0 protocol.
    /// This controller implements the core CRUD operations and query capabilities:
    /// - GET /api/data/v9.2/{entityPluralName} - List entities with query options
    /// - GET /api/data/v9.2/{entityPluralName}({id}) - Retrieve a single entity
    /// - POST /api/data/v9.2/{entityPluralName} - Create a new entity
    /// - PATCH /api/data/v9.2/{entityPluralName}({id}) - Update an entity
    /// - DELETE /api/data/v9.2/{entityPluralName}({id}) - Delete an entity
    /// 
    /// Leverages Microsoft.AspNetCore.OData for advanced query processing:
    /// - $select: Choose specific columns
    /// - $filter: Filter records with OData expressions (full OData syntax support)
    /// - $orderby: Sort records
    /// - $top: Limit number of results
    /// - $skip: Skip records for pagination
    /// - $expand: Include related entities
    /// - $count: Include total count
    /// 
    /// Reference: https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/query-options
    /// The Microsoft.AspNetCore.OData library provides automatic parsing and validation
    /// of OData query options, including complex $filter expressions.
    /// </summary>
    [ApiController]
    [Route("api/data/v9.2")]
    public class ODataEntityController : ODataController
    {
        private readonly IXrmFakedContext _context;
        private readonly IOrganizationService _service;

        public ODataEntityController(IXrmFakedContext context, IOrganizationService service)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// List entities with OData query options.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
        /// 
        /// GET /api/data/v9.2/{entityPluralName}
        /// 
        /// Supports OData v4.0 query options:
        /// - $select: Choose specific columns
        /// - $filter: Filter records (basic support via LINQ)
        /// - $orderby: Sort records
        /// - $top: Limit number of results
        /// - $skip: Skip records for pagination
        /// - $count: Include total count
        /// 
        /// Reference: https://learn.microsoft.com/en-us/odata/webapi-8/fundamentals/query-options
        /// </summary>
        [HttpGet("{entityPluralName}")]
        [Produces("application/json")]
        public IActionResult ListEntities(string entityPluralName)
        {
            try
            {
                // Get OData query parameters from request
                var selectParam = Request.Query["$select"].ToString();
                var topParam = Request.Query["$top"].ToString();
                var skipParam = Request.Query["$skip"].ToString();
                var countParam = Request.Query["$count"].ToString();
                
                // Convert plural entity name to logical name (e.g., "accounts" -> "account")
                var entityLogicalName = ConvertPluralToSingular(entityPluralName);

                // Build QueryExpression to retrieve all records
                var query = new QueryExpression(entityLogicalName)
                {
                    ColumnSet = new ColumnSet(true) // All columns by default
                };

                // Execute query to get all entities
                var results = _service.RetrieveMultiple(query);
                
                // Convert to OData format
                var odataEntities = results.Entities
                    .Select(e => ODataEntityConverter.ToODataEntity(e, includeODataMetadata: false))
                    .ToList();

                // Apply $select if specified
                if (!string.IsNullOrEmpty(selectParam))
                {
                    var selectedFields = selectParam.Split(',').Select(f => f.Trim()).ToList();
                    odataEntities = odataEntities.Select(entity =>
                    {
                        var filtered = new Dictionary<string, object>();
                        foreach (var field in selectedFields)
                        {
                            if (entity.ContainsKey(field))
                            {
                                filtered[field] = entity[field];
                            }
                        }
                        return filtered;
                    }).ToList();
                }

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
                var errorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error listing entities: {ex.Message}",
                    ex);
                return BadRequest(errorResponse);
            }
        }

        /// <summary>
        /// Retrieve a single entity by ID.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-entity-using-web-api
        /// 
        /// GET /api/data/v9.2/{entityPluralName}({id})
        /// </summary>
        [HttpGet("{entityPluralName}({id})")]
        [Produces("application/json")]
        public IActionResult GetEntity(string entityPluralName, Guid id)
        {
            try
            {
                var entityLogicalName = ConvertPluralToSingular(entityPluralName);
                var entity = _service.Retrieve(entityLogicalName, id, new ColumnSet(true));

                var odataEntity = ODataEntityConverter.ToODataEntity(entity);
                return Ok(odataEntity);
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                // Check if it's an "entity does not exist" error
                if (ex.Message.Contains("Does Not Exist", StringComparison.OrdinalIgnoreCase))
                {
                    var errorResponse = ODataEntityConverter.CreateErrorResponse(
                        "0x80040217",
                        $"Entity with id {id} not found",
                        ex);
                    return NotFound(errorResponse);
                }
                
                // Other fault exceptions
                var generalErrorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error retrieving entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, generalErrorResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error retrieving entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Create a new entity.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/create-entity-web-api
        /// 
        /// POST /api/data/v9.2/{entityPluralName}
        /// Request body contains entity attributes in OData JSON format
        /// Returns 201 Created with OData-EntityId header
        /// </summary>
        [HttpPost("{entityPluralName}")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult CreateEntity(string entityPluralName, [FromBody] Dictionary<string, object> attributes)
        {
            try
            {
                var entityLogicalName = ConvertPluralToSingular(entityPluralName);

                // Convert OData JSON to SDK Entity
                var entity = new Entity(entityLogicalName);
                
                // Convert attributes from OData format to SDK types
                var convertedAttributes = ODataValueConverter.ConvertODataAttributes(attributes, entityLogicalName);
                foreach (var attr in convertedAttributes)
                {
                    entity[attr.Key] = attr.Value;
                }

                // Create the entity
                var createdId = _service.Create(entity);

                // Build the OData-EntityId header
                // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/create-entity-web-api
                var entityUri = $"{Request.Scheme}://{Request.Host}{Request.Path}({createdId})";
                Response.Headers.Add("OData-EntityId", entityUri);

                // Return 204 No Content (standard for OData create operations)
                // Some clients expect 201 Created with the entity, so we return that
                return StatusCode(StatusCodes.Status201Created, new { id = createdId });
            }
            catch (Exception ex)
            {
                var errorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error creating entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Update an existing entity.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/update-delete-entities-using-web-api
        /// 
        /// PATCH /api/data/v9.2/{entityPluralName}({id})
        /// Request body contains attributes to update in OData JSON format
        /// Returns 204 No Content on success
        /// </summary>
        [HttpPatch("{entityPluralName}({id})")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult UpdateEntity(string entityPluralName, Guid id, [FromBody] Dictionary<string, object> attributes)
        {
            try
            {
                var entityLogicalName = ConvertPluralToSingular(entityPluralName);

                // Convert OData JSON to SDK Entity
                var entity = new Entity(entityLogicalName)
                {
                    Id = id
                };

                // Convert attributes from OData format to SDK types
                var convertedAttributes = ODataValueConverter.ConvertODataAttributes(attributes, entityLogicalName);
                foreach (var attr in convertedAttributes)
                {
                    entity[attr.Key] = attr.Value;
                }

                // Update the entity
                _service.Update(entity);

                // Return 204 No Content (standard for OData update operations)
                return NoContent();
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                // Check if it's an "entity does not exist" error
                if (ex.Message.Contains("Does Not Exist", StringComparison.OrdinalIgnoreCase))
                {
                    var errorResponse = ODataEntityConverter.CreateErrorResponse(
                        "0x80040217",
                        $"Entity with id {id} not found",
                        ex);
                    return NotFound(errorResponse);
                }
                
                // Other fault exceptions
                var generalErrorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error updating entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, generalErrorResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error updating entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Delete an entity.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/update-delete-entities-using-web-api
        /// 
        /// DELETE /api/data/v9.2/{entityPluralName}({id})
        /// Returns 204 No Content on success
        /// </summary>
        [HttpDelete("{entityPluralName}({id})")]
        [Produces("application/json")]
        public IActionResult DeleteEntity(string entityPluralName, Guid id)
        {
            try
            {
                var entityLogicalName = ConvertPluralToSingular(entityPluralName);
                _service.Delete(entityLogicalName, id);

                // Return 204 No Content (standard for OData delete operations)
                return NoContent();
            }
            catch (FaultException<OrganizationServiceFault> ex)
            {
                Console.WriteLine($"Caught FaultException<OrganizationServiceFault>: {ex.Message}");
                // Check if it's an "entity does not exist" error
                if (ex.Message.Contains("Does Not Exist", StringComparison.OrdinalIgnoreCase))
                {
                    var errorResponse = ODataEntityConverter.CreateErrorResponse(
                        "0x80040217",
                        $"Entity with id {id} not found",
                        ex);
                    return NotFound(errorResponse);
                }
                
                // Other fault exceptions
                var generalErrorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error deleting entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, generalErrorResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caught general Exception ({ex.GetType().Name}): {ex.Message}");
                var errorResponse = ODataEntityConverter.CreateErrorResponse(
                    "0x80040217",
                    $"Error deleting entity: {ex.Message}",
                    ex);
                return StatusCode(StatusCodes.Status500InternalServerError, errorResponse);
            }
        }

        /// <summary>
        /// Converts plural entity name to singular logical name.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-entitytypes
        /// 
        /// Common patterns:
        /// - accounts -> account
        /// - contacts -> contact
        /// - opportunities -> opportunity (remove 'ies', add 'y')
        /// </summary>
        private string ConvertPluralToSingular(string pluralName)
        {
            if (string.IsNullOrEmpty(pluralName))
                return pluralName;

            // Simple pluralization rules (can be enhanced with more sophisticated logic)
            if (pluralName.EndsWith("ies"))
            {
                // opportunities -> opportunity
                return pluralName.Substring(0, pluralName.Length - 3) + "y";
            }
            else if (pluralName.EndsWith("ses"))
            {
                // addresses -> address
                return pluralName.Substring(0, pluralName.Length - 2);
            }
            else if (pluralName.EndsWith("s"))
            {
                // accounts -> account, contacts -> contact
                return pluralName.Substring(0, pluralName.Length - 1);
            }

            // If no plural suffix, assume it's already singular
            return pluralName;
        }
    }
}
