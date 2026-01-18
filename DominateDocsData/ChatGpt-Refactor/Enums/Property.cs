namespace LiquidDocsData.Enums;

public class Property
{
    public enum Types
    {
        [System.ComponentModel.Description("SingleFamily")]
        SingleFamily,

        [System.ComponentModel.Description("Multi-Family")]
        MultiFamily,

        [System.ComponentModel.Description("Condo")]
        Condo,

        [System.ComponentModel.Description("Industrial")]
        Industrial,

        [System.ComponentModel.Description("Commercial")]
        Commercial,

        [System.ComponentModel.Description("Land")]
        Land,

        [System.ComponentModel.Description("Other")]
        Other
    }

    public enum Roles
    {
        [System.ComponentModel.Description("Security")]
        Security,

        [System.ComponentModel.Description("Subject Property")]
        SubjectProperty,

        [System.ComponentModel.Description("Borrower Primary Residence")]
        BorrowerPrimaryResidence,

        [System.ComponentModel.Description("Third Party Security")]
        ThirdPartySecurity,

        [System.ComponentModel.Description("Other")]
        Other
    }
}