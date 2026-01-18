using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class DocumentStore
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocId { get; set; }

    public Guid DocLibId { get; set; }

    public string Name { get; set; }

    public byte[] DocumentBytes { get; set; }

    public DateTime? UpdatedAt { get; set; }

}