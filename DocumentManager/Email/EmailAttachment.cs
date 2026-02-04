using DocumentManager.Email.Enums;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DocumentManager.Email;

[BsonIgnoreExtraElements]
public class EmailAttachment
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Logical / download file name (e.g. "LoanAgreement.pdf").</summary>
    [Required]
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME type, e.g. "application/pdf".</summary>
    public string? ContentType { get; set; }

    /// <summary>How the attachment data was supplied.</summary>
    public EmailAttachmentEnums.Type SourceType { get; set; }

    /// <summary>How the attachment should be interpreted by the business logic.</summary>
    public EmailAttachmentEnums.OutputType OutputType { get; set; }

    /// <summary>Optional file path for FilePath attachments.</summary>
    public string? FilePath { get; set; }

    /// <summary>Optional raw data for ByteArray attachments.</summary>
    public byte[]? Data { get; set; }

    /// <summary>
    /// Optional stream for FileStream attachments. Not persisted to Mongo.
    /// </summary>
    [BsonIgnore]
    public Stream? Stream { get; set; }

    /// <summary>Inline / embedded (Postmark logo-style) vs normal attachment.</summary>
    public bool IsInline { get; set; }

    /// <summary>ContentId for inline images (Postmark).</summary>
    public string? ContentId { get; set; }

    public Stream ToStream()
    {
        return SourceType switch
        {
            EmailAttachmentEnums.Type.FilePath when !string.IsNullOrWhiteSpace(FilePath)
                => File.OpenRead(FilePath),

            EmailAttachmentEnums.Type.ByteArray when Data is not null
                => new MemoryStream(Data, writable: false),

            EmailAttachmentEnums.Type.FileStream when Stream is not null
                => Stream,

            _ => throw new InvalidOperationException("Attachment does not contain usable data.")
        };
    }

    public byte[] ToBytes()
    {
        return SourceType switch
        {
            EmailAttachmentEnums.Type.ByteArray when Data is not null
                => Data,

            EmailAttachmentEnums.Type.FilePath when !string.IsNullOrWhiteSpace(FilePath)
                => File.ReadAllBytes(FilePath),

            EmailAttachmentEnums.Type.FileStream when Stream is not null
                => StreamToBytes(Stream),

            _ => throw new InvalidOperationException("Attachment does not contain usable data.")
        };
    }

    private static byte[] StreamToBytes(Stream s)
    {
        if (s is MemoryStream ms)
            return ms.ToArray();

        using var buffer = new MemoryStream();
        s.CopyTo(buffer);
        return buffer.ToArray();
    }
}