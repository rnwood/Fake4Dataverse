using System;
using System.Collections.Generic;
using System.Linq;
using Fake4Dataverse.Abstractions;
using Fake4Dataverse.Abstractions.Audit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.Service.Controllers
{
    /// <summary>
    /// API controller for audit functionality
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/overview
    /// 
    /// Provides REST endpoints to access audit history:
    /// - GET /api/audit - List all audit records (global summary)
    /// - GET /api/audit/entity/{entityName}/{id} - Get audit records for a specific entity record
    /// - GET /api/audit/details/{auditId} - Get detailed audit information including attribute changes
    /// </summary>
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase
    {
        private readonly IXrmFakedContext _context;
        private readonly IAuditRepository _auditRepository;

        public AuditController(IXrmFakedContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _auditRepository = _context.GetProperty<IAuditRepository>();
        }

        /// <summary>
        /// Get all audit records (global summary view)
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/retrieve-audit-data
        /// 
        /// GET /api/audit?top=100&skip=0&orderby=createdon desc
        /// 
        /// Returns a list of all audit records in the system, useful for global audit summary views.
        /// Supports pagination and ordering.
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        public IActionResult GetAllAudits(
            [FromQuery] int? top,
            [FromQuery] int? skip,
            [FromQuery] string? orderby,
            [FromQuery] string? filter)
        {
            try
            {
                var allAuditRecords = _auditRepository.GetAllAuditRecords().ToList();

                // Apply filtering by entity type if specified
                if (!string.IsNullOrEmpty(filter))
                {
                    // Simple filter parsing for entity type: filter=objecttypecode eq 'account'
                    if (filter.Contains("objecttypecode eq"))
                    {
                        var entityType = filter.Split('\'')[1];
                        allAuditRecords = allAuditRecords
                            .Where(a => a.Contains("objecttypecode") && 
                                   a.GetAttributeValue<string>("objecttypecode") == entityType)
                            .ToList();
                    }
                }

                // Apply ordering (default: createdon desc)
                var orderedRecords = allAuditRecords;
                if (orderby == "createdon desc" || string.IsNullOrEmpty(orderby))
                {
                    orderedRecords = allAuditRecords
                        .OrderByDescending(a => a.GetAttributeValue<DateTime>("createdon"))
                        .ToList();
                }
                else if (orderby == "createdon asc")
                {
                    orderedRecords = allAuditRecords
                        .OrderBy(a => a.GetAttributeValue<DateTime>("createdon"))
                        .ToList();
                }

                // Apply pagination
                var skipValue = skip ?? 0;
                var topValue = top ?? 100;
                var paginatedRecords = orderedRecords
                    .Skip(skipValue)
                    .Take(topValue)
                    .ToList();

                // Convert to simple objects for JSON serialization
                var result = new
                {
                    value = paginatedRecords.Select(ConvertAuditEntityToDto),
                    count = allAuditRecords.Count
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = "0x80040216", message = ex.Message } });
            }
        }

        /// <summary>
        /// Get audit records for a specific entity record
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/auditing/retrieve-audit-data
        /// 
        /// GET /api/audit/entity/{entityName}/{id}
        /// 
        /// Returns audit history for a single record, showing all changes made over time.
        /// </summary>
        [HttpGet("entity/{entityName}/{id}")]
        [Produces("application/json")]
        public IActionResult GetEntityAudits(string entityName, string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var recordId))
                {
                    return BadRequest(new { error = new { code = "0x80040216", message = "Invalid GUID format" } });
                }

                var entityRef = new EntityReference(entityName, recordId);
                var auditRecords = _auditRepository.GetAuditRecordsForEntity(entityRef).ToList();

                // Order by creation date descending (most recent first)
                var orderedRecords = auditRecords
                    .OrderByDescending(a => a.GetAttributeValue<DateTime>("createdon"))
                    .ToList();

                var result = new
                {
                    value = orderedRecords.Select(ConvertAuditEntityToDto),
                    count = orderedRecords.Count
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = "0x80040216", message = ex.Message } });
            }
        }

        /// <summary>
        /// Get detailed audit information including attribute changes
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.crm.sdk.messages.retrieveauditdetailsrequest
        /// 
        /// GET /api/audit/details/{auditId}
        /// 
        /// Returns detailed information about what changed in a specific audit record,
        /// including old and new values for modified attributes.
        /// </summary>
        [HttpGet("details/{auditId}")]
        [Produces("application/json")]
        public IActionResult GetAuditDetails(string auditId)
        {
            try
            {
                if (!Guid.TryParse(auditId, out var auditGuid))
                {
                    return BadRequest(new { error = new { code = "0x80040216", message = "Invalid GUID format" } });
                }

                var auditDetails = _auditRepository.GetAuditDetails(auditGuid);

                if (auditDetails == null)
                {
                    return NotFound(new { error = new { code = "0x80040217", message = "Audit details not found" } });
                }

                // Convert audit details to a serializable format
                var result = ConvertAuditDetailsToDto(auditDetails);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = "0x80040216", message = ex.Message } });
            }
        }

        /// <summary>
        /// Get audit status (whether auditing is enabled)
        /// 
        /// GET /api/audit/status
        /// </summary>
        [HttpGet("status")]
        [Produces("application/json")]
        public IActionResult GetAuditStatus()
        {
            try
            {
                var result = new
                {
                    isAuditEnabled = _auditRepository.IsAuditEnabled
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = "0x80040216", message = ex.Message } });
            }
        }

        /// <summary>
        /// Enable or disable auditing
        /// 
        /// POST /api/audit/status
        /// Body: { "isAuditEnabled": true }
        /// </summary>
        [HttpPost("status")]
        [Produces("application/json")]
        public IActionResult SetAuditStatus([FromBody] AuditStatusRequest request)
        {
            try
            {
                _auditRepository.IsAuditEnabled = request.IsAuditEnabled;

                var result = new
                {
                    isAuditEnabled = _auditRepository.IsAuditEnabled
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = new { code = "0x80040216", message = ex.Message } });
            }
        }

        /// <summary>
        /// Convert audit entity to a DTO for JSON serialization
        /// </summary>
        private object ConvertAuditEntityToDto(Entity audit)
        {
            return new
            {
                auditid = audit.GetAttributeValue<Guid>("auditid").ToString(),
                action = audit.GetAttributeValue<int>("action"),
                operation = audit.GetAttributeValue<string>("operation"),
                objectid = ConvertEntityReferenceToDto(audit.GetAttributeValue<EntityReference>("objectid")),
                objecttypecode = audit.GetAttributeValue<string>("objecttypecode"),
                userid = ConvertEntityReferenceToDto(audit.GetAttributeValue<EntityReference>("userid")),
                createdon = audit.GetAttributeValue<DateTime>("createdon").ToString("o"), // ISO 8601 format
            };
        }

        /// <summary>
        /// Convert entity reference to a DTO for JSON serialization
        /// </summary>
        private object? ConvertEntityReferenceToDto(EntityReference? entityRef)
        {
            if (entityRef == null) return null;

            return new
            {
                logicalName = entityRef.LogicalName,
                id = entityRef.Id.ToString(),
                name = entityRef.Name
            };
        }

        /// <summary>
        /// Convert audit details to a DTO for JSON serialization
        /// </summary>
        private object ConvertAuditDetailsToDto(object auditDetails)
        {
            // Handle AttributeAuditDetail type from Microsoft.Crm.Sdk.Messages
            if (auditDetails is Microsoft.Crm.Sdk.Messages.AttributeAuditDetail attributeAuditDetail)
            {
                var oldValueAttributes = new Dictionary<string, object?>();
                var newValueAttributes = new Dictionary<string, object?>();

                if (attributeAuditDetail.OldValue != null)
                {
                    foreach (var attr in attributeAuditDetail.OldValue.Attributes)
                    {
                        oldValueAttributes[attr.Key] = ConvertAttributeValue(attr.Value);
                    }
                }

                if (attributeAuditDetail.NewValue != null)
                {
                    foreach (var attr in attributeAuditDetail.NewValue.Attributes)
                    {
                        newValueAttributes[attr.Key] = ConvertAttributeValue(attr.Value);
                    }
                }

                return new
                {
                    auditRecord = ConvertAuditEntityToDto(attributeAuditDetail.AuditRecord),
                    oldValue = oldValueAttributes,
                    newValue = newValueAttributes
                };
            }

            // Return generic object if not recognized
            return new { details = auditDetails.ToString() };
        }

        /// <summary>
        /// Convert attribute value to a serializable format
        /// </summary>
        private object? ConvertAttributeValue(object? value)
        {
            if (value == null) return null;

            if (value is EntityReference entityRef)
            {
                return ConvertEntityReferenceToDto(entityRef);
            }
            else if (value is OptionSetValue optionSet)
            {
                return new { value = optionSet.Value };
            }
            else if (value is Money money)
            {
                return new { value = money.Value };
            }
            else if (value is DateTime dateTime)
            {
                return dateTime.ToString("o"); // ISO 8601 format
            }

            return value;
        }
    }

    /// <summary>
    /// Request model for setting audit status
    /// </summary>
    public class AuditStatusRequest
    {
        public bool IsAuditEnabled { get; set; }
    }
}
