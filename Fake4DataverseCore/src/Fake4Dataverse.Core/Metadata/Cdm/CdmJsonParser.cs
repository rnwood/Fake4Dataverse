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
        
        // In-memory cache for downloaded CDM JSON content
        private static readonly Dictionary<string, string> _cdmMemoryCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _cacheLock = new object();
        
        // File cache directory (optional, set via SetCacheDirectory)
        private static string _fileCacheDirectory = null;
        
        // Base URL for Microsoft's CDM GitHub repository
        private const string CDM_GITHUB_BASE_URL = "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments";
        private const string CDM_CRM_COMMON_PATH = "core/applicationCommon/foundationCommon/crmCommon";
        
        // Standard CDM schema groups for D365/Dataverse
        // Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon/foundationCommon/crmCommon
        private static readonly Dictionary<string, string> StandardSchemaPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core CRM schemas
            { "crmcommon", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/crmCommon.manifest.cdm.json" },
            
            // Sales & Marketing
            { "sales", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/sales.manifest.cdm.json" },
            { "marketing", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/marketing/marketing.manifest.cdm.json" },
            { "fieldservice", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/fieldService/fieldService.manifest.cdm.json" },
            
            // Service & Support
            { "service", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/service.manifest.cdm.json" },
            { "projectservice", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/projectService/projectService.manifest.cdm.json" },
            
            // Portals & Web
            { "portals", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/portals/portals.manifest.cdm.json" },
            
            // Analytics & Insights
            { "customerinsights", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/customerInsights/customerInsights.manifest.cdm.json" },
            
            // Additional modules
            { "linkedinleadgen", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/linkedInLeadGen/linkedInLeadGen.manifest.cdm.json" },
            { "socialengagement", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/socialEngagement/socialEngagement.manifest.cdm.json" },
            { "gamification", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/gamification/gamification.manifest.cdm.json" },
        };
        
        // Individual entity CDM paths (for loading specific entities)
        // Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon
        private static readonly Dictionary<string, string> StandardEntityPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Core entities
            { "account", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Account.cdm.json" },
            { "contact", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Contact.cdm.json" },
            { "lead", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Lead.cdm.json" },
            { "systemuser", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/SystemUser.cdm.json" },
            { "team", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Team.cdm.json" },
            { "businessunit", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/BusinessUnit.cdm.json" },
            { "organization", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Organization.cdm.json" },
            
            // Activity entities
            { "email", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Email.cdm.json" },
            { "phonecall", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/PhoneCall.cdm.json" },
            { "appointment", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Appointment.cdm.json" },
            { "task", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Task.cdm.json" },
            { "letter", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Letter.cdm.json" },
            { "fax", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/Fax.cdm.json" },
            { "activityparty", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/ActivityParty.cdm.json" },
            
            // Sales entities
            { "opportunity", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/Opportunity.cdm.json" },
            { "quote", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/Quote.cdm.json" },
            { "order", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/SalesOrder.cdm.json" },
            { "salesorder", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/SalesOrder.cdm.json" },
            { "invoice", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/Invoice.cdm.json" },
            { "competitor", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/Competitor.cdm.json" },
            { "product", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/Product.cdm.json" },
            { "pricelevel", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/PriceLevel.cdm.json" },
            { "discount", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/sales/Discount.cdm.json" },
            
            // Service entities
            { "incident", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/Incident.cdm.json" },
            { "case", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/Incident.cdm.json" },
            { "contract", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/Contract.cdm.json" },
            { "entitlement", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/Entitlement.cdm.json" },
            { "knowledgearticle", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/service/KnowledgeArticle.cdm.json" },
            
            // Marketing entities
            { "campaign", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/marketing/Campaign.cdm.json" },
            { "list", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/marketing/List.cdm.json" },
            { "marketinglist", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/marketing/List.cdm.json" },
            { "campaignresponse", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/marketing/CampaignResponse.cdm.json" },
            
            // Field Service entities
            { "workorder", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/fieldService/WorkOrder.cdm.json" },
            { "bookableresource", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/fieldService/BookableResource.cdm.json" },
            { "bookableresourcebooking", $"{CDM_GITHUB_BASE_URL}/{CDM_CRM_COMMON_PATH}/fieldService/BookableResourceBooking.cdm.json" },
        };
        
        /// <summary>
        /// Sets the directory to use for file-based caching of downloaded CDM files.
        /// If set, downloaded CDM files will be cached to disk in addition to memory.
        /// </summary>
        /// <param name="cacheDirectory">Directory path for caching CDM files. Pass null to disable file caching.</param>
        public static void SetCacheDirectory(string cacheDirectory)
        {
            lock (_cacheLock)
            {
                _fileCacheDirectory = cacheDirectory;
                if (!string.IsNullOrEmpty(_fileCacheDirectory) && !Directory.Exists(_fileCacheDirectory))
                {
                    Directory.CreateDirectory(_fileCacheDirectory);
                }
            }
        }
        
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
            
            // Deduplicate by logical name - keep the first occurrence of each entity
            var deduplicated = allMetadata
                .GroupBy(e => e.LogicalName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            
            if (deduplicated.Count < allMetadata.Count)
            {
                Console.WriteLine($"Note: Deduplicated {allMetadata.Count - deduplicated.Count} duplicate entity definition(s)");
            }
            
            return deduplicated;
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
            
            // Check memory cache first
            string json = null;
            lock (_cacheLock)
            {
                if (_cdmMemoryCache.TryGetValue(url, out string cachedJson))
                {
                    json = cachedJson;
                }
            }
            
            // Check file cache if memory cache miss and file caching is enabled
            if (json == null && !string.IsNullOrEmpty(_fileCacheDirectory))
            {
                var fileName = GetCacheFileName(url);
                var filePath = Path.Combine(_fileCacheDirectory, fileName);
                
                if (File.Exists(filePath))
                {
                    try
                    {
                        json = File.ReadAllText(filePath);
                        
                        // Store in memory cache for faster subsequent access
                        lock (_cacheLock)
                        {
                            if (!_cdmMemoryCache.ContainsKey(url))
                            {
                                _cdmMemoryCache[url] = json;
                            }
                        }
                    }
                    catch
                    {
                        // If file read fails, download from network
                        json = null;
                    }
                }
            }
            
            // Check embedded CDM files if still not found
            if (json == null)
            {
                json = TryGetEmbeddedCdmFile(url);
                
                if (json != null)
                {
                    // Store in memory cache
                    lock (_cacheLock)
                    {
                        if (!_cdmMemoryCache.ContainsKey(url))
                        {
                            _cdmMemoryCache[url] = json;
                        }
                    }
                    
                    // Optionally store in file cache for even faster subsequent access
                    if (!string.IsNullOrEmpty(_fileCacheDirectory))
                    {
                        try
                        {
                            var fileName = GetCacheFileName(url);
                            var filePath = Path.Combine(_fileCacheDirectory, fileName);
                            Directory.CreateDirectory(_fileCacheDirectory);
                            File.WriteAllText(filePath, json);
                        }
                        catch
                        {
                            // Ignore file cache write errors
                        }
                    }
                }
            }
            
            // Download if not cached
            if (json == null)
            {
                json = await _httpClient.GetStringAsync(url);
                
                // Store in memory cache
                lock (_cacheLock)
                {
                    if (!_cdmMemoryCache.ContainsKey(url))
                    {
                        _cdmMemoryCache[url] = json;
                    }
                }
                
                // Store in file cache if enabled
                if (!string.IsNullOrEmpty(_fileCacheDirectory))
                {
                    try
                    {
                        var fileName = GetCacheFileName(url);
                        var filePath = Path.Combine(_fileCacheDirectory, fileName);
                        File.WriteAllText(filePath, json);
                    }
                    catch
                    {
                        // Ignore file cache write errors
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
                    else if (import.CorpusPath.StartsWith("/", StringComparison.Ordinal))
                    {
                        // Absolute path from repository root - need to resolve to base GitHub URL
                        // Extract the base GitHub URL up to "/schemaDocuments"
                        var schemaDocsIndex = url.IndexOf("/schemaDocuments/", StringComparison.OrdinalIgnoreCase);
                        if (schemaDocsIndex > 0)
                        {
                            var githubBaseUrl = url.Substring(0, schemaDocsIndex + "/schemaDocuments".Length);
                            importUrl = githubBaseUrl + import.CorpusPath;
                        }
                        else
                        {
                            // Fallback - treat as relative
                            var relativePath = import.CorpusPath.TrimStart('/');
                            importUrl = baseUrl + relativePath;
                        }
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
                    catch (HttpRequestException ex) when (ex.Message.Contains("404"))
                    {
                        // Log but don't fail - some imports might not exist (404 is acceptable for optional imports)
                        Console.WriteLine($"Warning: Import file not found: {import.CorpusPath} from {url}");
                    }
                    catch (Exception ex)
                    {
                        // All other errors are critical - throw them
                        throw new InvalidOperationException($"Failed to load import {import.CorpusPath} from {url}. Import URL: {importUrl}", ex);
                    }
                }
            }
            
            // Process entity references in manifest files
            // Manifest files use "entities" array to reference entity definition files
            if (document?.Entities != null && document.Entities.Any())
            {
                var baseUrl = url.Substring(0, url.LastIndexOf('/') + 1);
                
                foreach (var entityRef in document.Entities)
                {
                    if (string.IsNullOrWhiteSpace(entityRef.EntityPath))
                    {
                        continue;
                    }
                    
                    // Resolve entity path - extract just the file path (before the '/EntityName' suffix)
                    // Format: "Account.cdm.json/Account" -> "Account.cdm.json"
                    var entityFilePath = entityRef.EntityPath;
                    var slashIndex = entityFilePath.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        entityFilePath = entityFilePath.Substring(0, slashIndex);
                    }
                    
                    // Resolve relative entity paths
                    string entityUrl;
                    if (entityFilePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        entityFilePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        entityUrl = entityFilePath;
                    }
                    else
                    {
                        // Handle relative paths - remove leading "./" if present
                        var relativePath = entityFilePath.TrimStart('.', '/');
                        entityUrl = baseUrl + relativePath;
                    }
                    
                    try
                    {
                        var entityMetadata = await DownloadAndParseWithImportsAsync(entityUrl, processedUrls);
                        allMetadata.AddRange(entityMetadata);
                    }
                    catch (Exception ex)
                    {
                        // Entity references from manifests are critical - throw the error
                        throw new InvalidOperationException($"Failed to load entity {entityRef.EntityName} from {entityRef.EntityPath}. URL: {entityUrl}", ex);
                    }
                }
            }
            
            // Process entity definitions in this document
            if (document?.Definitions != null && document.Definitions.Any())
            {
                foreach (var definition in document.Definitions)
                {
                    // Process definitions that are entities (have entityName or name, and are LocalEntity type or no type specified)
                    var hasEntityIdentifier = !string.IsNullOrEmpty(definition.EntityName) || !string.IsNullOrEmpty(definition.Name);
                    var isEntityType = string.IsNullOrEmpty(definition.Type) || 
                                      definition.Type == "LocalEntity" || 
                                      definition.Type == "CdmEntityDefinition";
                    
                    if (!hasEntityIdentifier || !isEntityType)
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
                // If no Type is specified, assume it's an entity definition
                if (!string.IsNullOrEmpty(definition.Type) && 
                    definition.Type != "LocalEntity" && 
                    definition.Type != "CdmEntityDefinition")
                {
                    continue;
                }
                
                // Skip if no entityName or name is present (not a valid entity)
                if (string.IsNullOrEmpty(definition.EntityName) && string.IsNullOrEmpty(definition.Name))
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
            
            // Get entity name - prefer entityName, then name
            var entityName = !string.IsNullOrWhiteSpace(definition.EntityName) 
                ? definition.EntityName 
                : definition.Name;
            
            // Use sourceName (Dataverse logical name) if available, otherwise use name (CDM name converted to lowercase)
            // In CDM, sourceName typically contains the actual Dataverse entity logical name
            string logicalName = !string.IsNullOrWhiteSpace(definition.SourceName) 
                ? definition.SourceName 
                : (entityName?.ToLowerInvariant() ?? "unknown");
            
            entityMetadata.LogicalName = logicalName;
            
            // Parse attributes
            var attributes = new List<AttributeMetadata>();
            string primaryIdAttribute = null;
            
            if (definition.HasAttributes != null)
            {
                // Extract actual attribute definitions from the hasAttributes list
                var attributeDefinitions = ExtractAttributeDefinitions(definition.HasAttributes);
                
                foreach (var cdmAttribute in attributeDefinitions)
                {
                    var attributeMetadata = ConvertToAttributeMetadata(cdmAttribute, logicalName);
                    if (attributeMetadata != null)
                    {
                        attributes.Add(attributeMetadata);
                        
                        // Identify primary key
                        var purposeStr = GetPurposeString(cdmAttribute.Purpose);
                        if (cdmAttribute.IsPrimaryKey == true || 
                            purposeStr == "identifiedBy" ||
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
        /// Extracts attribute definitions from the hasAttributes list.
        /// The list can contain either direct CdmAttributeDefinition objects or attributeGroupReference wrappers.
        /// </summary>
        private static List<CdmAttributeDefinition> ExtractAttributeDefinitions(List<object> hasAttributes)
        {
            var result = new List<CdmAttributeDefinition>();
            
            foreach (var item in hasAttributes)
            {
                if (item == null) continue;
                
                // Try to deserialize as attributeGroupReference
                var jsonElement = (System.Text.Json.JsonElement)item;
                
                if (jsonElement.TryGetProperty("attributeGroupReference", out var groupRef))
                {
                    // This is an attributeGroupReference - extract the members
                    if (groupRef.TryGetProperty("members", out var members))
                    {
                        // Members can be attribute definitions or string references
                        // We need to deserialize each member individually
                        if (members.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var member in members.EnumerateArray())
                            {
                                // Skip string references (attribute references)
                                if (member.ValueKind == JsonValueKind.String)
                                {
                                    continue;
                                }
                                
                                // Only process object definitions
                                if (member.ValueKind == JsonValueKind.Object)
                                {
                                    try
                                    {
                                        var attribute = JsonSerializer.Deserialize<CdmAttributeDefinition>(member.GetRawText(), new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true,
                                            AllowTrailingCommas = true,
                                            ReadCommentHandling = JsonCommentHandling.Skip
                                        });
                                        
                                        if (attribute != null)
                                        {
                                            result.Add(attribute);
                                        }
                                    }
                                    catch (JsonException)
                                    {
                                        // Skip attributes that can't be deserialized (might be entity relationships or other complex types)
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // This is a direct attribute definition
                    var attribute = JsonSerializer.Deserialize<CdmAttributeDefinition>(jsonElement.GetRawText(), new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true,
                        ReadCommentHandling = JsonCommentHandling.Skip
                    });
                    
                    if (attribute != null)
                    {
                        result.Add(attribute);
                    }
                }
            }
            
            return result;
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
        
        /// <summary>
        /// Extracts the purpose string from a CDM purpose definition.
        /// CDM purpose can be a simple string or a complex object with references.
        /// </summary>
        private static string GetPurposeString(object purpose)
        {
            if (purpose == null)
            {
                return null;
            }
            
            // If it's a simple string
            if (purpose is string stringPurpose)
            {
                return stringPurpose;
            }
            
            // If it's a JsonElement (from deserialization)
            if (purpose is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    return jsonElement.GetString();
                }
                else if (jsonElement.ValueKind == JsonValueKind.Object)
                {
                    // Try to extract purposeReference property
                    if (jsonElement.TryGetProperty("purposeReference", out var purposeRef))
                    {
                        return purposeRef.GetString();
                    }
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Downloads and parses standard CDM entities by name from Microsoft's CDM repository.
        /// Reference: https://github.com/microsoft/CDM/tree/master/schemaDocuments/core/applicationCommon
        /// 
        /// This method allows loading specific entities (e.g., "account", "contact") rather than
        /// entire schema groups. Useful for tests or when only specific entities are needed.
        /// </summary>
        /// <param name="entityNames">Collection of entity names to download (e.g., "account", "contact", "lead")</param>
        /// <returns>Collection of EntityMetadata objects</returns>
        public static async Task<IEnumerable<EntityMetadata>> FromStandardCdmEntitiesAsync(IEnumerable<string> entityNames)
        {
            if (entityNames == null || !entityNames.Any())
            {
                return Enumerable.Empty<EntityMetadata>();
            }
            
            var allMetadata = new List<EntityMetadata>();
            var processedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var entityName in entityNames)
            {
                if (!StandardEntityPaths.TryGetValue(entityName, out string url))
                {
                    throw new ArgumentException($"Unknown standard CDM entity: {entityName}. Available entities: {string.Join(", ", StandardEntityPaths.Keys)}");
                }
                
                try
                {
                    var metadata = await DownloadAndParseWithImportsAsync(url, processedUrls);
                    allMetadata.AddRange(metadata);
                }
                catch (HttpRequestException ex)
                {
                    throw new InvalidOperationException($"Failed to download CDM entity {entityName} from {url}", ex);
                }
            }
            
            // Deduplicate by logical name - keep the first occurrence of each entity
            var deduplicated = allMetadata
                .GroupBy(e => e.LogicalName, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();
            
            if (deduplicated.Count < allMetadata.Count)
            {
                Console.WriteLine($"Note: Deduplicated {allMetadata.Count - deduplicated.Count} duplicate entity definition(s)");
            }
            
            return deduplicated;
        }
        
        /// <summary>
        /// Generates a safe file name for caching a CDM file based on its URL.
        /// </summary>
        private static string GetCacheFileName(string url)
        {
            // Use URL hash to create unique but consistent file names
            var hash = url.GetHashCode().ToString("X8");
            var fileName = url.Split('/').Last();
            return $"{hash}_{fileName}";
        }
        
        /// <summary>
        /// Attempts to load CDM content from embedded schema files in the repository.
        /// These files are included to prevent test timeouts and enable offline development.
        /// 
        /// Embedded files are located in cdm-schema-files/ directory at the repository root.
        /// Source: https://github.com/microsoft/CDM (CC BY 4.0 license)
        /// </summary>
        private static string TryGetEmbeddedCdmFile(string url)
        {
            // Map of known URLs to embedded file names
            // These are the most commonly used entities for testing
            var embeddedFileMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments/core/applicationCommon/Account.cdm.json", "Account.cdm.json" },
                { "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments/core/applicationCommon/Contact.cdm.json", "Contact.cdm.json" },
                { "https://raw.githubusercontent.com/microsoft/CDM/master/schemaDocuments/core/applicationCommon/foundationCommon/crmCommon/sales/Opportunity.cdm.json", "Opportunity.cdm.json" },
            };
            
            if (!embeddedFileMap.TryGetValue(url, out string embeddedFileName))
            {
                return null; // No embedded file for this URL
            }
            
            // Try multiple possible locations for the embedded files
            var possiblePaths = new[]
            {
                // Relative to the assembly location
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cdm-schema-files", embeddedFileName),
                // Relative to current directory (for development)
                Path.Combine(Directory.GetCurrentDirectory(), "cdm-schema-files", embeddedFileName),
                // Up one level (common in test scenarios)
                Path.Combine(Directory.GetCurrentDirectory(), "..", "cdm-schema-files", embeddedFileName),
                // Up two levels (for nested test projects)
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "cdm-schema-files", embeddedFileName),
                // Up three levels (for deeply nested structures)
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "cdm-schema-files", embeddedFileName),
                // Repository root from test projects
                Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "cdm-schema-files", embeddedFileName),
            };
            
            foreach (var path in possiblePaths)
            {
                try
                {
                    var fullPath = Path.GetFullPath(path);
                    if (File.Exists(fullPath))
                    {
                        return File.ReadAllText(fullPath);
                    }
                }
                catch
                {
                    // Ignore errors and try next path
                    continue;
                }
            }
            
            return null; // Embedded file not found
        }
        
        /// <summary>
        /// Loads system entity metadata from embedded CDM resources in the Fake4Dataverse.Core assembly.
        /// Reference: https://learn.microsoft.com/en-us/power-apps/developer/data-platform/reference/about-entity-reference
        /// 
        /// This method loads system entity metadata (solution, appmodule, sitemap, savedquery, systemform, webresource, appmodulecomponent)
        /// from embedded CDM schema files. These system entities are required for Model-Driven App functionality and solution management.
        /// 
        /// System entities included:
        /// - solution: Solution entity for ALM operations
        /// - appmodule: Model-Driven App entity
        /// - sitemap: Navigation structure entity
        /// - savedquery: System views entity
        /// - systemform: Entity forms entity
        /// - webresource: Web resources (JS, CSS, HTML) entity
        /// - appmodulecomponent: App component linking entity
        /// </summary>
        /// <returns>Collection of EntityMetadata for system entities</returns>
        public static IEnumerable<EntityMetadata> FromEmbeddedSystemEntities()
        {
            var systemEntityFiles = new[]
            {
                "Solution.cdm.json",
                "AppModule.cdm.json",
                "SiteMap.cdm.json",
                "SavedQuery.cdm.json",
                "SystemForm.cdm.json",
                "WebResource.cdm.json",
                "AppModuleComponent.cdm.json"
            };
            
            var allMetadata = new List<EntityMetadata>();
            var assembly = typeof(CdmJsonParser).Assembly;
            
            foreach (var fileName in systemEntityFiles)
            {
                // Try to load from embedded resource first
                var resourceName = $"Fake4Dataverse.Core.Metadata.Cdm.SystemEntities.{fileName}";
                
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        using (var reader = new System.IO.StreamReader(stream))
                        {
                            var json = reader.ReadToEnd();
                            var metadata = FromCdmJson(json);
                            allMetadata.AddRange(metadata);
                        }
                    }
                }
            }
            
            return allMetadata;
        }
    }
}

