using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentManager.Email;

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
       
    public enum Streams
    {
        [System.ComponentModel.Description("Broadcast")]
        Broadcast,

        [System.ComponentModel.Description("Transactional")]
        Transactional,

        [System.ComponentModel.Description("Outbound")]
        Outbound,

        [System.ComponentModel.Description("Inbound")]
        Inbound
    }

    public enum AttachmentSourceType
    {
        [System.ComponentModel.Description("Byte Array")]
        ByteArray,

        [System.ComponentModel.Description("File Path")]
        FilePath,

        [System.ComponentModel.Description("File Stream")]
        FileStream
    }

    public enum AttachmentOutput
    {
        [System.ComponentModel.Description("IndividualDocument")]
        IndividualDocument,
               
        [System.ComponentModel.Description("Zip File")]
        ZipFile
    }
}
