namespace DocumentManager.Email.Enums;

public class EmailEnums
{
    public enum Templates
    {
        [System.ComponentModel.Description("Welcome")]
        Welcome = 42096364,

        [System.ComponentModel.Description("Password Reset")]
        PasswordReset,

        [System.ComponentModel.Description("User Invitation")]
        UserInvitation,

        [System.ComponentModel.Description("Invoice")]
        Invoice,

        [System.ComponentModel.Description("ContactUs")]
        ContactUs = 42103437,

        [System.ComponentModel.Description("UserContact")]
        UserContact = 42103828,

        [System.ComponentModel.Description("MergeTest")]
        MergeTest = 42470074,

        [System.ComponentModel.Description("Other")]
        Other
    }

    public enum Providers
    {
        [System.ComponentModel.Description("SendGrid")]
        SendGrid,

        [System.ComponentModel.Description("PostMark")]
        PostMark,

        [System.ComponentModel.Description("FluentPostMark")]
        FluentPostMark,

        [System.ComponentModel.Description("Fluent")]
        Fluent
    }

    public enum Streams
    {
        [System.ComponentModel.Description("Broadcast")]
        Broadcast,

        [System.ComponentModel.Description("Transactional")]
        Transactional,

        [System.ComponentModel.Description("Inbound")]
        Inbound
    }
}