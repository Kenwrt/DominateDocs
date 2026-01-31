namespace DominateDocsData.Models;

using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

public class DocumentDelivery
{
    public Guid DocId { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    public int Copies { get; set; } = 1;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DocumentTypes.DelieveryTypes DelieveryTypes { get; set; } = DocumentTypes.DelieveryTypes.Email;

    // keeping your spelling to avoid breaking stored data / other code
    public string DeliveryLoaction { get; set; } = "";
}
