using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class AkaName : IAliasNames
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public string AlsoKnownAs { get; set; }
}