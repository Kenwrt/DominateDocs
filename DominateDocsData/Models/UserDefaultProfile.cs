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

    public LoanFeatures DefaultFeatures { get; set; } = new();

    public List<Guid> AvailableDocumentLibraryGuids { get; set; } = new();

    public Guid DefaultDocumentLibraryGuid { get; set; } = Guid.Parse("533fb231-20f3-4819-8d83-64ede387bd02");

    public string Principal { get; set; } = "2,000,000.00";
    public Payment.RateTypes RateType { get; set; } = Payment.RateTypes.Fixed;
    public string InterestRate { get; set; } = "12.00";
    public string DefaultRate { get; set; } = "18.00";
    public string LateChargePercent { get; set; } = "5.00";
    public string LateChargeDays { get; set; } = "10";
    public Payment.AmortizationTypes AmortType { get; set; } = Payment.AmortizationTypes.InterestOnly;
    public Payment.Schedules RepaymentSchedule { get; set; } = Payment.Schedules.Monthly;
    public string Term { get; set; } = "12";
    public DateTime? OriginationDate { get; set; }
    public DateTime? FirstPaymentDate { get; set; }
}