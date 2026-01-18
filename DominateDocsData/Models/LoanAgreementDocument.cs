using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class LoanAgreementDocument
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string LoanNumber { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastUpdatedAtUtc { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Loan.Status Status { get; set; } = Loan.Status.Pending;

    public string StatusDescription => Status.GetDescription();

    [Required]
    public LoanAgreement LoanAgreement { get; set; } = new();

    public List<Borrower> Borrowers { get; set; } = new();
    public List<Guarantor> Guarantors { get; set; } = new();
    public List<Lender> Lenders { get; set; } = new();
    public List<Broker> Brokers { get; set; } = new();
    public List<PropertyRecord> Properties { get; set; } = new();
    public List<Lien> Liens { get; set; } = new();

    //   public List<DocumentSet> DocumentSets { get; set; } = new();

    public string? RawSnapshotJson { get; set; }
}