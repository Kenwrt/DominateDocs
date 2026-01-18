using LiquidDocsData.Enums;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class DocumentMerge
{
    [Key]
    [BsonIgnoreIfDefault]
    [BsonId]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Document Document { get; set; }

    public byte[] MergedDocumentBytes { get; set; }

    public LoanAgreement LoanAgreement { get; set; }

    public string? MergedDocumentPath { get; set; }

    public string HiddenTagName { get; set; } = "DominateDocsTag";

    public string HiddenTagValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? MergeCompleteAt { get; set; }

    public DocumentMergeState.Status Status { get; set; } = DocumentMergeState.Status.Pending;

}