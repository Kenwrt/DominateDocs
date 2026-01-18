using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
[Table("CorporationPartners")]
public class CorporationPartner
{
    [Key]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    public bool IsActive { get; set; } = true;
}