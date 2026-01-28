using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]

public class LoanDefaults   
{
      
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UserEnums.UserTypes? UserType { get; set; } = UserEnums.UserTypes.Lender;

    public Guid LenderId { get; set; } = Guid.Empty;

    public Guid BrokerId { get; set; } = Guid.Empty;

    public Guid ServicerId { get; set; } = Guid.Empty;

    public Guid OtherId { get; set; } = Guid.Empty;

    public string EmailDeliveryAddress { get; set; }
      
    public Guid LoanTypeId { get; set; }

    public string LoanTypeName { get; set; } = string.Empty;

    public List<Guid> AvailableDocumentLibraryGuids { get; set; } = new();

    public Guid DefaultDocumentLibraryGuid { get; set; } = Guid.Parse("533fb231-20f3-4819-8d83-64ede387bd02");
    
    public decimal PrincipalAmount { get; set; } = 0;

    public decimal InterestRate { get; set; } = 0;

    public decimal MaxInterestAllowed { get; set; } = 0;

    public int TermInMonths { get; set; } = 0;

    public decimal InitialMargin { get; set; } = 0;

    public VariableInterestProperties VariableInterestProperties { get; set; } = new();

    public BalloonPayments BalloonPayments { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.AmortizationTypes AmorizationType { get; set; } = Payment.AmortizationTypes.InterestOnly;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.Schedules RepaymentSchedule { get; set; } = DominateDocsData.Enums.Payment.Schedules.Monthly;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.RateIndexes RateIndex { get; set; } = DominateDocsData.Enums.Payment.RateIndexes.PRIME;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.RateTypes RateType { get; set; } = DominateDocsData.Enums.Payment.RateTypes.Fixed;
}