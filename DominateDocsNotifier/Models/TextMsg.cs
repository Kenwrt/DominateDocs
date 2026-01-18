using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsNotify.Models;

[BsonIgnoreExtraElements]
public class TextMsg
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? TextTemplateName { get; set; } = "PlainEmailTemplate.cshtml";

    public string? From { get; set; }

    public string? To { get; set; }

    public string? MessageBody { get; set; }
}