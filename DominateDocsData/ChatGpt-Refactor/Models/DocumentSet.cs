using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
[Table("DocumentSets")]
public class DocumentSet
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid LoanId { get; set; }

    public Guid UserId { get; set; }

    public string Name { get; set; }

    public String Description { get; set; }

    public List<Document> Documents { get; set; } = new();

    public bool IsActive { get; set; } = true;
}