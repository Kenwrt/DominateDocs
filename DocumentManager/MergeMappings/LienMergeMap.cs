using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class LienMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(Lien x)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Lien.Description}", x.Description.ToString()),
                new KeyValuePair<string, string>("{Lien.LienPosition}", x.LienPosition.ToString()),
                new KeyValuePair<string, string>("{Lien.List}", "")
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}