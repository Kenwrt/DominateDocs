using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class LenderMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(Lender x)
    {
        List<KeyValuePair<string, string>> values = new();

        string displayName = string.Empty;

        try
        {
            if (x.EntityType != DominateDocsData.Enums.Entity.Types.Individual)
            {
                displayName = $"{x.EntityName} a {x.StateOfIncorporationDescription} {x.EntityStructure}";
            }
            else
            {
                displayName = $"{x.EntityName} a {x.EntityType}";
            }

            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Lender.Name(s)}", displayName),
                new KeyValuePair<string, string>("{Lender.EIN}", x.EIN),
                new KeyValuePair<string, string>("{Lender.EntityType}", x.EntityType.ToString()),
                new KeyValuePair<string, string>("{Lender.EntityStructure}", x.EntityStructure.ToString()),
                new KeyValuePair<string, string>("{Lender.ContactsRole}", x.ContactsRole.ToString()),
                new KeyValuePair<string, string>("{Lender.StateOfIncorporation}", x.StateOfIncorporationDescription.ToString()),
                new KeyValuePair<string, string>("{Lender.ContactName}", x.ContactName),
                new KeyValuePair<string, string>("{Lender.ContactEmail}", x.ContactEmail),
                new KeyValuePair<string, string>("{Lender.SSN}", x.SSN),
                new KeyValuePair<string, string>("{Lender.ContactPhoneNumber}", x.ContactPhoneNumber),
                new KeyValuePair<string, string>("{Lender.FullAddress}", x.FullAddress),
                new KeyValuePair<string, string>("{Lender.StreeAddress}", x.StreetAddress),
                new KeyValuePair<string, string>("{Lender.City}", x.City),
                new KeyValuePair<string, string>("{Lender.State}", x.State.ToString()),
                new KeyValuePair<string, string>("{Lender.ZipCode}", x.ZipCode),
                new KeyValuePair<string, string>("{Lender.County}", x.County),
                new KeyValuePair<string, string>("{Lender.Country}", x.Country),
                new KeyValuePair<string, string>("{Lender.IsAliasNamesUsed}", x.IsAliasNamesUsed.ToString()),
                new KeyValuePair<string, string>("{Lender.LicenseNumber}", x.NmlsLicenseNumber.ToString()),
                new KeyValuePair<string, string>("{Lender.RegulatoryAuthority}", x.RegulatoryAuthority.ToString()),
                new KeyValuePair<string, string>("{Lender.PreferredStateVenue}", x.PreferredStateVenue.ToString()),
                new KeyValuePair<string, string>("{Lender.IsAForgeinNational}", x.IsAForgeinNational.ToString()),
                new KeyValuePair<string, string>("{Lender.IsLanuageTranslatorRequired}", x.IsLanuageTranslatorRequired.ToString()),
                new KeyValuePair<string, string>("{Lender.EntityOwners}", x.EntityOwnersFormatted),
                new KeyValuePair<string, string>("{Lender.AliasNames}",x.AliasNamesFormatted),
                new KeyValuePair<string, string>("{Lender.SigningAuthories}", x.SigningAuthoritiesFormatted),
                new KeyValuePair<string, string>("{Lender.SignatureLines}", x.SignatureLinesFormatted),
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}