using DominateDocsData.Models;

namespace DocumentManager.MergeMappings;

public static class PropertyRecordMergeMap
{
    public static List<KeyValuePair<string, string>> GetValues(PropertyRecord x)
    {
        List<KeyValuePair<string, string>> values = new();

        try
        {
            values = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("{Property.FullAddress}", x.FullAddress),
                new KeyValuePair<string, string>("{Property.StreetAddress}", x.StreetAddress),
                new KeyValuePair<string, string>("{Property.State}", x.State.ToString()),
                new KeyValuePair<string, string>("{Property.ZipCode}", x.ZipCode),
                new KeyValuePair<string, string>("{Property.City}", x.City),
                new KeyValuePair<string, string>("{Property.County}", x.County ),
                new KeyValuePair<string, string>("{Property.ParcelNumber}", x.ParcelNumber),
                new KeyValuePair<string, string>("{Property.Country}", x.Country),
                new KeyValuePair<string, string>("{Property.LegalDescription}", x.LegalDescription.ToString()),
                new KeyValuePair<string, string>("{Property.EstimatedValue}", x.EstimatedValue.ToString()),
                new KeyValuePair<string, string>("{Property.LastAppraisedValue}", x.LastAppraisedValue.ToString()),
                new KeyValuePair<string, string>("{Property.Type}", x.PropertyType.ToString()),
                new KeyValuePair<string, string>("{Property.SquareFootage}", x.SquareFootage.ToString()),
                new KeyValuePair<string, string>("{Property.YearBuilt}", x.YearBuilt.ToString()),
                new KeyValuePair<string, string>("{Property.LastAppraisalDate}", x.LastAppraisalDate.ToString()),
                new KeyValuePair<string, string>("{Property.IsOwnerOccupied}", x.IsOwnerOccupied.ToString()),
                new KeyValuePair<string, string>("{Property.PurchaseDate}", x.PurchaseDate.ToString()),
                new KeyValuePair<string, string>("{Property.PurchasePrice}", x.PurchasePrice.ToString()),
                new KeyValuePair<string, string>("{Property.MinimumReleasePrice}", x.MinimumReleasePrice.ToString()),
                new KeyValuePair<string, string>("{Property.PropertyTax}", x.PropertyTax.ToString()),
                new KeyValuePair<string, string>("{Property.Notes}", x.Notes.ToString()),
                new KeyValuePair<string, string>("{Property.TitleOrderNumber}", x.TitleOrderNumber.ToString()),
                new KeyValuePair<string, string>("{Property.TitleReportExceptionItemsToBeDeleted}", x.TitleReportExceptionItemsToBeDeleted.ToString()),
                new KeyValuePair<string, string>("{Property.AdditionalTitleEndorsmentRequested}", x.AdditionalTitleEndorsmentRequested.ToString()),
                new KeyValuePair<string, string>("{Property.TitleReportEffectiveDate}", x.TitleReportEffectiveDate.ToString()),
                new KeyValuePair<string, string>("{Property.IsReduceTitleCoverAmount}", x.IsReduceTitleCoverAmount.ToString()),
                new KeyValuePair<string, string>("{Property.TitleDocumentNumber}", x.TitleDocumentNumber.ToString()),
                new KeyValuePair<string, string>("{Property.EntityOwners}", x.EntityOwnersFormatted),
                new KeyValuePair<string, string>("{Property.SignatureLines}", x.SignatureLinesFormatted),
            };
        }
        catch (Exception ex)
        {
            // logger.LogError(ex.Message);
        }

        return values;
    }
}