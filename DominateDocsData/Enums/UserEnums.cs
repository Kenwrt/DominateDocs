namespace DominateDocsData.Enums;

public class UserEnums
{
    public enum Roles
    {
        [System.ComponentModel.Description("Admin")]
        Admin,

        [System.ComponentModel.Description("User")]
        User,

        [System.ComponentModel.Description("DevAdmin")]
        DevAdmin,

        [System.ComponentModel.Description("Support")]
        Support
    }

    public enum Status
    {
        [System.ComponentModel.Description("Active")]
        Active,

        [System.ComponentModel.Description("Disabled")]
        Disables
    }

    public enum UserTypes
    {
        [System.ComponentModel.Description("Lender")]
        Lender,

        [System.ComponentModel.Description("Broker")]
        Broker,

        [System.ComponentModel.Description("Servicer")]
        Servicer,


        [System.ComponentModel.Description("Other")]
        Other
    }
}