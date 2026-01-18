using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
[Table("DocumentTags")]
public record DocumentTag
{
    public Guid DocumentId { get; init; }

    public string DocumentName { get; init; }

    public Guid LibraryId { get; init; }

    public Guid DocumentCollectionId { get; init; }

    public string BaseTemplateId { get; init; }

    public Guid DocumentStoreId { get; init; }
}