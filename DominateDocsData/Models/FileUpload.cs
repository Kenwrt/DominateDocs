using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
[Table("FileUploads")]
public class FileUpload
{
    [BsonRepresentation(BsonType.String)]
    public Guid RequestUploadId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid FirmId { get; set; }

    [BsonRepresentation(BsonType.String)]
    public Guid DocumentSetId { get; set; }

    public string FileName { get; set; }

    public string FilePath { get; set; }

    public byte[] FileContent { get; set; }

    public string TempName { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(MongoDB.Bson.BsonType.String)]
    public string Status { get; set; }
}