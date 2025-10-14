using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Xrm.Sdk;

namespace Fake4Dataverse.CloudFlows
{
    /// <summary>
    /// Converts OData/REST API values to Dataverse SDK types.
    /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
    /// 
    /// The Dataverse connector in Power Automate uses OData conventions for the Web API:
    /// - OptionSet values are represented as integers
    /// - EntityReferences use @odata.bind notation (e.g., "accounts(guid)")
    /// - Money values are represented as decimal numbers
    /// - Lookup fields use _fieldname_value for the GUID
    /// - DateTime values are in ISO 8601 format
    /// 
    /// This converter translates these OData representations to their corresponding SDK types
    /// that the OrganizationService expects (OptionSetValue, EntityReference, Money, etc.).
    /// </summary>
    public static class ODataValueConverter
    {
        /// <summary>
        /// Converts an attribute value from OData format to SDK format based on attribute metadata.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/web-api-types-operations
        /// 
        /// This method handles the conversion of common Dataverse types:
        /// - Integers that should be OptionSet values
        /// - Decimal numbers that should be Money values
        /// - String GUIDs with entity names that should be EntityReferences
        /// - ISO datetime strings that should be DateTime objects
        /// </summary>
        /// <param name="attributeName">The logical name of the attribute</param>
        /// <param name="value">The value in OData format (from JSON/REST API)</param>
        /// <param name="entityLogicalName">The entity logical name (for context)</param>
        /// <returns>The value converted to the appropriate SDK type</returns>
        public static object ConvertODataValue(string attributeName, object value, string entityLogicalName = null)
        {
            if (value == null)
                return null;

            // Handle System.Text.Json.JsonElement (from ASP.NET Core JSON deserialization)
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement
            if (value is JsonElement jsonElement)
            {
                value = ConvertJsonElement(jsonElement);
                if (value == null)
                    return null;
            }

            // If it's already an SDK type, return as-is
            if (value is OptionSetValue || value is EntityReference || value is Money)
                return value;

            // Try to detect and convert based on patterns and naming conventions
            
            // OptionSet fields often end with "code" (e.g., prioritycode, statecode, statuscode)
            // or are common option set fields
            if (IsLikelyOptionSetField(attributeName))
            {
                if (value is int intValue)
                    return new OptionSetValue(intValue);
                
                if (value is long longValue)
                    return new OptionSetValue((int)longValue);
                
                if (value is double doubleValue && doubleValue == Math.Floor(doubleValue))
                    return new OptionSetValue((int)doubleValue);
            }

            // Money fields often end with specific patterns or are common money fields
            if (IsLikelyMoneyField(attributeName))
            {
                if (value is decimal decValue)
                    return new Money(decValue);
                
                if (value is double dblValue)
                    return new Money((decimal)dblValue);
                
                if (value is int intMoneyValue)
                    return new Money(intMoneyValue);
            }

            // EntityReference fields use @odata.bind notation or _fieldname_value pattern
            // Format: "entityname(guid)" or just a GUID for lookup value fields
            if (value is string strValue)
            {
                // Check for @odata.bind format: "accounts(00000000-0000-0000-0000-000000000000)"
                var odataMatch = Regex.Match(strValue, @"^(\w+)\(([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\)$");
                if (odataMatch.Success)
                {
                    var refEntityName = odataMatch.Groups[1].Value;
                    var refId = Guid.Parse(odataMatch.Groups[2].Value);
                    return new EntityReference(refEntityName, refId);
                }

                // Check if attribute name indicates a lookup (ends with "id" but not the entity's own ID field)
                if (IsLikelyLookupField(attributeName, entityLogicalName))
                {
                    if (Guid.TryParse(strValue, out var lookupId))
                    {
                        // Try to infer the entity name from the attribute name
                        // Common pattern: regardingobjectid -> regardingobject, customerid -> customer
                        var lookupEntityName = InferEntityNameFromLookupField(attributeName);
                        if (!string.IsNullOrEmpty(lookupEntityName))
                        {
                            return new EntityReference(lookupEntityName, lookupId);
                        }
                    }
                }

                // Check for datetime strings
                if (DateTime.TryParse(strValue, out var dateValue))
                {
                    return dateValue;
                }
            }

            // Return the value as-is if no conversion is needed
            return value;
        }

        /// <summary>
        /// Converts all attributes in a dictionary from OData format to SDK format.
        /// </summary>
        public static Dictionary<string, object> ConvertODataAttributes(
            Dictionary<string, object> attributes, 
            string entityLogicalName = null)
        {
            if (attributes == null)
                return null;

            var converted = new Dictionary<string, object>();
            foreach (var attr in attributes)
            {
                converted[attr.Key] = ConvertODataValue(attr.Key, attr.Value, entityLogicalName);
            }
            return converted;
        }

        /// <summary>
        /// Converts a JsonElement to the appropriate .NET type.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonelement
        /// 
        /// When ASP.NET Core deserializes JSON into Dictionary<string, object>, the objects
        /// are JsonElement types that need to be converted to proper .NET types.
        /// </summary>
        private static object ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number => ConvertJsonNumber(element),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => null // Arrays and objects not yet supported in this context
            };
        }

        /// <summary>
        /// Converts a JSON number to the most appropriate .NET numeric type.
        /// Tries int first, then long, then decimal for maximum precision.
        /// </summary>
        private static object ConvertJsonNumber(JsonElement element)
        {
            // Try integer first
            if (element.TryGetInt32(out var intValue))
                return intValue;
            
            // Try long for larger integers
            if (element.TryGetInt64(out var longValue))
                return longValue;
            
            // Default to decimal for floating point (preserves precision)
            if (element.TryGetDecimal(out var decimalValue))
                return decimalValue;
            
            // Fallback to double
            return element.GetDouble();
        }

        /// <summary>
        /// Determines if an attribute name is likely an OptionSet field.
        /// Reference: Dataverse naming conventions for option set fields
        /// 
        /// Common OptionSet patterns:
        /// - Fields ending in "code" (prioritycode, statecode, statuscode, typecode)
        /// - Fields ending in "type" or "reason"
        /// - Common option set field names
        /// </summary>
        private static bool IsLikelyOptionSetField(string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return false;

            var lower = attributeName.ToLowerInvariant();

            // Common option set suffixes
            if (lower.EndsWith("code") || 
                lower.EndsWith("type") || 
                lower.EndsWith("reason") ||
                lower.EndsWith("mode"))
                return true;

            // Common specific option set fields
            var commonOptionSets = new HashSet<string>
            {
                "statecode", "statuscode", "prioritycode", "typecode",
                "category", "severity", "rating", "status",
                "preferredcontactmethod", "industrycode", "ownershipcode",
                "accountcategorycode", "accountclassificationcode",
                "businesstypecode", "customersizecode", "customertypecode",
                "paymenttermscode", "preferredappointmentdaycode",
                "preferredappointmenttimecode", "shippingmethodcode",
                "territorycode", "budgetstatus", "timezoneruleversionnumber"
            };

            return commonOptionSets.Contains(lower);
        }

        /// <summary>
        /// Determines if an attribute name is likely a Money field.
        /// Reference: Dataverse naming conventions for money fields
        /// </summary>
        private static bool IsLikelyMoneyField(string attributeName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return false;

            var lower = attributeName.ToLowerInvariant();

            // Common money field patterns
            if (lower.Contains("amount") || 
                lower.Contains("revenue") || 
                lower.Contains("budget") ||
                lower.Contains("price") ||
                lower.Contains("cost") ||
                lower.Contains("value") ||
                lower.Contains("fee") ||
                lower.Contains("charge"))
                return true;

            // Specific common money fields
            var commonMoneyFields = new HashSet<string>
            {
                "creditlimit", "annualincome", "estimatedvalue",
                "totalamount", "totallineitemamount", "totaldiscountamount",
                "totaltax", "freightamount", "exchangerate"
            };

            return commonMoneyFields.Contains(lower);
        }

        /// <summary>
        /// Determines if an attribute name is likely a lookup field.
        /// Lookup fields typically end with "id" but are not the entity's primary key.
        /// </summary>
        private static bool IsLikelyLookupField(string attributeName, string entityLogicalName)
        {
            if (string.IsNullOrEmpty(attributeName))
                return false;

            var lower = attributeName.ToLowerInvariant();

            // Must end with "id"
            if (!lower.EndsWith("id"))
                return false;

            // Exclude the entity's own ID field (primary key)
            if (!string.IsNullOrEmpty(entityLogicalName) && lower == entityLogicalName + "id")
                return false;

            // Common lookup fields
            var commonLookups = new HashSet<string>
            {
                "ownerid", "createdby", "modifiedby", "regardingobjectid",
                "parentaccountid", "primarycontactid", "customerid",
                "accountid", "contactid", "opportunityid", "leadid",
                "transactioncurrencyid", "owningbusinessunit", "owninguser"
            };

            return commonLookups.Contains(lower) || true; // Any field ending in "id" is potentially a lookup
        }

        /// <summary>
        /// Infers the entity name from a lookup field name.
        /// Examples: regardingobjectid -> regardingobject, customerid -> customer
        /// </summary>
        private static string InferEntityNameFromLookupField(string lookupFieldName)
        {
            if (string.IsNullOrEmpty(lookupFieldName))
                return null;

            var lower = lookupFieldName.ToLowerInvariant();

            // Remove "id" suffix
            if (lower.EndsWith("id"))
            {
                var baseName = lower.Substring(0, lower.Length - 2);

                // Special cases where the pattern doesn't follow simple rules
                var specialCases = new Dictionary<string, string>
                {
                    ["regardingobject"] = "activitypointer", // polymorphic
                    ["ownerid"] = "systemuser", // could be systemuser or team
                    ["customer"] = "account", // could be account or contact
                    ["parent"] = "account" // typically parent account
                };

                if (specialCases.ContainsKey(baseName))
                    return specialCases[baseName];

                // For most cases, the entity name is the base name
                return baseName;
            }

            return null;
        }
    }
}
