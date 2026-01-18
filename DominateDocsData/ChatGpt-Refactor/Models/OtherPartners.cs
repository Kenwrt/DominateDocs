using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
[Table("OtherPartners")]
public class OtherPartners
{
    [Key]
    [Required]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public bool IsActive { get; set; } = true;
}