namespace LiquidDocsData.Enums;

public class CreditCard
{
    public enum Types
    {
        [System.ComponentModel.Description("MasterCard")]
        MasterCard,

        [System.ComponentModel.Description("Visa")]
        Visa,

        [System.ComponentModel.Description("AMEX")]
        AMEX
    }
}