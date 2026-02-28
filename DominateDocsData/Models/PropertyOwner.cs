using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class PropertyOwner : EntityBase, IPartyNames, IPropertyAddresses
{
    //Entity Base Class Plus    

    public bool IsPowerOfAttorneyIssued { get; set; } = false;
    
    public bool IsNotificationAddress { get; set; }

    public int PercentageOfOwnership { get; set; }

    public bool IsJointOwnership { get; set; }

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