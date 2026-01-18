using MongoDB.Bson.Serialization.Attributes;

namespace DominateDocsData.Models.MergeDTOs;

[BsonIgnoreExtraElements]
public class GuarantorDTO
{
    public List<Guarantor> GuarantorList { get; set; } = new();

    public string EntityDescriptors { get; set; } = string.Empty;

    public string EntitySignatureLines { get; set; } = string.Empty;
}