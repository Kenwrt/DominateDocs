using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class Document
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? DocLibId { get; set; }

    public Guid? DocSetId { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public string MasterTemplateDocumentUsedName { get; set; }

    public byte[] TemplateDocumentBytes { get; set; }

    public byte[] MergedDocumentBytes { get; set; }

    public byte[] DocumentPdfBytes { get; set; }

    public string HiddenTagName { get; set; } = "DominateDocsTag";

    public string HiddenTagValue { get; set; }

    public string? Language { get; set; }

    public bool IsStateLanguageRequired { get; set; } = false;

    public bool AreIndividualDocumentsRequired { get; set; } = false;

    public bool IsInDocumentStore { get; set; } = false;

    public DateTime? UpdatedAt { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public UsStates.UsState State { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Document.Types DocumentType { get; set; }

    public bool IsActive { get; set; } = true;
}