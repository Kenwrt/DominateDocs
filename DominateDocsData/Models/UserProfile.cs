using DominateDocsData.Enums;
using DominateDocsData.Models.Stripe;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("UserId")]
    public Guid UserId { get; set; }

    [BsonElement("UserName")]
    public string? UserName { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }

    public string ConfirmedPassword { get; set; }

    public string? ProfilePictureUrl { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UserEnums.Roles UserRole { get; set; }

    public UserDefaultProfile UserDefaultProfile { get; set; } = new();

    public List<Guid> LoanAgreementGuids { get; set; } = new();

    public Subscription Subscription { get; set; } = new();

    public List<LoanDocumentSetGeneratedEvent> LoanDocsGenerated { get; set; } = new();

    public List<ChargingAuditTrail>? CharingAuditTrails { get; set; } = new();


}