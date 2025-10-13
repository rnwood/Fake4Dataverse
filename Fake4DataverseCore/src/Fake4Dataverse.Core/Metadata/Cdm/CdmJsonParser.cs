using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk.Metadata;
using Fake4Dataverse.Extensions;

namespace Fake4Dataverse.Metadata.Cdm
{
    /// <summary>
    /// Parses CDM (Common Data Model) JSON files and converts them to Dataverse EntityMetadata.
    /// Reference: https://github.com/microsoft/CDM
    /// 
    /// The Common Data Model is Microsoft's standard schema definition format that provides
    /// a shared data language across business applications and data sources. CDM JSON files
    /// define entity schemas with attributes, data types, and relationships.
    /// 
    /// This parser handles CDM JSON schema files and converts them to EntityMetadata objects
    /// that can be used to initialize metadata in XrmFakedContext.
    /// </summary>
    internal class CdmJsonParser
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        
        // Base URL for Microsoft's CDM GitHub repository
        private const string CDM_GITHUB_BASE_URL = "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments";
        
        // Standard CDM paths for D365/Dataverse entities
        // Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon/foundationCommon/crmCommon
        private static readonly Dictionary<string, string> StandardEntityPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Foundation entities
            { "Account", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/crmCommon/Account.cdm.json" },
            { "Contact", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/crmCommon/Contact.cdm.json" },
            { "Lead", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/crmCommon/Lead.cdm.json" },
            { "Opportunity", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/crmCommon/sales/Opportunity.cdm.json" },
            { "User", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/User.cdm.json" },
            { "Team", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/Team.cdm.json" },
            { "BusinessUnit", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/BusinessUnit.cdm.json" },
            { "Organization", $"{CDM_GITHUB_BASE_URL}/core/applicationCommon/foundationCommon/Organization.cdm.json" },
        };
        
        /// <summary>
        /// Parses CDM JSON from a file and converts it to EntityMetadata.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata
        /// </summary>
        /// <param name="filePath">Path to the CDM JSON file</param>
        /// <returns>Collection of EntityMetadata objects</returns>
        public static IEnumerable<EntityMetadata> FromCdmJsonFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"CDM JSON file not found: {filePath}");
            }
            
            string json = File.ReadAllText(filePath);
            return FromCdmJson(json);
        }
        
        /// <summary>
        /// Parses CDM JSON from multiple files and converts them to EntityMetadata.
        /// </summary>
        /// <param name="filePaths">Collection of file paths to CDM JSON files</param>
        /// <returns>Collection of EntityMetadata objects from all files</returns>
        public static IEnumerable<EntityMetadata> FromCdmJsonFiles(IEnumerable<string> filePaths)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }
            
            var allMetadata = new List<EntityMetadata>();
            foreach (var filePath in filePaths)
            {
                allMetadata.AddRange(FromCdmJsonFile(filePath));
            }
            
            return allMetadata;
        }
        
        /// <summary>
        /// Downloads and parses standard CDM entity schemas from Microsoft's CDM repository.
        /// Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon
        /// 
        /// This method downloads standard entity schemas (Account, Contact, etc.) directly from
        /// Microsoft's official CDM repository on GitHub.
        /// </summary>
        /// <param name="entityNames">Names of standard entities to download (e.g., "Account", "Contact")</param>
        /// <returns>Collection of EntityMetadata objects for the requested entities</returns>
        public static async Task<IEnumerable<EntityMetadata>> FromStandardCdmEntitiesAsync(IEnumerable<string> entityNames)
        {
            if (entityNames == null)
            {
                throw new ArgumentNullException(nameof(entityNames));
            }
            
            var allMetadata = new List<EntityMetadata>();
            foreach (var entityName in entityNames)
            {
                if (!StandardEntityPaths.TryGetValue(entityName, out string url))
                {
                    throw new ArgumentException($"Unknown standard CDM entity: {entityName}. Available entities: {string.Join(", ", StandardEntityPaths.Keys)}");
                }
                
                try
                {
                    var json = await _httpClient.GetStringAsync(url);
                    allMetadata.AddRange(FromCdmJson(json));
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException($"Failed to download CDM schema for {entityName} from {url}", ex);
                }
            }
            
            return allMetadata;
        }
        
        /// <summary>
        /// Parses CDM JSON string and converts it to EntityMetadata.
        /// </summary>
        /// <param name="json">CDM JSON string</param>
        /// <returns>Collection of EntityMetadata objects</returns>
        public static IEnumerable<EntityMetadata> FromCdmJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON cannot be null or empty", nameof(json));
            }
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            CdmDocument document;
            try
            {
                document = JsonSerializer.Deserialize<CdmDocument>(json, options);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Failed to parse CDM JSON. Ensure the file is valid CDM JSON format.", ex);
            }
            
            if (document?.Definitions == null || !document.Definitions.Any())
            {
                throw new InvalidOperationException("CDM document contains no entity definitions");
            }
            
            var entityMetadataList = new List<EntityMetadata>();
            
            foreach (var definition in document.Definitions)
            {
                // Only process LocalEntity types (entity definitions)
                if (definition.Type != "LocalEntity" && definition.Type != "CdmEntityDefinition")
                {
                    continue;
                }
                
                var entityMetadata = ConvertToEntityMetadata(definition);
                entityMetadataList.Add(entityMetadata);
            }
            
            return entityMetadataList;
        }
        
        /// <summary>
        /// Converts a CDM entity definition to Dataverse EntityMetadata.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.entitymetadata
        /// 
        /// EntityMetadata represents entity schema information including:
        /// - LogicalName: The unique logical name of the entity
        /// - Attributes: Collection of attribute metadata (fields)
        /// - PrimaryIdAttribute: The name of the primary key attribute
        /// </summary>
        private static EntityMetadata ConvertToEntityMetadata(CdmEntityDefinition definition)
        {
            var entityMetadata = new EntityMetadata();
            
            // Use sourceName (Dataverse logical name) if available, otherwise use name (CDM name converted to lowercase)
            // In CDM, sourceName typically contains the actual Dataverse entity logical name
            string logicalName = !string.IsNullOrWhiteSpace(definition.SourceName) 
                ? definition.SourceName 
                : definition.Name.ToLowerInvariant();
            
            entityMetadata.LogicalName = logicalName;
            
            // Parse attributes
            var attributes = new List<AttributeMetadata>();
            string primaryIdAttribute = null;
            
            if (definition.HasAttributes != null)
            {
                foreach (var cdmAttribute in definition.HasAttributes)
                {
                    var attributeMetadata = ConvertToAttributeMetadata(cdmAttribute, logicalName);
                    if (attributeMetadata != null)
                    {
                        attributes.Add(attributeMetadata);
                        
                        // Identify primary key
                        if (cdmAttribute.IsPrimaryKey == true || 
                            cdmAttribute.Purpose == "identifiedBy" ||
                            cdmAttribute.Name.Equals($"{logicalName}id", StringComparison.OrdinalIgnoreCase))
                        {
                            primaryIdAttribute = attributeMetadata.LogicalName;
                        }
                    }
                }
            }
            
            // Set attributes
            if (attributes.Any())
            {
                entityMetadata.SetSealedPropertyValue("Attributes", attributes.ToArray());
            }
            
            // Set primary ID attribute
            if (!string.IsNullOrWhiteSpace(primaryIdAttribute))
            {
                entityMetadata.SetFieldValue("_primaryIdAttribute", primaryIdAttribute);
            }
            
            return entityMetadata;
        }
        
        /// <summary>
        /// Converts a CDM attribute definition to Dataverse AttributeMetadata.
        /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata
        /// 
        /// AttributeMetadata represents field schema information including:
        /// - LogicalName: The unique logical name of the attribute
        /// - AttributeType: The data type of the attribute (String, Integer, etc.)
        /// - Additional properties based on attribute type (MaxLength for strings, etc.)
        /// </summary>
        private static AttributeMetadata ConvertToAttributeMetadata(CdmAttributeDefinition cdmAttribute, string entityLogicalName)
        {
            if (string.IsNullOrWhiteSpace(cdmAttribute.Name))
            {
                return null;
            }
            
            // Use sourceName if available, otherwise use name converted to lowercase
            string logicalName = !string.IsNullOrWhiteSpace(cdmAttribute.SourceName)
                ? cdmAttribute.SourceName
                : cdmAttribute.Name.ToLowerInvariant();
            
            // Determine data type
            string dataType = GetDataTypeString(cdmAttribute.DataType);
            
            // Create appropriate AttributeMetadata based on data type
            // Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributetypecode
            AttributeMetadata attributeMetadata = CreateAttributeMetadataForType(dataType, cdmAttribute);
            
            // Set common properties
            attributeMetadata.SetFieldValue("_logicalName", logicalName);
            attributeMetadata.SetFieldValue("_entityLogicalName", entityLogicalName);
            
            return attributeMetadata;
        }
        
        /// <summary>
        /// Creates the appropriate AttributeMetadata type based on the data type string.
        /// </summary>
        private static AttributeMetadata CreateAttributeMetadataForType(string dataType, CdmAttributeDefinition cdmAttribute)
        {
            string normalizedType = dataType.ToLowerInvariant();
            
            if (normalizedType == "string" || normalizedType == "text")
            {
                return new StringAttributeMetadata
                {
                    MaxLength = cdmAttribute.MaximumLength
                };
            }
            else if (normalizedType == "guid" || normalizedType == "uniqueidentifier")
            {
                if (cdmAttribute.IsPrimaryKey == true)
                {
                    var attr = new AttributeMetadata();
                    attr.SetSealedPropertyValue("AttributeType", AttributeTypeCode.Uniqueidentifier);
                    return attr;
                }
                return new UniqueIdentifierAttributeMetadata();
            }
            else if (normalizedType == "int" || normalizedType == "integer" || normalizedType == "int32")
            {
                return new IntegerAttributeMetadata();
            }
            else if (normalizedType == "long" || normalizedType == "int64" || normalizedType == "bigint")
            {
                return new BigIntAttributeMetadata();
            }
            else if (normalizedType == "decimal")
            {
                return new DecimalAttributeMetadata();
            }
            else if (normalizedType == "double" || normalizedType == "float")
            {
                return new DoubleAttributeMetadata();
            }
            else if (normalizedType == "boolean" || normalizedType == "bool")
            {
                return new BooleanAttributeMetadata();
            }
            else if (normalizedType == "datetime" || normalizedType == "date" || normalizedType == "time")
            {
                return new DateTimeAttributeMetadata();
            }
            else if (normalizedType == "money" || normalizedType == "currency")
            {
                return new MoneyAttributeMetadata();
            }
            else if (normalizedType == "picklist" || normalizedType == "optionset")
            {
                return new PicklistAttributeMetadata();
            }
            else if (normalizedType == "lookup" || normalizedType == "entityreference")
            {
                return new LookupAttributeMetadata();
            }
            else if (normalizedType == "memo" || normalizedType == "multilinetext")
            {
                return new MemoAttributeMetadata();
            }
            else if (normalizedType == "image" || normalizedType == "file")
            {
                return new ImageAttributeMetadata();
            }
            else
            {
                // Default to string for unknown types
                return new StringAttributeMetadata();
            }
        }
        
        /// <summary>
        /// Extracts the data type string from a CDM data type definition.
        /// CDM data types can be simple strings or complex objects with references.
        /// </summary>
        private static string GetDataTypeString(object dataType)
        {
            if (dataType == null)
            {
                return "string"; // Default to string
            }
            
            // If it's a simple string
            if (dataType is string stringType)
            {
                return stringType;
            }
            
            // If it's a JsonElement (from deserialization)
            if (dataType is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return jsonElement.GetString();
                }
                else if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    // Try to extract dataTypeReference property
                    if (jsonElement.TryGetProperty("dataTypeReference", out var dataTypeRef))
                    {
                        return dataTypeRef.GetString();
                    }
                }
            }
            
            return "string"; // Default to string if we can't determine the type
        }
    }
}
