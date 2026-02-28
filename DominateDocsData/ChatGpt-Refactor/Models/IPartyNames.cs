using LiquidDocsData.Enums;

namespace LiquidDocsData.Models;

public interface IPartyNames
{
    string EntityName { get; }
    public Entity.Types EntityType { get; }
    string StateOfOrganizationDescription { get; }
    string EntityStructureDescription { get; }
}