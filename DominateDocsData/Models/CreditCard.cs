using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DominateDocsData.Models;

public class CreditCard
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CardSubscribeId { get; set; }
    public Guid UserId { get; set; }
    public string? CardholderName { get; set; }
    public string? Last4Digits { get; set; }
    public string? CardNumber { get; set; }
    public string? CCV { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public Enums.CreditCard.Types CardType { get; set; } = Enums.CreditCard.Types.MasterCard;

    public string? BillingAddress { get; set; }
    public string? BillingCity { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UsStates.UsState BillingState { get; set; }

    public string? BillingZipCode { get; set; }
    public string? ExpDate { get; set; }
    public bool IsActive { get; set; } = true;
}