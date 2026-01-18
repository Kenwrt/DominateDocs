using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
[Table("LendingProducts")]
public class LendingProduct
{
    [Key]
    [Required]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; }

    [MaxLength(500)]
    public string Description { get; set; }

    public decimal InterestRate { get; set; }

    public decimal MinLoanAmount { get; set; }

    public decimal MaxLoanAmount { get; set; }

    public int TermInMonths { get; set; }

    public bool IsSecured { get; set; }
}