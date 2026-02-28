namespace DominateDocsData.Enums;

public class Payment
{
    public enum RateTypes
    {
        [System.ComponentModel.Description("Variable")]
        Variable,

        [System.ComponentModel.Description("Fixed")]
        Fixed
    }

    public enum AmortizationTypes
    {
        [System.ComponentModel.Description("Interest Only")]
        InterestOnly,

        [System.ComponentModel.Description("Partially Amortized")]
        PartiallyAmortized,

        [System.ComponentModel.Description("Fully Amortized")]
        FullyAmortized,

        [System.ComponentModel.Description("Other")]
        Other
    }

    public enum RateIndexes
    {
        [System.ComponentModel.Description("SOFR_OIS")]
        SOFR_OIS,

        [System.ComponentModel.Description("PRIME")]
        PRIME,

        [System.ComponentModel.Description("CMT_1Y")]
        CMT_1Y,

        [System.ComponentModel.Description("CMT_3M")]
        CMT_3M,

        [System.ComponentModel.Description("EFFR")]
        EFFR,

        [System.ComponentModel.Description("AMERIBOR_ON")]
        AMERIBOR_ON
    }

    public enum IndexPaths
    {
        [System.ComponentModel.Description("Index Remains Constant")]
        IndexRemainsConstant,

        [System.ComponentModel.Description("Assumend +0.25% Annual Increase")]
        AssumendPercentAnnualIncrease
    }

    public enum Schedules
    {
        [System.ComponentModel.Description("Monthly")]
        Monthly,

        [System.ComponentModel.Description("Quarterly")]
        Quarterly,

        [System.ComponentModel.Description("Semi Annual")]
        SemiAnnual,

        [System.ComponentModel.Description("Yearly")]
        Yearly
    }

    public enum PrepaymentPremiums
    {
        [System.ComponentModel.Description("Penalty in Months")]
        PenaltyInMonths,

        [System.ComponentModel.Description("Yearly Step Down (linear)")]
        YearlyStepDownLinear,

        [System.ComponentModel.Description("Yearly Step Down (Non-linear)")]
        YearlyStepDownNonLinear,

        [System.ComponentModel.Description("Strict Penalty in Months/Percentage/Specific Amount (Loackout)")]
        StrictPenaltyInMonthsLoackout
    }

    public enum PerDiemInterestOptions
    {
    }

    public enum ReserveTypes
    {
        [System.ComponentModel.Description("None")]
        None,

        [System.ComponentModel.Description("Use Specific Dollar Amount")]
        UseSpecificDollarAmount,

        [System.ComponentModel.Description("Calculate Monthly Amount")]
        CalculateMonthlyAmount
    }

    public enum FeeTypes
    {
        [System.ComponentModel.Description("Origination")]
        Origination,

        [System.ComponentModel.Description("Underwriting")]
        Underwriting,

        [System.ComponentModel.Description("Processing")]
        Processing,

        [System.ComponentModel.Description("Document Preparation")]
        DocumentPreparation,

        [System.ComponentModel.Description("Commitment")]
        Commitment,


        [System.ComponentModel.Description("Administration")]
        Administration,


        [System.ComponentModel.Description("Wire")]
        Wire,


        [System.ComponentModel.Description("Inspection")]
        Inspection,


        [System.ComponentModel.Description("Appraisal")]
        Appraisal

    }

    public enum ExtensionFeeTypes
    {
        [System.ComponentModel.Description("Percent Of Loan Balance")]
        PercentOfLoanBalance,

        [System.ComponentModel.Description("Dollar Amount")]
        DollarAmount,

        [System.ComponentModel.Description("No Fee")]
        NoFee
    }

    public enum PrepayPenaltyTypes
    {
        [System.ComponentModel.Description("Stepdown")]
        Stepdown,

        [System.ComponentModel.Description("Guaranteed Interest")]
        GuaranteedInterest
    }

    public enum StepdownStructures
    {
        [System.ComponentModel.Description("6-5-4-3-2-1")]
        SixYear,

        [System.ComponentModel.Description("5-4-3-2-1")]
        FiveYear,

        [System.ComponentModel.Description("4-3-2-1")]
        FourYear,

        [System.ComponentModel.Description("3-2-1")]
        ThreeYear

      
    }

    public enum TerminationFeeTypes
    {
        [System.ComponentModel.Description("Dollar Amount")]
        DollarAmount,

        [System.ComponentModel.Description("Percentage Of Loan")]
        PercentageOfLoan,

        [System.ComponentModel.Description("Greater Of")]
        GreaterOf

    }

    

}