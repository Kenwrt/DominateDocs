using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class GuarantorMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(Guarantor x)
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
                new KeyValuePair<string, string>("{Guarantor.Name(s)}", displayName),
                new KeyValuePair<string, string>("{Guarantor.EIN}", x.EIN),
                new KeyValuePair<string, string>("{Guarantor.EntityType}", x.EntityType.ToString()),
                new KeyValuePair<string, string>("{Guarantor.EntityStructure}", x.EntityStructure.ToString()),
                new KeyValuePair<string, string>("{Guarantor.ContactsRole}", x.ContactsRole.ToString()),
                new KeyValuePair<string, string>("{Guarantor.StateOfIncorporation}", x.StateOfIncorporation.ToString()),
                new KeyValuePair<string, string>("{Guarantor.ContactName}", x.ContactName),
                new KeyValuePair<string, string>("{Guarantor.ContactEmail}", x.ContactEmail),
                new KeyValuePair<string, string>("{Guarantor.SSN}", x.SSN),
                new KeyValuePair<string, string>("{Guarantor.ContactPhoneNumber}", x.ContactPhoneNumber),
                new KeyValuePair<string, string>("{Guarantor.FullAddress}", x.FullAddress),
                new KeyValuePair<string, string>("{Guarantor.StreeAddress}", x.StreetAddress),
                new KeyValuePair<string, string>("{Guarantor.City}", x.City),
                new KeyValuePair<string, string>("{Guarantor.State}", x.State.ToString()),
                new KeyValuePair<string, string>("{Guarantor.ZipCode}", x.ZipCode),
                new KeyValuePair<string, string>("{Guarantor.County}", x.County),
                new KeyValuePair<string, string>("{Guarantor.Country}", x.Country),
                new KeyValuePair<string, string>("{Guarantor.IsAliasNamesUsed}", x.IsAliasNamesUsed.ToString()),
                new KeyValuePair<string, string>("{Guarantor.RelationshipToBorrower}", x.RelationshipToBorrower.ToString()),
                new KeyValuePair<string, string>("{Guarantor.IsAForgeinNational}", x.IsAForgeinNational.ToString()),
                new KeyValuePair<string, string>("{Guarantor.IsLanuageTranslatorRequired}", x.IsLanuageTranslatorRequired.ToString()),
                new KeyValuePair<string, string>("{Guarantor.Assets}", x.Assets.ToString()),
                new KeyValuePair<string, string>("{Guarantor.Liabilities}", x.Liabilities.ToString()),
                new KeyValuePair<string, string>("{Guarantor.EntityOwners}", x.EntityOwnersFormatted),
                new KeyValuePair<string, string>("{Guarantor.AliasNames}",x.AliasNamesFormatted),
                new KeyValuePair<string, string>("{Guarantor.SigningAuthories}", x.SigningAuthoritiesFormatted),
                new KeyValuePair<string, string>("{Guarantor.SignatureLines}", x.SignatureLinesFormatted),
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}