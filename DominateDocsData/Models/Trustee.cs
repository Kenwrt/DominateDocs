using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class Trustee : EntityBase, IPartyNames
{

    //Entity Base Class Plus

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