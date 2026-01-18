//using DocumentFormat.OpenXml.Wordprocessing;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentManager.State;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsNotify.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text;

namespace DocumentManager.Services;

public partial class LoanApplicationBackgroundService : BackgroundService
{
    private readonly ILogger<LoanApplicationBackgroundService> logger;

    private readonly IOptions<DocumentManagerConfigOptions> options;

    private readonly IDocumentManagerState docState;

    private readonly IWordServices wordServices;

    // private readonly  notificationService;

    private readonly IRazorLiteService razorLiteService;

    private readonly IDocumentMergeBackgroundService documentMergeBackgroundService;

    private readonly ConcurrentDictionary<Guid, MergeTracker> mergeTrackers = new();

    private SemaphoreSlim loanSemaphoreSlim = null;

    public event EventHandler<LoanAgreement> OnLoanCompletedEvent;

    public event EventHandler<LoanAgreement> OnLoanErrorEvent;

    private IWebHostEnvironment env;

    public LoanApplicationBackgroundService(ILogger<LoanApplicationBackgroundService> logger, IOptions<DocumentManagerConfigOptions> options, IDocumentManagerState docState, IWordServices wordServices, IRazorLiteService razorLiteService, IWebHostEnvironment env, IDocumentMergeBackgroundService documentMergeBackgroundService)
    {
        this.logger = logger;
        this.options = options;
        this.docState = docState;
        this.wordServices = wordServices;
        this.razorLiteService = razorLiteService;
        this.env = env;
        this.documentMergeBackgroundService = documentMergeBackgroundService;

        loanSemaphoreSlim = new(options.Value.MaxLoanApplicationThreads);

        docState.IsRunBackgroundLoanApplicationServiceChanged += OnIsRunBackgroundLoanServiceChanged;

        documentMergeBackgroundService.OnDocumentMergeCompletedEvent += OnDocumentMergeCompleted;
        documentMergeBackgroundService.OnDocumentMergeErrorEvent += OnDocumentMergeError;
    }

    private async Task ProcessLoanAsync(LoanAgreement loan, CancellationToken stoppingToken)
    {
        LoanAgreement loanState = null;

        try
        {
            await loanSemaphoreSlim.WaitAsync();

            docState.LoanList.TryAdd(loan.Id, loan);

            docState.StateHasChanged();

            //Prepare Loan Agreement Names and Signature Lines
            StringBuilder lenderNames = new StringBuilder();
            StringBuilder borrowerNames = new StringBuilder();
            StringBuilder brokerNames = new StringBuilder();
            StringBuilder propertyAddresses = new StringBuilder();

            loan.LenderNames = await BuildLenderNamesAsync(loan.Lenders);

            loan.BorrowerNames = await BuildPartyNamesAsync<DominateDocsData.Models.Borrower>(loan.Borrowers);

            loan.BrokerNames = await BuildPartyNamesAsync<DominateDocsData.Models.Broker>(loan.Brokers);

            loan.GuarantorNames = await BuildPartyNamesAsync<DominateDocsData.Models.Guarantor>(loan.Guarantors);

            loan.PropertyAddresses = await BuildPropertyAddressAsync<DominateDocsData.Models.PropertyRecord>(loan.Properties);

            //Get a list of Documents to Process based upon the Rules engine

            //Old Section
            //var data = new Dictionary<string, object?>
            //{
            //    ["@State_Generated"] = "California",
            //    ["@Loan_Features"] = new[] { "ACH Payments" }
            //};

            //var docs = DocumentOutputEvaluator.BuildFinalDocumentSet(loanType, data);

            // Create output folder
            //  var documentSetPath = Path.Combine(env.WebRootPath, "MergedDocuments", $"{loan.Id}_{loan.DocumentSet.Id}");
            //  Directory.CreateDirectory(documentSetPath); // your current if() is backwards

            // var docs = loan.DocumentSet?.Documents?.ToList() ?? new List<DominateDocsData.Models.Document>();
            //    var tracker = new MergeTracker(docs.Count);

            // register tracker BEFORE enqueueing
            //  mergeTrackers[loan.Id] = tracker;

            // Enqueue merges
            //foreach (var doc in docs)
            //{
            //    var documentMerge = new DocumentMerge
            //    {
            //        LoanAgreement = loan,
            //        Document = doc,
            //        Status = DocumentMergeState.Status.Queued
            //    };

            //    docState.DocumentProcessingQueue.Enqueue(documentMerge);
            //}
            ///////////////////////////////
            ///
            //New Section
            // Example: you already loaded document DTOs somewhere (list page, cache, etc.)
            //var dtoById = documentDtos.ToDictionary(d => d.Id);

            //// Evaluate
            //var finalDtos = DocumentOutputEvaluator.BuildFinalDocumentSet(
            //    loanType,
            //    data,
            //    id => dtoById.TryGetValue(id, out var dto) ? dto : null
            //);



            // Wait for all documents in set to be merged
            Dictionary<Guid, byte[]> mergedFiles;
            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                timeoutCts.CancelAfter(TimeSpan.FromMinutes(5)); // pick your SLA

                // mergedFiles = await tracker.WhenAllMerged.WaitAsync(timeoutCts.Token);
            }
            finally
            {
                mergeTrackers.TryRemove(loan.Id, out _);
            }

            // Zip + email
            // Build file names however you want. Example:
            //var filesForZip = mergedFiles.ToDictionary(
            //    kv => $"{kv.Key}",
            //    kv => kv.Value
            //);

            //using var zipStream = await CreateZipStream(filesForZip);

            //zipStream.Position = 0;

            //var attachment = new EmailAttachment
            //{
            //    FileName = $"{loan.ReferenceName}.zip",
            //    ContentType = "application/zip",
            //    OutputType = EmailAttachmentEnums.OutputType.ZipFile,

            //    SourceType = EmailAttachmentEnums.Type.FileStream,
            //    Stream = new MemoryStream(zipStream.ToArray())
            //};

            // SendMail(attachment);

            // TODO: email zipStream as attachment (don’t forget zipStream.Position = 0 is already done)

            ////Get the Document Set
            //foreach (var doc in loan.DocumentSet.Documents)
            //{
            //    DocumentMerge documentMerge = new()
            //    {
            //        LoanAgreement = loan,
            //        Document = doc,
            //        Status = DocumentMergeState.Status.Queued
            //    };

            //    //Send to Document Merge Processing Queue
            //    docState.DocumentProcessingQueue.Enqueue(documentMerge);

            //}

            ////Zip Document Set

            ////Write Document Store in Azure

            ////Make document available for Distribution  Add to the Distribution Queue

            docState.LoanList.Remove(loan.Id, out loanState);

            OnLoanCompletedEvent?.Invoke(this, loan);
        }
        catch (Exception)
        {
            OnLoanErrorEvent?.Invoke(this, loan);
            docState.LoanList.Remove(loan.Id, out loanState);
            throw;
        }
        finally
        {
            loanSemaphoreSlim?.Release();
        }
    }

    private async Task SendMailToRecipents(List<EmailAttachment> attachments)
    {
        try
        {
            string ken = "";

            DominateDocsNotify.Models.EmailMsg email = new EmailMsg()
            {
                //To = EditingMailMsg.To,
                //ReplyTo = EditingMailMsg.To,
                //Subject = ,
                //PostMarkTemplateId = (int)DominateDocsNotify.Enums.EmailEnums.Templates.MergeTest,
                //TemplateModel = new
                //{
                //    login_url = "https://DominateDocs.law/account/login",
                //    username = EditingMailMsg.Name ?? string.Empty,
                //    product_name = "DominateDocs",
                //    support_email = "https://DominateDocs.law/support",
                //    help_url = "https://DominateDocs.law/help"
                //},

                Attachments = attachments
            };

            //NotifyState.EmailMsgProcessingQueue.Enqueue(email);
        }
        catch (System.Exception ex)
        {
            throw;
        }
    }

    private async Task<string> BuildPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IPartyNames
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isIndividual = p.EntityType == DominateDocsData.Enums.Entity.Types.Individual;

            string line = isIndividual ? $"{p.EntityName} a {p.EntityType}" : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}";

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildLenderNamesAsync(IEnumerable<DominateDocsData.Models.Lender> lender, CancellationToken cancellationToken = default)
    {
        if (lender is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in lender)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isIndividual = p.EntityType == DominateDocsData.Enums.Entity.Types.Individual;

            string line = "";
            if (p.NmlsLicenseNumber is not null)
            {
                line = isIndividual ? $"{p.EntityName} a {p.EntityType}" : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription} (CFL License No.{p.NmlsLicenseNumber})";
            }
            else
            {
                line = isIndividual ? $"{p.EntityName} a {p.EntityType}" : $"{p.EntityName} a {p.StateOfIncorporationDescription} {p.EntityStructureDescription}";
            }

            if (first)
            {
                sb.AppendLine(line);
                first = false;
            }
            else
            {
                sb.AppendLine($", {line}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildSigningPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : ISigningPartyNames
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine($"{p.Name} as {p.Title}");
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.Name} as {p.Title}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildAliasPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IAliasNames
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine($"{p.Name} as {p.AlsoKnownAs}");
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.Name} as {p.AlsoKnownAs}");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildOwnershipPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IOwnershipNames
    {
        if (parties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in parties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine($"{p.Name} a {p.PercentOfOwnership}% owner");
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.Name} a {p.PercentOfOwnership}% owner");
            }
        }

        return sb.ToString();
    }

    private async Task<string> BuildPropertyAddressAsync<T>(IEnumerable<T> properties, CancellationToken cancellationToken = default) where T : IPropertyAddresses
    {
        if (properties is null) return string.Empty;

        var sb = new StringBuilder();
        var first = true;

        foreach (var p in properties)
        {
            if (cancellationToken.IsCancellationRequested) break;

            if (first)
            {
                sb.AppendLine(p.FullAddress);
                first = false;
            }
            else
            {
                sb.AppendLine($", {p.FullAddress}");
            }
        }

        return sb.ToString();
    }

    public async Task ProcessTestDocumentAsync(byte[] documentBytes, LoanAgreement loan, CancellationToken stoppingToken)
    {
        LoanAgreement loanState = null;

        try
        {
            await loanSemaphoreSlim.WaitAsync();

            docState.LoanList.TryAdd(loan.Id, loan);

            docState.StateHasChanged();

            //var outputBytes = wordServices.ReplaceTokens(doc.DocumentBytes, borrowerValues.ToDictionary(kv => kv.Key, kv => kv.Value ?? string.Empty, StringComparer.Ordinal));

            using var ms = new MemoryStream(documentBytes, writable: true); // wraps the existing buffer

            MemoryStream mergedSteram = await razorLiteService.ProcessAsync(ms, loan);

            //// Borrowers
            //foreach (var borrower in loan.Borrowers)
            //{
            //    var borrowerValues = BorrowerMergeMap.GetValues(borrower);

            //}

            docState.LoanList.Remove(loan.Id, out loanState);

            OnLoanCompletedEvent?.Invoke(this, loan);
        }
        catch (Exception)
        {
            OnLoanErrorEvent?.Invoke(this, loan);
            docState.LoanList.Remove(loan.Id, out loanState);
            throw;
        }
        finally
        {
            loanSemaphoreSlim?.Release();
        }
    }

    private async Task<MemoryStream> CreateZipStream(Dictionary<string, byte[]> files)
    {
        var zipStream = new MemoryStream();

        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in files)
            {
                var entry = archive.CreateEntry(file.Key, CompressionLevel.Optimal);

                using var entryStream = entry.Open();
                entryStream.Write(file.Value, 0, file.Value.Length);
            }
        }

        zipStream.Position = 0; // critical or your attachment will be empty
        return zipStream;
    }

    private async Task<List<EmailAttachment>> MergeDocument()
    {
        List<EmailAttachment> attachments = new();

        Dictionary<string, byte[]> files = new();

        string docName = "";

        try
        {
            //    attachments.Add(attach);
            //}
            //else
            //{
            //    foreach (var doc in EditingDocumentSet.Documents)
            //    {
            //        var file = await ProcessDocumentAsync(doc);

            //        if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.Word)
            //        {
            //            docName = $"{doc.Name}.docm";
            //        }
            //        else
            //        {
            //            docName = $"{doc.Name}.pdf";
            //        }

            //        if (!files.ContainsKey(docName)) files.Add(docName, file.MergedDocumentBytes);

            //    }

            //    if (!IsZipFile)
            //    {
            //        EmailAttachment attach;

            //        //Make Attachment for each file
            //        foreach (var file in files)
            //        {
            //            if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.Pdf)
            //            {
            //                attach = new EmailAttachment
            //                {
            //                    FileName = $"{file.Key}",
            //                    ContentType = "application/pdf",
            //                    OutputType = EmailAttachmentEnums.OutputType.PDF,
            //                    SourceType = EmailAttachmentEnums.Type.FileStream,
            //                    Stream = new MemoryStream(file.Value)

            //                };
            //            }
            //            else
            //            {
            //                attach = new EmailAttachment
            //                {
            //                    FileName = $"{file.Key}",
            //                    ContentType = "application/docm",
            //                    OutputType = EmailAttachmentEnums.OutputType.WordDoc,

            //                    SourceType = EmailAttachmentEnums.Type.FileStream,
            //                    Stream = new MemoryStream(file.Value)
            //                };
            //            }

            //            attachments.Add(attach);
            //        }

            //    }
            //    else
            //    {
            //        //Make Compressed File
            //        var zipStream = CreateZipStream(files);
            //        zipStream.Position = 0;

            //        var attach = new EmailAttachment
            //        {
            //            FileName = $"{EditingDocumentSet.Name}.zip",
            //            ContentType = "application/zip",
            //            OutputType = EmailAttachmentEnums.OutputType.ZipFile,

            //            SourceType = EmailAttachmentEnums.Type.FileStream,
            //            Stream = new MemoryStream(zipStream.ToArray())
            //        };

            //        attachments.Add(attach);

            //    }

            // }

            // SendMail(attachments);
        }
        catch (Exception ex)
        {
            //logger.LogError($"Error during document merge: {ex.Message}");
            //attachments = null;
        }

        return attachments;
    }

    private void OnDocumentMergeCompleted(object sender, DocumentMerge e)
    {
        try
        {
            // Only proceed if we have a tracker for this loan
            if (!mergeTrackers.TryGetValue(e.LoanAgreement.Id, out var tracker))
                return;

            if (e.Status == DocumentMergeState.Status.Complete && e.MergedDocumentBytes is not null)
            {
                tracker.CompleteOne(e.Document.Id, e.MergedDocumentBytes);
            }
            else
            {
                tracker.Fail(new Exception($"Merge failed for doc {e.Document.Id} (loan {e.LoanAgreement.Id})."));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling document merge completed.");
        }
    }

    private void OnDocumentMergeError(object sender, DocumentMerge e)
    {
        if (mergeTrackers.TryGetValue(e.LoanAgreement.Id, out var tracker))
            tracker.Fail(new Exception($"Merge error for doc {e.Document.Id} (loan {e.LoanAgreement.Id})."));
    }

    private void OnIsRunBackgroundLoanServiceChanged(object? sender, bool e)
    {
        if (docState.IsRunBackgroundLoanApplicationService)
        {
            DoRecoveryAsync();
        }
        else
        {
            DoCleanupAsync();
        }
    }

    //Service Maintenance Area
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (options.Value.IsActive)
                {
                    if (docState.IsRunBackgroundLoanApplicationService)
                    {
                        List<Task> loanTasks = new();

                        while (docState.LoanProcessQueue.Count > 0 && !stoppingToken.IsCancellationRequested)
                        {
                            logger.LogDebug("Loan Processing Background Service Found Job at: {time} Processong Queue Entry", DateTimeOffset.Now);

                            try
                            {
                                LoanAgreement loan = null;

                                docState.LoanProcessQueue.TryDequeue(out loan);

                                if (loan is not null) loanTasks.Add(Task.Run(async () => await ProcessLoanAsync(loan, stoppingToken)));
                            }
                            catch (Exception ex)
                            {
                                logger.LogError($"{ex.Message}");
                            }
                        }

                        if (loanTasks.Count > 0) await Task.WhenAll(loanTasks);

                        if (docState.LoanProcessQueue.Count == 0) logger.LogDebug("Loan Processing Background Service running at: {time}  Nothing Queued", DateTimeOffset.Now);
                    }
                    else
                    {
                        logger.LogDebug("Loan Processing Background Service PAUSED at: {time}", DateTimeOffset.Now);
                    }
                }
                else
                {
                    logger.LogDebug($"Loan Processing Background Service NOT Active");
                }

                docState.IsReadyForProcessing = true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            await Task.Delay(TimeSpan.FromMinutes(2));
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
        logger.LogDebug($"Loan Processing Background Service Recovering Tranactions");
    }

    private async Task DoCleanupAsync()
    {
        logger.LogDebug($"Loan Processing Background Service Performing Cleanup tasks");
    }
}