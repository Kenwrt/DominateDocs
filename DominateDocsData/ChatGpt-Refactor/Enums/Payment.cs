namespace LiquidDocsData.Enums;

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

    public enum FeesPaidToOptions
    {
        [System.ComponentModel.Description("Deliver To Lender's Address")]
        DeliverToLendersAddress,

        [System.ComponentModel.Description("Deliver To Broker's Address")]
        DeliverToBrokersAddress,

        [System.ComponentModel.Description("Payment Deffered Until After Closing")]
        PaymentDefferedUntilAfterClosing,

        [System.ComponentModel.Description("Delivery Instruction To Be Provided")]
        DeliveryInstructionToBeProvided
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
}