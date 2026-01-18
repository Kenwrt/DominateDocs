using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class SigningAuthorityMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(SigningAuthority sa)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{SigningAuthority.Name}", sa.Name ),
                new KeyValuePair<string, string>("{SigningAuthority.Title}", sa.Title),
                 new KeyValuePair<string, string>("{SigningAuthority.Email}", sa.Email),
                new KeyValuePair<string, string>("{SigningAuthority.PhoneNumber}", sa.PhoneNumber),
                new KeyValuePair<string, string>("{SigningAuthority.FullAddress}", sa.FullAddress),
                new KeyValuePair<string, string>("{SigningAuthority.StreeAddress}", sa.StreetAddress),
                new KeyValuePair<string, string>("{SigningAuthority.City}", sa.City),
                new KeyValuePair<string, string>("{SigningAuthority.State}", sa.State.ToString()),
                new KeyValuePair<string, string>("{SigningAuthority.ZipCode}", sa.ZipCode),
                new KeyValuePair<string, string>("{SigningAuthority.County}", sa.County),
                new KeyValuePair<string, string>("{SigningAuthority.Country}", sa.Country ),
                new KeyValuePair<string, string>("{SigningAuthority.SSN}", sa.SSN),

                //Produces Virtual Lists
                //SigningAuthority.List
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}