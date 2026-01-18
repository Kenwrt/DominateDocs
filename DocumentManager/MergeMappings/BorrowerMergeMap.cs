using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class BorrowerMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(Borrower x)
    {
        string displayName = string.Empty;
        string signatureLine = string.Empty;

        List<KeyValuePair<string, string>> values = new();

        try
        {
            if (x.EntityType != DominateDocsData.Enums.Entity.Types.Individual)
            {
                displayName = $"{x.EntityName} a {x.StateOfIncorporation} {x.EntityStructure}";
                signatureLine = $"_______________________________________\n\r {x.EntityName} a Indiviual";
            }
            else
            {
                displayName = $"{x.EntityName} a {x.EntityType}";
                signatureLine = $"_____________________________________\n\r {x.EntityName} by {x.ContactName} as {x.ContactsRole}";
            }

            if (!String.IsNullOrEmpty(x.EntityName))
            {
            }

            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Borrower.Name(s)}", displayName ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.EntityName}", displayName ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.EIN}", x.EIN?.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.EntityType}", x.EntityType.ToString()),
                new KeyValuePair<string, string>("{Borrower.EntityStructure}", x.EntityStructure.ToString()),
                new KeyValuePair<string, string>("{Borrower.ContactsRole}", x.ContactsRole.ToString()),
                new KeyValuePair<string, string>("{Borrower.StateOfIncorporation}", x.StateOfIncorporation.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.ContactName}", x.ContactName ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.ContactEmail}", x.ContactEmail ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.ContactPhoneNumber}", x.ContactPhoneNumber ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.FullAddress}", x.FullAddress ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.StreeAddress}", x.StreetAddress ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.City}", x.City ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.State}", x.State?.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.ZipCode}", x.ZipCode ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.County}", x.County ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.Country}", x.Country ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.SSN}", x.SSN ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.IsAliasNamesUsed}", x.IsAliasNamesUsed.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.IsAForgeinNational}", x.IsAForgeinNational.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.IsLanuageTranslatorRequired}", x.IsLanuageTranslatorRequired.ToString() ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.EntityOwners}", x.EntityOwnersFormatted ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.AliasNames}",x.AliasNamesFormatted ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.SigningAuthorities}", x.SigningAuthoritiesFormatted ?? string.Empty),
                new KeyValuePair<string, string>("{Borrower.SignatureLines}", signatureLine ?? string.Empty),
};
        }
        catch (Exception ex)
        {
            string ken = ex.Message;
        }

        return values;
    }
}