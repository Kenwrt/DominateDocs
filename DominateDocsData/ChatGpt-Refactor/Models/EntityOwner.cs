using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class EntityOwner : IOwnershipNames
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string FullAddress { get; set; }

    public string StreetAddress { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string ZipCode { get; set; }

    public string County { get; set; }

    public string Country { get; set; }

    public double? Lat { get; set; }
    public double? Lng { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.ContactRoles EntityRole { get; set; } = Entity.ContactRoles.Manager;

    public decimal PercentOfOwnership { get; set; }
}