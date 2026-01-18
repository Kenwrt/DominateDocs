namespace LiquidDocsData.Enums;

public class Document
{
    public enum Types
    {
        [System.ComponentModel.Description("Security")]
        Security,

        [System.ComponentModel.Description("UCC")]
        UCC,

        [System.ComponentModel.Description("Note")]
        Note,

        [System.ComponentModel.Description("LoanAgreement")]
        LoanAgreement,

        [System.ComponentModel.Description("Mortage")]
        Mortage,

        [System.ComponentModel.Description("Deed of Trust")]
        Deed
    }

    
}