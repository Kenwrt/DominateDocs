using DominateDocsData.Enums;

namespace DominateDocsData.Models;

public interface IPartyNames
{
    string EntityName { get; }
    public Entity.Types EntityType { get; }
    string StateOfIncorporationDescription { get; }
    string EntityStructureDescription { get; }
}