using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class SigningAuthority : ISigningPartyNames
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; }

    public string Title { get; set; }

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

    public string SSN { get; set; }

    public bool IsActive { get; set; } = true;
}