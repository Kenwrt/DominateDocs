namespace DominateDocsData.Enums;

public class QuickLoanSettings
{
    
    public enum FormSections
    {
        [System.ComponentModel.Description("Borrower")]
        Borrower,

        [System.ComponentModel.Description("Lender")]
        Lender,

        [System.ComponentModel.Description("Broker")]
        Broker,

        [System.ComponentModel.Description("Guarantor")]
        Guarantor,

        [System.ComponentModel.Description("Property")]
        Property,


        [System.ComponentModel.Description("Servicer")]
        Servicer
    }


}