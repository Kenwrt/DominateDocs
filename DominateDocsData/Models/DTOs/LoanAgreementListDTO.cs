using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.DTOs;

public class LoanAgreementListDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? ReferenceName { get; set; }

    public string LoanNumber { get; set; }

    public decimal PrincipalAmount { get; set; } = 0;

    public decimal InterestRate { get; set; } = 0;
       
    public int TermInMonths { get; set; } = 0;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Loan.Status Status { get; set; } = Loan.Status.Pending;


    public DateTime? OriginationDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public LoanAgreementListDTO(Guid Id, string? ReferenceName, string LoanNumber, decimal PrincipalAmount, decimal InterestRate, int TermInMonths, Loan.Status Status, DateTime? OriginationDate, DateTime? MaturityDate)
    {
        this.Id = Id;
        this.ReferenceName = ReferenceName;
        this.LoanNumber = LoanNumber;
        this.PrincipalAmount = PrincipalAmount;
        this.InterestRate = InterestRate;
        this.TermInMonths = TermInMonths;
        this.Status = Status;
        this.OriginationDate = OriginationDate;
        this.MaturityDate = MaturityDate;

    }
}
