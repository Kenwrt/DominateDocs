using DominateDocsData.Enums;
using DominateDocsData.Models.Storage;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class Document
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocLibId { get; set; }

    public string Name { get; set; }

    public Guid DocStoreId { get; set; }

    // Azure Blob pointers (instead of byte[])
    public BlobRef? TemplateRef { get; set; }
    public BlobRef? MergedRef { get; set; }

    public string MasterTemplateDocumentUsedName { get; set; }

    public byte[] TemplateDocumentBytes { get; set; } // Will delete this Later after data migration to Azure
    
    public byte[] MergedDocumentBytes { get; set; } // Will delete this Later after data migration to Azure
    
    public string HiddenTagName { get; set; } = "DominateDocsTag";
    
    public string HiddenTagValue { get; set; }
    
    public DateTime? UpdatedAt { get; set; } //will delete later after data migration to Azure

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public List<DocumentTypes.GenerateMultipleFor> GenerateMultipleFor { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    // Convenience flags (optional)
    [BsonIgnore]
    public bool HasTemplate => TemplateRef is not null && !TemplateRef.IsEmpty;

    [BsonIgnore]
    public bool HasMerged => MergedRef is not null && !MergedRef.IsEmpty;
}



  

   

   

   
