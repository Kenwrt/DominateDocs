namespace DocumentManager.RazorLite;

public interface ITemplateProcessor
{
    Task ProcessAsync(string inputPath, string outputPath, object model);
}