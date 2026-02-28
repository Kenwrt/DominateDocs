namespace DominateDocsData.Models;

using DominateDocsData.Enums;
using LiquidDocsData.Models;
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

    public bool HasLiens => Liens?.Any() == true;
    public bool HasPropertyOwners => PropertyOwners?.Any() == true;
  

    public DateOnly? LastAppraisalDate { get; set; }

    public bool IsOwnerOccupied { get; set; } = false;

    public DateOnly? PurchaseDate { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal MinimumReleasePrice { get; set; }

    public decimal PropertyTax { get; set; }

    public DateOnly? CreatedAt { get; set; }

    public string? Notes { get; set; }


    public bool IsPropertyOwnerSameAsBorrower { get; set; } = true;

    public bool IsPropertyOwnerSameAsGuarantor { get; set; } = true;

    public bool IsPropertyOwnerThridPartyOwner { get; set; } = true;

    public string TitleDocumentNumber { get; set; }

    public string TitleOrderNumber { get; set; }

    public string TitleReportExceptionItemsToBeDeleted { get; set; }

    public string AdditionalTitleEndorsmentRequested { get; set; }

    public DateOnly? TitleReportEffectiveDate { get; set; }

    public bool IsReduceTitleCoverAmount { get; set; } = false;

    public bool IsPropertyOwnerDisplay { get; set; } = false;

    public bool HasEntityOweners => EntityOwners?.Any() == true;
    public List<EntityOwner> EntityOwners { get; set; } = new();
    public string EntityOwnersFormatted { get; set; }

    public string SignatureLinesFormatted { get; set; }

}