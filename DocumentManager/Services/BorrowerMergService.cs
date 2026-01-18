using DnsClient.Internal;
using DominateDocsData.Models;
using MailMerge;
using Microsoft.Extensions.Logging;

namespace DocumentManager.Services;

public class BorrowerMergService
{
    private ILogger<BorrowerMergService> logger;

    public BorrowerMergService(ILogger<BorrowerMergService> logger)
    {
        this.logger = logger;
    }

    public async Task<byte[]> Merge(Borrower borrower, byte[] documentBytes)
    {
        byte[] mergedDocument = null;
        string outputPath = "Templates/BorrowerMerged.docx";
        string templatePath = "Templates/BorrowerTemplate.docx";

        try
        {
            var merger = new MailMerger();
            var values = new Dictionary<string, string>
            {
                { "FirstName", borrower.EntityName },
                { "FirstName", borrower.ContactName },
                { "Email", borrower.ContactEmail }
            };

            merger.Merge(templatePath, values, outputPath);

            if (File.Exists(outputPath))
            {
                //read back into bytes
                mergedDocument = null; //should hold bytes
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return mergedDocument;
    }
}