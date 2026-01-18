using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]

public class UserDefaultProfile
{
      
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UserEnums.UserTypes UserType { get; set; } = UserEnums.UserTypes.Lender;

    public Guid LenderId { get; set; } = Guid.Empty;

    public Guid BrokerId { get; set; } = Guid.Empty;

    public Guid ServicerId { get; set; } = Guid.Empty;

    public Guid OtherId { get; set; } = Guid.Empty;

    public string EmailDeliveryAddress { get; set; }
      
    public Guid LoanTypeId { get; set; }

    public string LoanTypeName { get; set; } = string.Empty;

    public List<Guid> AvailableDocumentLibraryGuids { get; set; } = new();

    public Guid DefaultDocumentLibraryGuid { get; set; } = Guid.Parse("533fb231-20f3-4819-8d83-64ede387bd02");

    public LoanTerms LoanTerms { get; set; } = new();
}