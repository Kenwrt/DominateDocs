namespace DominateDocsData.Models;

using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

[BsonIgnoreExtraElements]
public abstract class EntityBase
{
    [Key]
    [Required]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid UserDefaultProfileId { get; set; }
    public Guid DefaultDocLibraryId { get; set; }

    public string? ReferenceCode { get; set; } = null;
    public string EntityName { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.Types EntityType { get; set; } 

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.Structures EntityStructure { get; set; }

    public string EntityStructureDescription => EntityStructure.GetDescription();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public UsStates.UsState StateOfOrganization { get; set; }

    public string StateOfOrganizationDescription => StateOfOrganization.GetDescription();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public UsStates.UsState PreferredStateVenue { get; set; }

    public string ContactName { get; set; }
    public string ContactEmail { get; set; }
    public string ContactPhoneNumber { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Entity.ContactRoles ContactTitle { get; set; }

    public string FullAddress { get; set; }
    public string StreetAddress { get; set; }
    public string City { get; set; }
    public string State { get; set; }
    public string ZipCode { get; set; }
    public string County { get; set; }
    public string Country { get; set; }
    public double? Lat { get; set; }
    public double? Lng { get; set; }

    public string EIN { get; set; }
    public string SSN { get; set; }

    public bool IsStateLicense { get; set; } = false;
    public bool IsCflLicensed { get; set; } = false;
    public bool HasStateLicenses => StateLicenses?.Any() == true;
    public List<License> StateLicenses { get; set; } = new();

    public bool HasAliasNames => AliasNames?.Any() == true;

    // ✅ NEW storage for aliases (strings).
    // Use a new BSON field name so existing docs with AliasNames (documents) don't crash.
    [BsonElement("AliasNamesNew")]
    public List<string> AliasNames { get; set; } = new();

    // ✅ Legacy storage (documents). This maps to the old field name in Mongo.
    [BsonElement("AliasNames")]
    public List<AkaName> AliasNamesOld { get; set; } = new();

    public string AliasNamesFormatted { get; set; }
    public bool IsAliasNamesUsed { get; set; } = false;

    public bool HasEntityOweners => EntityOwners?.Any() == true;
    public List<EntityOwner> EntityOwners { get; set; } = new();
    public string EntityOwnersFormatted { get; set; }

    public bool HasTrustees => Trustees?.Any() == true;
    public List<Trustee> Trustees { get; set; } = new();
    public string TrusteesFormatted { get; set; }

    public bool HasSigningAuthorities => SigningAuthorities?.Any() == true;
    public bool IsSignatureAuthority { get; set; } = false;
    public List<SigningAuthority> SigningAuthorities { get; set; } = new();
    public string SigningAuthoritiesFormatted { get; set; }

    public bool IsAForgeinNational { get; set; } = false;
    public bool IsLanuageTranslatorRequired { get; set; } = false;

    public string SignatureLinesFormatted { get; set; }
    public string FormattedName { get; set; }
}