public interface IRazorLiteService
{
    Task<MemoryStream> ProcessAsync(MemoryStream ms, object model);

    Task ProcessAsync(string inputPath, string outputPath, object model);
}