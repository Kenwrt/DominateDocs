using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
[Table("Liens")]
public class StateLendingLicense
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string LicenseNumber { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.UsStates.UsState State { get; set; }
}