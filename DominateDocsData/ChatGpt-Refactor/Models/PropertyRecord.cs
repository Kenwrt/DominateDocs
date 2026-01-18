namespace LiquidDocsData.Models;

using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;

[BsonIgnoreExtraElements]
public class PropertyRecord : IPropertyAddresses
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string LegalDescription { get; set; }

    public List<PropertyOwner> PropertyOwners { get; set; } = new();
    public string PropertyOwnersFormatted { get; set; }

    public string FullAddress { get; set; }

    public string StreetAddress { get; set; }

    public string City { get; set; }

    public string State { get; set; }

    public string ZipCode { get; set; }

    public string County { get; set; }

    public string Country { get; set; }

    public double? Lat { get; set; }
    public double? Lng { get; set; }

    public string ParcelNumber { get; set; }

    public decimal EstimatedValue { get; set; }

    public decimal? LastAppraisedValue { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Property.Types PropertyType { get; set; }

    public double SquareFootage { get; set; }

    public int YearBuilt { get; set; }

    public List<Lien> Liens { get; set; }

    public DateTime? LastAppraisalDate { get; set; }

    public bool IsOwnerOccupied { get; set; } = false;

    public DateTime PurchaseDate { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal MinimumReleasePrice { get; set; }

    public decimal PropertyTax { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public string TitleDocumentNumber { get; set; }

    public string TitleOrderNumber { get; set; }

    public string TitleReportExceptionItemsToBeDeleted { get; set; }

    public string AdditionalTitleEndorsmentRequested { get; set; }

    public DateTime? TitleReportEffectiveDate { get; set; }

    public bool IsReduceTitleCoverAmount { get; set; } = false;

    public List<EntityOwner> EntityOwners { get; set; } = new();
    public string EntityOwnersFormatted { get; set; }

    public string SignatureLinesFormatted { get; set; }
}