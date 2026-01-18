namespace LiquidDocsData.Enums;

public class Entity
{
    public enum ContactRoles
    {
        [System.ComponentModel.Description("Member")]
        Member,

        [System.ComponentModel.Description("President")]
        Presidcent,

        [System.ComponentModel.Description("Chief Executive Officer")]
        ChiefExecutiveOfficer,

        [System.ComponentModel.Description("Chief Financial Officer")]
        ChiefFinancialOfficer,

        [System.ComponentModel.Description("Chief Operating Officer")]
        ChiefOperatingOfficer,

        [System.ComponentModel.Description("Authorized Signer")]
        AuthorizedSigner,

        [System.ComponentModel.Description("Manager")]
        Manager,

        [System.ComponentModel.Description("Sole Member")]
        SoleMember,

        [System.ComponentModel.Description("Trustee")]
        Trustee
    }

    public enum Types
    {
        [System.ComponentModel.Description("Individual")]
        Individual,

        [System.ComponentModel.Description("Entity")]
        Entity
    }

    public enum Structures
    {
        [System.ComponentModel.Description("Limited Liability Company")]
        LLC,

        [System.ComponentModel.Description("Corporaton")]
        INC,

        [System.ComponentModel.Description("Non-Profit")]
        NonProfit,

        [System.ComponentModel.Description("Unincorporated Entity")]
        UnincorporatedEntity,

        [System.ComponentModel.Description("Trust")]
        Trust
    }
}