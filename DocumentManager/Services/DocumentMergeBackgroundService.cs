using DocumentFormat.OpenXml.Packaging;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenXmlPowerTools;
using System.Text;

namespace DocumentManager.Services;

public class DocumentMergeBackgroundService : BackgroundService, IDocumentMergeBackgroundService
{
    private readonly ILogger<DocumentMergeBackgroundService> logger;

    private readonly IOptions<DocumentManagerConfigOptions> options;

    private IWebHostEnvironment webEnv;

    private IRazorLiteService razorLiteService;

    private IWordServices wordServices;

    private readonly IDocumentManagerState docState;

    private SemaphoreSlim docSemaphoreSlim = null;

    public event EventHandler<DocumentMerge>? OnDocumentMergeCompletedEvent;

    public event EventHandler<DocumentMerge>? OnDocumentMergeErrorEvent;

    public DocumentMergeBackgroundService(ILogger<DocumentMergeBackgroundService> logger, IOptions<DocumentManagerConfigOptions> options, IDocumentManagerState docState, IWebHostEnvironment webEnv, IRazorLiteService razorLiteService, IWordServices wordServices)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.webEnv = webEnv;
        this.razorLiteService = razorLiteService;
        this.wordServices = wordServices;

        docSemaphoreSlim = new(options.Value.MaxDocumentMergeThreads);

        docState.IsRunBackgroundDocumentMergeServiceChanged += OnIsRunDocumentBackgroundServiceChanged;
    }

    private async Task MergeDocumentAsync(DocumentMerge documentMerge, CancellationToken stoppingToken)
    {
        try
        {
            await docSemaphoreSlim.WaitAsync();

            docState.DocumentList.TryAdd(documentMerge.Id, documentMerge);

            byte[] mergedDocBytes;

            using var ms = new MemoryStream(capacity: documentMerge.Document.TemplateDocumentBytes.Length + 4096); // give it some headroom

            ms.Write(documentMerge.Document.TemplateDocumentBytes, 0, documentMerge.Document.TemplateDocumentBytes.Length);

            ms.Position = 0;

            //Process the document with RazorLite
            MemoryStream msResult = await razorLiteService.ProcessAsync(ms, documentMerge.LoanAgreement);

            if (msResult is not null)
            {
                if (documentMerge.Document.OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.DOCX)
                {
                    //no need to convert already in word format

                    msResult.Position = 0;
                    documentMerge.Document.Name = $"{documentMerge.Document.Name}.docm";
                }
                else
                {
                    MemoryStream pdfStream = await wordServices.ConvertWordToPdfAsync(msResult);

                    pdfStream.Position = 0;

                    documentMerge.MergedDocumentBytes = pdfStream.ToArray();
                    documentMerge.Document.Name = $"{documentMerge.Document.Name}.pdf";

                    //Convert to PDF

                    //var pdfPath = Path.Combine(dir, $"{DisplayHelper.CapitalizeWordsNoSpaces(doc.Name)}-TestMerge.pdf");
                    //await File.WriteAllBytesAsync(pdfPath, pdfStream.ToArray());
                }

                documentMerge.Status = DocumentMergeState.Status.Complete;
            }
            else
            {
                documentMerge.MergedDocumentBytes = null;
                documentMerge.Status = DocumentMergeState.Status.Error;
            }

            OnDocumentMergeCompletedEvent?.Invoke(this, documentMerge);

            docState.StateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error processing document {documentMerge.Id}: {ex.Message}");

            OnDocumentMergeErrorEvent?.Invoke(this, documentMerge);
            docState.DocumentList.TryRemove(documentMerge.Id, out documentMerge);
            throw;
        }
        finally
        {
            docSemaphoreSlim?.Release();
        }
    }

    private static string GetDocumentText(WordprocessingDocument doc)
    {
        using var reader = new StreamReader(doc.MainDocumentPart.GetStream());
        return reader.ReadToEnd();
    }

    private static void SetDocumentText(WordprocessingDocument doc, string text)
    {
        using var writer = new StreamWriter(doc.MainDocumentPart.GetStream(FileMode.Create));
        writer.Write(text);
    }

    private async Task<string> GetHtmlFromWordDoc(string wordFilePath)
    {
        System.Xml.Linq.XElement html = null;
        string htmlString = string.Empty;
        string htmlOutputPath = System.IO.Path.Combine("Content", "LenderClosingInstructions.mht");

        try
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(wordFilePath, false))
            {
                OpenXmlPowerTools.HtmlConverterSettings settings = new OpenXmlPowerTools.HtmlConverterSettings()
                {
                    PageTitle = "Converted from Word"
                };

                html = OpenXmlPowerTools.HtmlConverter.ConvertToHtml(wordDoc, settings);

                // Save the HTML output
                File.WriteAllText(htmlOutputPath, html.ToStringNewLineOnAttributes(), Encoding.UTF8);

                Console.WriteLine($"HTML file generated: {htmlOutputPath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error during conversion: " + ex.Message);
        }

        return html.ToStringNewLineOnAttributes();
    }

    // Background Service Maintenance Area
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (docState.IsRunBackgroundDocumentMergeService)
                    {
                        List<Task> docTasks = new();

                        while (docState.DocumentProcessingQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Document Processing Queued Item Found Job at: {time} Processong Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                DocumentMerge documentMerge = null;

                                docState.DocumentProcessingQueue.TryDequeue(out documentMerge);

                                if (documentMerge is not null) docTasks.Add(Task.Run(async () => await MergeDocumentAsync(documentMerge, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (docTasks.Count > 0) await Task.WhenAll(docTasks);

                        if (docState.DocumentProcessingQueue.Count == 0) logger.LogDebug("Document Merge Processing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);
                    }
                    else
                    {
                        logger.LogDebug("Document Merge Processing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Docment Merge Processing Background Service NOT Active");
                }

                docState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await base.StartAsync(cancellationToken);

        await DoRecoveryAsync();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        await DoCleanupAsync();
    }

    private async Task DoRecoveryAsync()
    {
        logger.LogDebug($"Document Merge Processing Background Service Recovering Tranactions");
    }

    private async Task DoCleanupAsync()
    {
        logger.LogDebug($"Document Merge Processing Background Service Performing Cleanup tasks");
    }

    private void OnIsRunDocumentBackgroundServiceChanged(object? sender, bool e)
    {
        if (docState.IsRunBackgroundDocumentMergeService)
        {
            DoRecoveryAsync();
        }
        else
        {
            DoCleanupAsync();
        }
    }
}

//public class TextReplacement
//{
//    public string Search { get; set; }
//    public string Replace { get; set; }
//}