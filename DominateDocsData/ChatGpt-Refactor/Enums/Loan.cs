namespace LiquidDocsData.Enums;

public class Loan
{
    public enum Types
    {
        [System.ComponentModel.Description("Commercial")]
        Comercial,

        [System.ComponentModel.Description("DSCR")]
        DSCR,

        [System.ComponentModel.Description("RTL")]
        RTL,

        [System.ComponentModel.Description("Construction")]
        ConstructionOrRehab
    }

    public enum Status
    {
        [System.ComponentModel.Description("Pending")]
        Pending,

        [System.ComponentModel.Description("Approved")]
        Approved,

        [System.ComponentModel.Description("Cancelled")]
        Cancelled
    }
}