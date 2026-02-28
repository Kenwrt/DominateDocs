namespace DominateDocsData.Enums;

public class Property
{
    public enum Types
    {
        [System.ComponentModel.Description("Single Family")]
        SingleFamily,

        [System.ComponentModel.Description("Multi-Family 2To4")]
        MultiFamily2To4,

        [System.ComponentModel.Description("Multi-Family 5-Plus")]
        MultiFamily5Plus,

        [System.ComponentModel.Description("Commercial")]
        Commercial,

        [System.ComponentModel.Description("Mixed Use")]
        MixedUse,

        [System.ComponentModel.Description("Land")]
        Land,

        [System.ComponentModel.Description("Industrial")]
        Industrial
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

    public enum OwnerTypes
    {
        [System.ComponentModel.Description("Borrower")]
        Borrower,

        [System.ComponentModel.Description("Guarantor")]
        Guarantor,

        [System.ComponentModel.Description("Third-Party Owner")]
        ThirdPartyOwner
        
    }


    public enum TitlePolicyTypes
    {
        [System.ComponentModel.Description("Single")]
        Single,

        [System.ComponentModel.Description("PerProperty")]
        PerProperty
    }

}