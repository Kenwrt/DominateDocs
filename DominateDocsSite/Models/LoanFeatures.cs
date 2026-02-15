using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Models;

/// <summary>All toggleable loan features with their sub-fields.</summary>
public class LoanFeatures
{
    // ── Conditional Extension ──
    public bool ConditionalExtension { get; set; } = true;
    public string ExtensionCount { get; set; } = "2";
    public string ExtensionTermMonths { get; set; } = "6";
    public string ExtensionFeePercent { get; set; } = "0.50";

    // ── Prepayment Penalty ──
    public bool PrepaymentPenalty { get; set; } = true;
    public PrepayPenaltyType PrepayType { get; set; } = PrepayPenaltyType.Stepdown;
    public StepdownStructure StepdownStructure { get; set; } = StepdownStructure.FiveYear;
    public string GuaranteedMonths { get; set; } = "6";

    // ── Loan Intended for Sale ──
    public bool LoanIntendedForSale { get; set; }
    public List<Assignee> Assignees { get; set; } = [];

    // ── Partial Release ──
    public bool PartialRelease { get; set; }

    // ── Simple Toggles ──
    public bool BorrowerServiceFees { get; set; }
    public bool ACHPaymentsRequired { get; set; } = true;

    // ── Exit Fee ──
    public bool ExitFee { get; set; }
    public string ExitFeeDollar { get; set; } = string.Empty;
    public string ExitFeePercent { get; set; } = string.Empty;

    // ── Termination Fee ──
    public bool TerminationFee { get; set; }
    public TerminationFeeType TerminationType { get; set; } = TerminationFeeType.DollarAmount;
    public string TerminationDollar { get; set; } = string.Empty;
    public string TerminationPercent { get; set; } = string.Empty;

    // ── Document Inclusions ──
    public bool W9Included { get; set; } = true;
    public bool MERSLanguage { get; set; }
    public bool SigningAffidavit { get; set; }
}
