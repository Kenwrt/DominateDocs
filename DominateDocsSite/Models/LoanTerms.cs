using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Models;

/// <summary>Core loan terms — rate, amortization, schedule.</summary>
public class LoanTerms
{
    public string Principal { get; set; } = "2,000,000.00";
    public RateType RateType { get; set; } = RateType.Fixed;
    public string InterestRate { get; set; } = "12.00";
    public string DefaultRate { get; set; } = "18.00";
    public string LateChargePercent { get; set; } = "5.00";
    public string LateChargeDays { get; set; } = "10";
    public AmortizationType AmortType { get; set; } = AmortizationType.InterestOnly;
    public RepaymentSchedule RepaymentSchedule { get; set; } = RepaymentSchedule.Monthly;
    public string Term { get; set; } = "12";
    public DateTime? OriginationDate { get; set; }
    public DateTime? FirstPaymentDate { get; set; }
}

/// <summary>Construction/rehab details — nested under Bridge loan type.</summary>
public class ConstructionDetails
{
    public bool HasContractor { get; set; }
    public string ContractorName { get; set; } = string.Empty;
    public string ContractorRole { get; set; } = string.Empty;
    public List<Signatory> ContractorSignatories { get; set; } = [];

    public bool HasDesignPro { get; set; }
    public string DesignProName { get; set; } = string.Empty;
    public List<Signatory> DesignProSignatories { get; set; } = [];

    public bool DutchInterest { get; set; }
    public string HoldbackAmount { get; set; } = string.Empty;
    public string HoldbackPaidTo { get; set; } = string.Empty;
    public string HoldbackPaymentDue { get; set; } = string.Empty;  // combobox value
}

/// <summary>DSCR-specific loan details.</summary>
public class DSCRDetails
{
    public string Ratio { get; set; } = string.Empty;
    public bool ReserveAccount { get; set; }
}
