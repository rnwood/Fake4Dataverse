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
        
        // Cache for downloaded CDM JSON content to avoid repeated network calls
        private static readonly Dictionary<string, string> _cdmCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();
        
        // Base URL for Microsoft's CDM GitHub repository
        private const string CDM_GITHUB_BASE_URL = "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments";
        private const string CDM_CRM_COMMON_PATH = "core/applicationCommon/foundationCommon/crmCommon";
        
        // Standard CDM schema groups for D365/Dataverse
        // Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon/foundationCommon/crmCommon
        private static readonly Dictionary<string, string> StandardSchemaPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "crmcommon", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/crmCommon.cdm.json" },
            { "sales", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/sales.cdm.json" },
            { "service", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/service.cdm.json" },
            { "portals", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/portals/portals.cdm.json" },
            { "customerinsights", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/customerInsights/customerInsights.cdm.json" },
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
        /// Downloads and parses standard CDM schema groups from Microsoft's CDM repository.
        /// Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon
        /// 
        /// This method downloads standard schema groups (crmcommon, sales, service, etc.) directly from
        /// Microsoft's official CDM repository on GitHub. It follows the imports in each schema file
        /// to recursively load all dependent entity definitions.
        /// </summary>
        /// <param name="schemaNames">Names of standard schemas to download (e.g., "crmcommon", "sales", "service")</param>
        /// <returns>Collection of EntityMetadata objects for all entities in the requested schemas</returns>
        public static async Task<IEnumerable<EntityMetadata>> FromStandardCdmSchemasAsync(IEnumerable<string> schemaNames)
        {
            if (schemaNames == null)
            {
                throw new ArgumentNullException(nameof(schemaNames));
            }
            
            var allMetadata = new List<EntityMetadata>();
            var processedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var schemaName in schemaNames)
            {
                if (!StandardSchemaPaths.TryGetValue(schemaName, out string url))
                {
                    throw new ArgumentException($"Unknown standard CDM schema: {schemaName}. Available schemas: {string.Join(", ", StandardSchemaPaths.Keys)}");
                }
                
                try
                {
                    var metadata = await DownloadAndParseWithImportsAsync(url, processedUrls);
                    allMetadata.AddRange(metadata);
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException($"Failed to download CDM schema for {schemaName} from {url}", ex);
                }
            }
            
            return allMetadata;
        }
        
        /// <summary>
        /// Recursively downloads and parses a CDM JSON file and all its imports.
        /// Uses caching to avoid repeated downloads of the same file.
        /// </summary>
        /// <param name="url">URL of the CDM JSON file to download</param>
        /// <param name="processedUrls">Set of already processed URLs to avoid circular dependencies</param>
        /// <returns>Collection of EntityMetadata objects from this file and all imports</returns>
        private static async Task<IEnumerable<EntityMetadata>> DownloadAndParseWithImportsAsync(string url, HashSet<string> processedUrls)
        {
            // Avoid processing the same file twice
            if (processedUrls.Contains(url))
            {
                return Enumerable.Empty<EntityMetadata>();
            }
            
            processedUrls.Add(url);
            
            // Check cache first
            string json;
            lock (_cacheLock)
            {
                if (_cdmCache.TryGetValue(url, out string cachedJson))
                {
                    json = cachedJson;
                }
                else
                {
                    json = null;
                }
            }
            
            // Download if not cached
            if (json == null)
            {
                json = await _httpClient.GetStringAsync(url);
                
                // Store in cache
                lock (_cacheLock)
                {
                    if (!_cdmCache.ContainsKey(url))
                    {
                        _cdmCache[url] = json;
                    }
                }
            }
            
            var allMetadata = new List<EntityMetadata>();
            
            // Parse the current document
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
                throw new InvalidOperationException($"Failed to parse CDM JSON from {url}. Ensure the file is valid CDM JSON format.", ex);
            }
            
            // Process imports first (dependencies must be loaded before entities that reference them)
            if (document?.Imports != null && document.Imports.Any())
            {
                var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
                
                foreach (var import in document.Imports)
                {
                    if (string.IsNullOrWhiteSpace(import.CorpusPath))
                    {
                        continue;
                    }
                    
                    // Resolve relative import paths
                    string importUrl;
                    if (import.CorpusPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        import.CorpusPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        importUrl = import.CorpusPath;
                    }
                    else
                    {
                        // Handle relative paths - remove leading "./" if present
                        var relativePath = import.CorpusPath.TrimStart('.', '/');
                        importUrl = baseUrl + relativePath;
                    }
                    
                    try
                    {
                        var importedMetadata = await DownloadAndParseWithImportsAsync(importUrl, processedUrls);
                        allMetadata.AddRange(importedMetadata);
                    }
                    catch (Exception ex)
                    {
                        // Log but don't fail - some imports might not be critical
                        Console.WriteLine($"Warning: Failed to load import {import.CorpusPath} from {url}: {ex.Message}");
                    }
                }
            }
            
            // Process entity definitions in this document
            if (document?.Definitions != null && document.Definitions.Any())
            {
                foreach (var definition in document.Definitions)
                {
                    // Only process LocalEntity types (entity definitions)
                    if (definition.Type != "LocalEntity" && definition.Type != "CdmEntityDefinition")
                    {
                        continue;
                    }
                    
                    var entityMetadata = ConvertToEntityMetadata(definition);
                    allMetadata.Add(entityMetadata);
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
