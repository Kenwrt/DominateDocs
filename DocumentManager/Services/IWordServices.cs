namespace DocumentManager.Services;

public interface IWordServices
{
    void CheckForDuplicateZipEntries(byte[] docxBytes);

    Task<MemoryStream> ConvertWordToPdfAsync(MemoryStream wordStream, CancellationToken cancellationToken = default);

    string DetectFormatBySignature(byte[] b);

    MemoryStream InsertHiddenTag(Stream originalDocStream, Dictionary<string, string> tagList);

    MemoryStream InsertHiddenTag(Stream originalDocStream, string tagName, string tagValue);

    Task<MemoryStream> InsertHiddenTagAsync(Stream originalDocStream, Dictionary<string, string> tagList);

    Task<MemoryStream> InsertHiddenTagAsync(Stream originalDocStream, string tagName, string tagValue);

    byte[] RemoveBadCompatSetting(byte[] docxBytes);

    byte[] ReplaceTokens(byte[] docxBytes, IDictionary<string, string> map);

    Task<string> RetrieveHiddenTagsAsync(Stream originalDocStream, string tagName);

    List<WordServices.DocError> ValidateDocx(byte[] bytes);
}