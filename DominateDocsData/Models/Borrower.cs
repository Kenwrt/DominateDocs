using DominateDocsData.Enums;
using LiquidDocsData.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class Borrower : EntityBase, IPartyNames
{
    // Nothing but the Base Class



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