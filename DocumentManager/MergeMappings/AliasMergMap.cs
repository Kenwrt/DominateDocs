using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class AliasMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(AkaName x)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{AkaName.Name}", x.Name.ToString()),
                new KeyValuePair<string, string>("{AkaName.AlsoKnownAs}", x.AlsoKnownAs.ToString())
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}