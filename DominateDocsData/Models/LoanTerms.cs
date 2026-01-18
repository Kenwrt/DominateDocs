using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DominateDocsData.Models;

public class LoanTerms
{
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
