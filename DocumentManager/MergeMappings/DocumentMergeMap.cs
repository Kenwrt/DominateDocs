using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class DocumentMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(DocumentMerge documentMerge)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Document.Name}", documentMerge.Document.Name),
                new KeyValuePair<string, string>("{Document.TodaysDate}", System.DateTime.Now.ToString() )
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}