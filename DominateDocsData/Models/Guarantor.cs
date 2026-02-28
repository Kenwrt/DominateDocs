using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class Guarantor : EntityBase, IPartyNames
{
   //Base Class Plus

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.GuarantorPosition.Types GuarantorType { get; set; } = DominateDocsData.Enums.GuarantorPosition.Types.FullRecourse;
       
    public string? RelationshipToBorrower { get; set; }  // e.g. Parent, Business Partner

    public decimal Assets { get; set; }

    public decimal Liabilities { get; set; }

    public void EnforceTypeIntegrity()
    {
        switch (EntityType)
        {
            case Entity.Types.Individual:
                EntityStructure = Entity.Structures.None;
                Trustees?.Clear();
                break;

            case Entity.Types.Trust:
                EntityStructure = Entity.Structures.None;
                EntityOwners?.Clear();
                break;

            case Entity.Types.Entity:
                Trustees?.Clear();
                break;
        }
    }


}