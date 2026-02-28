using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class SigningAuthority : EntityBase, IPartyNames
{
    // Entity Base Class Plus

    public string Name { get; set; }
    public string Title { get; set; }

}