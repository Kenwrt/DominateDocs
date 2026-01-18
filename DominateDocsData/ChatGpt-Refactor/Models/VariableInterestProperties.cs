using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;
public class VariableInterestProperties
{
   
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.RateIndexes RateIndex { get; set; }

   
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public  Payment.Schedules AdjustmentInterval { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.IndexPaths AssumedIndexPath { get; set; }

  
    public decimal StartIndexPercent { get; set; } = 5.30m; // today's SOFR

    public DateOnly FirstPaymentDate { get; set; } 

    public decimal BasisPointsPerReset { get; set; } = 25m;     // assume +0.25% each reset

    public List<decimal> ExplicitResetCurvePercents { get; set; } = new() { 5.30m, 5.55m, 5.80m, 5.75m, 5.60m };

    public List<RateChange> RateChangeList { get; set; } = new();

    public PaymentSchedule PaymentSchedule { get; set; } = new();

    public decimal? IndexSpotPercentAtClosing { get; set; }

    public int AdjustmentIntervalMonths { get; set; }

    // Store the projected curve you used (JSON)
    public string? ProjectedIndexCurveJson { get; set; }
}
