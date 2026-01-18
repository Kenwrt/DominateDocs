using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
[Table("FeeToBePaid")]
public class FeeToBePaid
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; }

    public Guid LoanAgreementId { get; set; }

    public Guid SettlementStatmentId { get; set; }

    public string Description { get; set; }

    public decimal FeeAmmount { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.FeesPaidToOptions FeesPaidToOption { get; set; } = Payment.FeesPaidToOptions.PaymentDefferedUntilAfterClosing;
}