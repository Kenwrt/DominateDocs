namespace LiquidDocsData.Enums;

public class LoanService
{
    public enum RemittanceSchedules
    {
        [System.ComponentModel.Description("Daily")]
        Daily,

        [System.ComponentModel.Description("Weekly")]
        Weekly,

        [System.ComponentModel.Description("BiWeekly")]
        BiWeekly,

        [System.ComponentModel.Description("Monthly")]
        Monthly
    }

    public enum PaymentMethods
    {
        [System.ComponentModel.Description("ACH")]
        ACH,

        [System.ComponentModel.Description("Wire")]
        Wire,

        [System.ComponentModel.Description("Check")]
        Check
    }

    public enum ReportingFrequencies
    {
        [System.ComponentModel.Description("Daily")]
        Daily,

        [System.ComponentModel.Description("Weekly")]
        Weekly,

        [System.ComponentModel.Description("Monthly")]
        Monthly,

        [System.ComponentModel.Description("Quarterly")]
        Quarterly
    }

    public enum DataDeliveryFormats
    {
        [System.ComponentModel.Description("CSV")]
        CSV,

        [System.ComponentModel.Description("XLSX")]
        XLSX,

        [System.ComponentModel.Description("JSON")]
        JSON,

        [System.ComponentModel.Description("XML")]
        XML,

        [System.ComponentModel.Description("PDF")]
        PDF
    }

    public enum EscrowTypes
    {
        [System.ComponentModel.Description("Taxes")]
        Taxes,

        [System.ComponentModel.Description("Insurance")]
        Insurance,

        [System.ComponentModel.Description("HOA")]
        HOA,

        [System.ComponentModel.Description("Other")]
        Other
    }

    public enum ServicingFeeBases
    {
        [System.ComponentModel.Description("Percent Of UPB")]
        PercentOfUPB,

        [System.ComponentModel.Description("Fixed PerLoan")]// percent of unpaid principal balance
        FixedPerLoan,

        [System.ComponentModel.Description("Tiered")]// flat dollar per loan per month
        Tiered             // tiered/other arrangement
    }

    public enum Capabilities
    {
        [System.ComponentModel.Description("Boarding")]
        Boarding,

        [System.ComponentModel.Description("DeBoarding")]
        DeBoarding,

        [System.ComponentModel.Description("Escrow Administration")]
        EscrowAdministration,

        [System.ComponentModel.Description("Force Placed Insurance")]
        ForcePlacedInsurance,

        [System.ComponentModel.Description("Collections")]
        Collections,

        [System.ComponentModel.Description("Payoffs")]
        Payoffs,

        [System.ComponentModel.Description("Partial Release Processing")]
        PartialReleaseProcessing,

        [System.ComponentModel.Description("ARM Adjustments")]
        ARMAdjustments,

        [System.ComponentModel.Description("Construction Draws")]
        ConstructionDraws,

        [System.ComponentModel.Description("ACH Processing")]
        ACHProcessing,

        [System.ComponentModel.Description("Lockbox Processing")]
        LockboxProcessing,

        [System.ComponentModel.Description("REO Disposition")]
        REODisposition
    }
}