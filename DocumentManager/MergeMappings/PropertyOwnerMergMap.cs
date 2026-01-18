using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class PropertyOwnerMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(PropertyOwner x)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{PropertyOwner.EntityName}", x.EntityName),
                new KeyValuePair<string, string>("{PropertyOwner.ContactName}", x.ContactName),
                new KeyValuePair<string, string>("{PropertyOwner.ContactEmail}", x.ContactEmail),
                new KeyValuePair<string, string>("{PropertyOwner.ContactPhoneNumber}", x.ContactPhoneNumber),
                new KeyValuePair<string, string>("{PropertyOwner.FullAddress}", x.FullAddress),
                new KeyValuePair<string, string>("{PropertyOwner.StreeAddress}", x.StreetAddress),
                new KeyValuePair<string, string>("{PropertyOwner.City}", x.City),
                new KeyValuePair<string, string>("{PropertyOwner.State}", x.State.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.ZipCode}", x.ZipCode),
                new KeyValuePair<string, string>("{PropertyOwner.County}", x.County),
                new KeyValuePair<string, string>("{PropertyOwner.Country}", x.Country),

                new KeyValuePair<string, string>("{PropertyOwner.SSN}", x.SSN),
                new KeyValuePair<string, string>("{PropertyOwner.EIN}", x.EIN.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.IsNotificationAddress}", x.IsNotificationAddress.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.PercentageOfOwnership}", x.PercentageOfOwnership.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.IsAForgeinNational}", x.IsAForgeinNational.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.IsLanuageTranslatorRequired}", x.IsLanuageTranslatorRequired.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.IsJointOwnership}", x.IsJointOwnership.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.IsAliasNamesUsed}", x.IsAliasNamesUsed.ToString()),
                new KeyValuePair<string, string>("{PropertyOwner.EntityOwners}", x.EntityOwnersFormatted),
                new KeyValuePair<string, string>("{PropertyOwner.AliasNames}",x.AliasNamesFormatted),
                new KeyValuePair<string, string>("{PropertyOwner.SigningAuthories}", x.SigningAuthoritiesFormatted),
                new KeyValuePair<string, string>("{PropertyOwner.SignatureLines}", x.SignatureLinesFormatted),
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}