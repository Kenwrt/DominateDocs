
using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

/// <summary>All toggleable loan features with their sub-fields.</summary>
public class LoanFeatures
{
    // ── Conditional Extension ──
    public bool IsConditionalExtension { get; set; } = true;
    public string ExtensionCount { get; set; } = "2";
    public string ExtensionTermMonths { get; set; } = "6";
    public string ExtensionFeePercent { get; set; } = "0.50";

    // ── Prepayment Penalty ──
    public bool IsPrepaymentPenalty { get; set; } = true;

  
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.PrepayPenaltyTypes PrepayType { get; set; } = Payment.PrepayPenaltyTypes.Stepdown;

   
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.StepdownStructures StepdownStructure { get; set; } = Payment.StepdownStructures.FiveYear;

    public string GuaranteedMonths { get; set; } = "6";

    // ── Loan Intended for Sale ──
    public bool IsLoanIntendedForSale { get; set; }
    public List<Assignee> Assignees { get; set; } = [];

    // ── Partial Release ──
    public bool IsPartialRelease { get; set; }

    // ── Simple Toggles ──
    public bool IsBorrowerServiceFees { get; set; }
    public bool IsACHPaymentsRequired { get; set; } = true;

    // ── Exit Fee ──
    public bool IsExitFee { get; set; }
    public string ExitFeeDollar { get; set; } = string.Empty;
    public string ExitFeePercent { get; set; } = string.Empty;

    // ── Termination Fee ──
    public bool IsTerminationFee { get; set; }
   
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.TerminationFeeTypes TerminationType { get; set; } = Payment.TerminationFeeTypes.DollarAmount;

    public string TerminationDollar { get; set; } = string.Empty;
    public string TerminationPercent { get; set; } = string.Empty;

    // ── Document Inclusions ──
    public bool IsW9Included { get; set; } = true;
    public bool IsMERSLanguage { get; set; }
    public bool IsSigningAffidavit { get; set; }
}
