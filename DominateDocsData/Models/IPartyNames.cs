using DominateDocsData.Enums;
using System.Xml;

namespace DominateDocsData.Models;

public interface IPartyNames 
{
    new string EntityName { get; }
    new DominateDocsData.Enums.Entity.Types EntityType { get; }
    string StateOfOrganizationDescription { get; }
    string EntityStructureDescription { get; }
}