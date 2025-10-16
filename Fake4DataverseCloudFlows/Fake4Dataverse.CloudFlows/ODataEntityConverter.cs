using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Converts Dataverse SDK types to OData/REST API JSON format.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
    /// 
    /// The Dataverse Web API uses OData v4.0 conventions for JSON representation:
    /// - OptionSet values are represented as integers
    /// - EntityReferences use navigation properties (e.g., "_primarycontactid_value")
    /// - Money values are represented as decimal numbers
    /// - DateTime values are in ISO 8601 format
    /// - Boolean values are lowercase (true/false)
    /// 
    /// This converter translates SDK types (OptionSetValue, EntityReference, Money, etc.)
    /// to their OData/JSON representations for REST API responses.
    /// 
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
    /// OData JSON responses include metadata annotations like @odata.context, @odata.etag
    /// </summary>
    public static class ODataEntityConverter
    {
        /// <summary>
        /// Converts an Entity to OData JSON format.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/retrieve-entity-using-web-api
        /// 
        /// OData entity representation includes:
        /// - Entity attributes converted to appropriate JSON types
        /// - Lookup fields with _fieldname_value format containing the GUID
        /// - Navigation properties with @odata.type annotations
        /// - Primary key as the entity ID field (e.g., accountid)
        /// </summary>
        /// <param name="entity">The SDK Entity to convert</param>
        /// <param name="includeODataMetadata">Include @odata.context and other metadata</param>
        /// <returns>Dictionary representing the OData JSON entity</returns>
        public static Dictionary<string, object> ToODataEntity(Entity entity, bool includeODataMetadata = true)
        {
            if (entity == null)
                return null;

            var result = new Dictionary<string, object>();

            // Add OData context metadata if requested
            if (includeODataMetadata)
            {
                result["@odata.context"] = $"#Microsoft.Dynamics.CRM.{entity.LogicalName}/$entity";
                
                // Add etag if available (used for optimistic concurrency)
                if (entity.RowVersion != null)
                {
                    result["@odata.etag"] = $"W/\"{entity.RowVersion}\"";
                }
            }

            // Add the primary key field (e.g., accountid, contactid)
            var primaryKeyField = entity.LogicalName + "id";
            result[primaryKeyField] = entity.Id;

            // Convert each attribute to OData format
            foreach (var attribute in entity.Attributes)
            {
                // Skip the ID if it's already been added as the primary key
                if (attribute.Key == primaryKeyField || attribute.Key == entity.LogicalName + "id")
                    continue;

                var odataValue = ConvertAttributeToOData(attribute.Key, attribute.Value);
                
                // For EntityReferences, add the lookup value field
                if (attribute.Value is EntityReference entityRef)
                {
                    // Add the _fieldname_value lookup field with GUID
                    result[$"_{attribute.Key}_value"] = entityRef.Id;
                    
                    // Optionally include the name in a formatted value field
                    if (!string.IsNullOrEmpty(entityRef.Name))
                    {
                        result[$"_{attribute.Key}_value@OData.Community.Display.V1.FormattedValue"] = entityRef.Name;
                    }
                }
                else if (odataValue != null)
                {
                    result[attribute.Key] = odataValue;
                }
            }

            // Add formatted values if available
            if (entity.FormattedValues != null)
            {
                foreach (var formattedValue in entity.FormattedValues)
                {
                    result[$"{formattedValue.Key}@OData.Community.Display.V1.FormattedValue"] = formattedValue.Value;
                }
            }

            return result;
        }

        /// <summary>
        /// Converts an EntityCollection to OData JSON format for list responses.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/query-data-web-api
        /// 
        /// OData collection responses include:
        /// - @odata.context pointing to the collection
        /// - value array containing entity objects
        /// - @odata.count if count was requested
        /// - @odata.nextLink for pagination if more results available
        /// </summary>
        /// <param name="entityCollection">The EntityCollection to convert</param>
        /// <param name="entityLogicalName">The logical name of the entity type</param>
        /// <param name="includeCount">Include the total count</param>
        /// <param name="nextLink">Pagination URL for next page</param>
        /// <returns>Dictionary representing the OData JSON collection response</returns>
        public static Dictionary<string, object> ToODataCollection(
            EntityCollection entityCollection, 
            string entityLogicalName,
            bool includeCount = false,
            string nextLink = null)
        {
            var result = new Dictionary<string, object>();

            // Add OData context for the collection
            result["@odata.context"] = $"#Microsoft.Dynamics.CRM.{entityLogicalName}";

            // Add count if requested
            if (includeCount && entityCollection.TotalRecordCount >= 0)
            {
                result["@odata.count"] = entityCollection.TotalRecordCount;
            }

            // Convert entities to OData format (without individual metadata)
            var entities = entityCollection.Entities
                .Select(e => ToODataEntity(e, includeODataMetadata: false))
                .ToList();

            result["value"] = entities;

            // Add pagination link if provided
            if (!string.IsNullOrEmpty(nextLink))
            {
                result["@odata.nextLink"] = nextLink;
            }

            return result;
        }

        /// <summary>
        /// Converts an SDK attribute value to its OData JSON representation.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
        /// 
        /// Type conversions:
        /// - OptionSetValue -> integer (the Value property)
        /// - Money -> decimal (the Value property)
        /// - EntityReference -> handled separately with _fieldname_value pattern
        /// - DateTime -> ISO 8601 string
        /// - bool -> lowercase true/false
        /// - Guid -> string representation
        /// </summary>
        private static object ConvertAttributeToOData(string attributeName, object value)
        {
            if (value == null)
                return null;

            // OptionSet values are represented as integers in OData
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#picklist-choices
            if (value is OptionSetValue optionSet)
            {
                return optionSet.Value;
            }

            // Money values are represented as decimals in OData
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#money
            if (value is Money money)
            {
                return money.Value;
            }

            // EntityReferences are handled by the caller using _fieldname_value pattern
            // We don't include the EntityReference object itself in the JSON
            if (value is EntityReference)
            {
                return null; // Handled separately by caller
            }

            // DateTime values in ISO 8601 format
            // Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations#date-and-time
            if (value is DateTime dateTime)
            {
                return dateTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
            }

            // OptionSetValueCollection (multi-select option sets) as array of integers
            if (value is OptionSetValueCollection optionSetCollection)
            {
                return optionSetCollection.Select(o => o.Value).ToArray();
            }

            // EntityCollection - convert to array of entities
            if (value is EntityCollection entityCollection)
            {
                return entityCollection.Entities.Select(e => ToODataEntity(e, includeODataMetadata: false)).ToArray();
            }

            // AliasedValue - unwrap and convert the actual value
            if (value is AliasedValue aliasedValue)
            {
                return ConvertAttributeToOData(aliasedValue.AttributeLogicalName, aliasedValue.Value);
            }

            // Guid values as strings
            if (value is Guid guid)
            {
                return guid.ToString();
            }

            // Boolean values (lowercase for JSON)
            if (value is bool boolValue)
            {
                return boolValue;
            }

            // All other types (string, int, decimal, etc.) pass through as-is
            return value;
        }

        /// <summary>
        /// Creates an OData error response.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/compose-http-requests-handle-errors
        /// 
        /// OData error format includes:
        /// - error object with code and message
        /// - Optional innererror with details
        /// </summary>
        public static Dictionary<string, object> CreateErrorResponse(string code, string message, Exception innerException = null)
        {
            var error = new Dictionary<string, object>
            {
                ["code"] = code ?? "0x80040217",
                ["message"] = message ?? "An error occurred"
            };

            if (innerException != null)
            {
                error["innererror"] = new Dictionary<string, object>
                {
                    ["message"] = innerException.Message,
                    ["type"] = innerException.GetType().Name,
                    ["stacktrace"] = innerException.StackTrace
                };
            }

            return new Dictionary<string, object>
            {
                ["error"] = error
            };
        }
    }
}
