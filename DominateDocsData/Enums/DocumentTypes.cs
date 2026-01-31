namespace DominateDocsData.Enums;

public class DocumentTypes
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

        [System.ComponentModel.Description("Mortgage")]
        Mortgage,

        [System.ComponentModel.Description("Standard")]
        Standard,

        [System.ComponentModel.Description("Deed of Trust")]
        Deed
    }

    public enum OutputTypes
    {
        [System.ComponentModel.Description("DOCX and PDF")]
        DOCXPDF,

        [System.ComponentModel.Description("DOCX")]
        DOCX,

        [System.ComponentModel.Description("PDF")]
        PDF
    }

    public enum GenerateMultipleFor
    {
        [System.ComponentModel.Description("Lenders")]
        Lenders,

        [System.ComponentModel.Description("Borrowers")]
        Borrowers,

        [System.ComponentModel.Description("Guarantors")]
        Guarantors,

        [System.ComponentModel.Description("Trustees")]
        Trustees,

        [System.ComponentModel.Description("Properties")]
        Properties,

        [System.ComponentModel.Description("Assignees")]
        Assignees,

        [System.ComponentModel.Description("Brokers")]
        Brokers
    }

    public enum TestTypes
    {
        [System.ComponentModel.Description("Document")]
        Document,

        [System.ComponentModel.Description("Loan Types")]
        DocumentSet
    }

    public enum DelieveryTypes
    {
        [System.ComponentModel.Description("Email")]
        Email,

        [System.ComponentModel.Description("Cloud")]
        Cloud,

        [System.ComponentModel.Description("FTP")]
        FTP,

        [System.ComponentModel.Description("FileSystem")]
        FileSystem
    }
}