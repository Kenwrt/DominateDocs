using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class Borrower : IPartyNames
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.Structures EntityStructure { get; set; } = Entity.Structures.LLC;

    public string EntityStructureDescription => EntityStructure.GetDescription();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.ContactRoles ContactsRole { get; set; } = Entity.ContactRoles.Manager;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.Types EntityType { get; set; } = Entity.Types.Individual;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public UsStates.UsState StateOfIncorporation { get; set; }

    public string StateOfIncorporationDescription => StateOfIncorporation.GetDescription();

    public string EntityName { get; set; }

    public string ContactName { get; set; }

    public string ContactEmail { get; set; }

    public string ContactPhoneNumber { get; set; }

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

    public string EIN { get; set; }

    public bool IsAForgeinNational { get; set; } = false;
    public bool IsLanuageTranslatorRequired { get; set; } = false;
    public bool IsAliasNamesUsed { get; set; } = false;

    public bool IsSignatureAuthority { get; set; } = false;

    public bool IsActive { get; set; }

    public List<SigningAuthority> SigningAuthorities { get; set; } = new();
    public string SigningAuthoritiesFormatted { get; set; }

    public List<AkaName> AliasNames { get; set; } = new();
    public string AliasNamesFormatted { get; set; }

    public List<EntityOwner> EntityOwners { get; set; } = new();
    public string EntityOwnersFormatted { get; set; }

    public string SignatureLinesFormatted { get; set; }
}