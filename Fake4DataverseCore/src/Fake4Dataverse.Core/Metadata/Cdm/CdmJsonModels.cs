using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Fake4Dataverse.Metadata.Cdm
{
    /// <summary>
    /// Models for parsing CDM (Common Data Model) JSON schema files.
    /// Reference: https://github.com/microsoft/CDM
    /// 
    /// CDM JSON files define entity metadata including attributes, data types, and relationships
    /// in a standardized format used by Microsoft Dynamics 365, Power Platform, and other services.
    /// </summary>
    
    /// <summary>
    /// Root CDM document structure.
    /// CDM documents contain entity definitions, imports, and metadata.
    /// </summary>
    internal class CdmDocument
    {
        [JsonPropertyName("jsonSchemaSemanticVersion")]
        public string JsonSchemaSemanticVersion { get; set; }
        
        [JsonPropertyName("imports")]
        public List<CdmImport> Imports { get; set; }
        
        [JsonPropertyName("definitions")]
        public List<CdmEntityDefinition> Definitions { get; set; }
    }
    
    /// <summary>
    /// Represents an import reference to another CDM document.
    /// CDM supports importing definitions from other schema files.
    /// </summary>
    internal class CdmImport
    {
        [JsonPropertyName("corpusPath")]
        public string CorpusPath { get; set; }
        
        [JsonPropertyName("moniker")]
        public string Moniker { get; set; }
    }
    
    /// <summary>
    /// Represents an entity definition in CDM.
    /// Maps to EntityMetadata in Dynamics 365/Dataverse.
    /// </summary>
    internal class CdmEntityDefinition
    {
        [JsonPropertyName("$type")]
        public string Type { get; set; }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        
        [JsonPropertyName("hasAttributes")]
        public List<CdmAttributeDefinition> HasAttributes { get; set; }
        
        [JsonPropertyName("sourceName")]
        public string SourceName { get; set; }
    }
    
    /// <summary>
    /// Represents an attribute definition in CDM.
    /// Maps to AttributeMetadata in Dynamics 365/Dataverse.
    /// Reference: https://learn.microsoft.com/en-us/dotnet/api/microsoft.xrm.sdk.metadata.attributemetadata
    /// </summary>
    internal class CdmAttributeDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("dataType")]
        public object DataType { get; set; } // Can be string or complex object
        
        [JsonPropertyName("description")]
        public string Description { get; set; }
        
        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; }
        
        [JsonPropertyName("maximumLength")]
        public int? MaximumLength { get; set; }
        
        [JsonPropertyName("sourceName")]
        public string SourceName { get; set; }
        
        [JsonPropertyName("isPrimaryKey")]
        public bool? IsPrimaryKey { get; set; }
        
        [JsonPropertyName("isNullable")]
        public bool? IsNullable { get; set; }
        
        [JsonPropertyName("purpose")]
        public string Purpose { get; set; }
        
        [JsonPropertyName("appliedTraits")]
        public List<object> AppliedTraits { get; set; }
    }
    
    /// <summary>
    /// Represents a CDM data type reference.
    /// CDM supports both simple string types (e.g., "string", "guid") 
    /// and complex type references with additional properties.
    /// </summary>
    internal class CdmDataTypeReference
    {
        [JsonPropertyName("dataTypeReference")]
        public string DataTypeReference { get; set; }
        
        [JsonPropertyName("entity")]
        public string Entity { get; set; }
    }
}
