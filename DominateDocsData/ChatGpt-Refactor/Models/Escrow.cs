using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
[Table("Escrow")]
public class Escrow
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public bool IsCorporateEntity { get; set; } = false;

    [Column(TypeName = "nvarchar(120)")]
    public string? EntityName { get; set; }

    [Column(TypeName = "nvarchar(60)")]
    public string ContactName { get; set; }

    [Column(TypeName = "nvarchar(60)")]
    public string ContactEmail { get; set; }

    [Column(TypeName = "nvarchar(12)")]
    public string ContactPhoneNumber { get; set; }

    [Column(TypeName = "nvarchar(120)")]
    public string StreetAddress { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string City { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string State { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string County { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string TitleOrderNumber { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string TitleReportExceptionItemsToBeDeleted { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string AdditionalTitleEndorsmentRequested { get; set; }

    public DateTime? TitleReportEffectiveDate { get; set; }

    [Column(TypeName = "bool")]
    public bool IsNotificationAddress { get; set; } = false;

    [Column(TypeName = "bool")]
    public bool IsReduceTitleCoverAmount { get; set; } = false;

    [NotMapped]
    [BsonIgnore]
    public string FullAddress => $"{StreetAddress} {City} {State} {ZipCode}".Trim();

    [Column(TypeName = "nvarchar(15)")]
    public string ZipCode { get; set; }

    [Column(TypeName = "nvarchar(64)")]
    public string Country { get; set; }

    public bool IsActive { get; set; }
}