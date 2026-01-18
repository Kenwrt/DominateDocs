using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class EntityOwnerMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(EntityOwner x)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{EntityOwner.Name}", x.Name ),
                new KeyValuePair<string, string>("{EntityOwner.EntityRole}", x.EntityRole.ToString()),
                 new KeyValuePair<string, string>("{EntityOwner.Email}", x.Email),
                new KeyValuePair<string, string>("{EntityOwner.PhoneNumber}", x.PhoneNumber),
                new KeyValuePair<string, string>("{EntityOwner.FullAddress}", x.FullAddress),
                new KeyValuePair<string, string>("{EntityOwner.StreeAddress}", x.StreetAddress),
                new KeyValuePair<string, string>("{EntityOwner.City}", x.City),
                new KeyValuePair<string, string>("{EntityOwner.State}", x.State.ToString()),
                new KeyValuePair<string, string>("{EntityOwner.ZipCode}", x.ZipCode),
                new KeyValuePair<string, string>("{EntityOwner.County}", x.County),
                new KeyValuePair<string, string>("{EntityOwner.Country}", x.Country ),
                new KeyValuePair<string, string>("{EntityOwner.List}", "" )
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}