using MongoDB.Bson.Serialization.Attributes;

namespace DominateDocsData.Models.MergeDTOs;

[BsonIgnoreExtraElements]
public class LenderDTO
{
    public List<Lender> LenderList { get; set; } = new();

    public string EntityDescriptors { get; set; } = string.Empty;

    public string EntitySignatureLines { get; set; } = string.Empty;
}