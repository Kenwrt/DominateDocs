namespace DocumentManager.Email.Enums;

public class EmailAttachmentEnums
{
    public enum Type
    {
        [System.ComponentModel.Description("Byte Array")]
        ByteArray,

        [System.ComponentModel.Description("File Path")]
        FilePath,

        [System.ComponentModel.Description("File Stream")]
        FileStream
    }

    public enum OutputType
    {
        [System.ComponentModel.Description("Word Document")]
        WordDoc,

        [System.ComponentModel.Description("PDF")]
        PDF,

        [System.ComponentModel.Description("Zip File")]
        ZipFile
    }
}