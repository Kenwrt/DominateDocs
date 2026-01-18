using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models.Storage;

[BsonIgnoreExtraElements]
public class BlobRef
{
    /// <summary>Azure Storage container name (e.g. "doc-templates")</summary>
    [Required]
    public string Container { get; set; } = default!;

    /// <summary>Blob name/key inside the container (you control this; often includes Guids)</summary>
    [Required]
    public string BlobName { get; set; } = default!;

    /// <summary>Optional: ETag for caching/concurrency (Azure returns this on upload)</summary>
    public string? ETag { get; set; }

    /// <summary>Optional: VersionId if Blob Versioning is enabled</summary>
    public string? VersionId { get; set; }

    /// <summary>Optional: Size in bytes</summary>
    public long? SizeBytes { get; set; }

    /// <summary>Optional: Hash (e.g., SHA256 hex) for integrity checks</summary>
    public string? Hash { get; set; }

    public bool IsEmpty => string.IsNullOrWhiteSpace(Container) || string.IsNullOrWhiteSpace(BlobName);
}
