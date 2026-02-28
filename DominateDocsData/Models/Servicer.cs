using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class Servicer : EntityBase
{
  
    //Entity Base Class Plus

    public bool SelfServiced { get; set; } = true;

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