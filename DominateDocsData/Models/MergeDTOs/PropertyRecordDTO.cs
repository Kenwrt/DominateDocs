using MongoDB.Bson.Serialization.Attributes;

namespace DominateDocsData.Models.MergeDTOs;

[BsonIgnoreExtraElements]
public class PropertyRecordDTO
{
    public List<PropertyRecord> PropertyList { get; set; } = new();

    public string EntityDescriptors { get; set; } = string.Empty;

    public string EntitySignatureLines { get; set; } = string.Empty;
}