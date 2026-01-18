using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class Guarantor : IPartyNames
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.GuarantorPosition.Types GuarantorType { get; set; } = LiquidDocsData.Enums.GuarantorPosition.Types.FullRecourse;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.ContactRoles ContactsRole { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.Structures EntityStructure { get; set; }

    public string EntityStructureDescription => EntityStructure.GetDescription();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.Types EntityType { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public UsStates.UsState StateOfIncorporation { get; set; }

    public string StateOfIncorporationDescription => StateOfIncorporation.GetDescription();

    public string EntityName { get; set; }

    public string ContactName { get; set; }

    public string ContactEmail { get; set; }

    public string ContactPhoneNumber { get; set; }

    public string SSN { get; set; }  // Encrypt or mask in production.

    public string FullAddress { get; set; }

    public string StreetAddress { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string ZipCode { get; set; }

    public string County { get; set; }

    public string Country { get; set; }

    public double? Lat { get; set; }

    public double? Lng { get; set; }

    public string? EIN { get; set; }

    public string? RelationshipToBorrower { get; set; }  // e.g. Parent, Business Partner

    public decimal Assets { get; set; }

    public decimal Liabilities { get; set; }

    public bool IsAForgeinNational { get; set; } = false;

    public bool IsLanuageTranslatorRequired { get; set; } = false;

    public bool IsSignatureAuthority { get; set; } = false;
    public List<SigningAuthority> SigningAuthorities { get; set; } = new();
    public string SigningAuthoritiesFormatted { get; set; }

    public List<AkaName> AliasNames { get; set; } = new();
    public string AliasNamesFormatted { get; set; }

    public bool IsAliasNamesUsed { get; set; } = false;

    public List<EntityOwner> EntityOwners { get; set; } = new();
    public string EntityOwnersFormatted { get; set; }

    public string SignatureLinesFormatted { get; set; }

    public bool IsActive { get; set; } = true;
}