namespace DominateDocsData.Models;

using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

[BsonIgnoreExtraElements]
public class Contractor :EntityBase
{
    //Entity Base Class Plus

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public ConstructionContractors.Roles ContractorRole { get; set; } = ConstructionContractors.Roles.GeneralContractor;

    public bool IsDutchInterest { get; set; } = false;
    public bool DutchInterest { get; set; }
    public decimal HoldbackAmount { get; set; } = 0.00M;
    public string HoldbackPaidTo { get; set; } = string.Empty;
    public DateOnly HoldbackPaymentDue { get; set; }   // combobox value

}