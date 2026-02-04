using DocumentManager.Email.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DocumentManager.Email;

[BsonIgnoreExtraElements]
public class EmailMsg
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Subject { get; set; }

    public string? EmailTemplateName { get; set; } = "PlainEmailTemplate.cshtml";

    public EmailEnums.Templates PostMarkTemplate { get; set; }

    public int PostMarkTemplateId { get; set; }

    public string? Phone { get; set; }
    public string? Name { get; set; }
    public string? From { get; set; } = "no-reply@DominateDocs.law";
    public string? To { get; set; }
    public string? ReplyTo { get; set; }
    public string? MessageBody { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public EmailEnums.Providers ProviderType { get; set; } = EmailEnums.Providers.PostMark;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public EmailEnums.Streams StreamType { get; set; } = EmailEnums.Streams.Broadcast;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public EmailEnums.Templates TemplateType { get; set; } = EmailEnums.Templates.Other;

    public object TemplateModel { get; set; }

    // ✅ New
    public List<EmailAttachment> Attachments { get; set; } = new();
}