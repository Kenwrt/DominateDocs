using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class BrokerMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(Broker x)
    {
        List<KeyValuePair<string, string>> values = new();

        string displayName = string.Empty;

        try
        {
            if (x.EntityType != DominateDocsData.Enums.Entity.Types.Individual)
            {
                displayName = $"{x.EntityName} a {x.StateOfIncorporation} {x.EntityStructure}";
            }
            else
            {
                displayName = $"{x.EntityName} a {x.EntityType}";
            }

            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Broker.Name(s)}", displayName),
                new KeyValuePair<string, string>("{Broker.EIN}", x.EIN),
                new KeyValuePair<string, string>("{Broker.EntityType}", x.EntityType.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.EntityStructure}", x.EntityStructure.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.ContactsRole}", x.ContactsRole.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.StateOfIncorporation}", x.StateOfIncorporation.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.ContactName}", x.ContactName ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.ContactEmail}", x.ContactEmail ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.ContactPhoneNumber}", x.ContactPhoneNumber ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.FullAddress}", x.FullAddress ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.StreeAddress}", x.StreetAddress ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.City}", x.City ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.State}", x.State.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.ZipCode}", x.ZipCode ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.County}", x.County ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.Country}", x.Country ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.SSN}", x.SSN ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.IsAliasNamesUsed}", x.IsAliasNamesUsed.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.IsAForgeinNational}", x.IsAForgeinNational.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.IsLanuageTranslatorRequired}", x.IsLanuageTranslatorRequired.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.EntityOwners}", x.EntityOwnersFormatted ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.AliasNames}",x.AliasNamesFormatted ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.SigningAuthories}", x.SigningAuthoritiesFormatted ?? string.Empty),
                new KeyValuePair<string, string>("{Broker.SignatureLines}", x.SignatureLinesFormatted ?? string.Empty),
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}