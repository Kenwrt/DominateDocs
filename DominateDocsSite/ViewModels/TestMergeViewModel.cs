using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager.Services;
using DominateDocsData.Models;
using DominateDocsNotify.Enums;
using DominateDocsNotify.Models;
using DominateDocsNotify.State;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text;

namespace DominateDocsSite.ViewModels;

public partial class TestMergeViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Document> documentList = new();

    //[ObservableProperty]
    //private ObservableCollection<DominateDocsData.Models.DocumentSet> documentSetList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.LoanAgreement> loanAgreementList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Document> documentMergeList = new();

    [ObservableProperty]
    private EmailMsg editingMailMsg = new();

    [ObservableProperty]
    private bool isZipFile = false;

    [ObservableProperty]
    private DominateDocsData.Enums.DocumentTypes.TestTypes testType = DominateDocsData.Enums.DocumentTypes.TestTypes.Document;

    [ObservableProperty]
    private DominateDocsData.Enums.DocumentTypes.OutputTypes outputType = DominateDocsData.Enums.DocumentTypes.OutputTypes.PDF;

    [ObservableProperty]
    private EmailMsg selectedMailMsg = null;

    [ObservableProperty]
    private DominateDocsData.Models.Document editingDocument = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement loanAgreement = null;

    // [ObservableProperty]
    // private DominateDocsData.Models.DocumentSet editingDocumentSet = null;

    [ObservableProperty]
    private DominateDocsData.Models.Document selectedDocument = null;

    [ObservableProperty]
    private DominateDocsData.Models.Document selectedDocumentSet = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private IRazorLiteService razorLiteService;
    private IWebHostEnvironment env;
    private IWordServices wordServices;
    private readonly ILogger<TestMergeViewModel> logger;

    public INotifyState? NotifyState { get; set; }

    public TestMergeViewModel(IMongoDatabaseRepo dbApp, ILogger<TestMergeViewModel> logger, UserSession userSession, IApplicationStateManager appState, IRazorLiteService razorLiteService, IWebHostEnvironment env, IWordServices wordServices, INotifyState? NotifyState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.razorLiteService = razorLiteService;
        this.env = env;
        this.wordServices = wordServices;
        this.NotifyState = NotifyState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        GetNewRecord();

        dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().ToList().ForEach(la => LoanAgreementList.Add(la));

        dbApp.GetRecords<DominateDocsData.Models.Document>().ToList().ForEach(lf => DocumentList.Add(lf));

        //  dbApp.GetRecords<DominateDocsData.Models.DocumentSet>().ToList().ForEach(lf => DocumentSetList.Add(lf));
    }

    [RelayCommand]
    private void MoveDocuments()
    {
       List<DocumentLibrary> docLibList =  dbApp.GetRecords<DominateDocsData.Models.DocumentLibrary>().ToList();

       foreach (var lib in docLibList)
       {
            foreach (var doc in lib.Documents)
            {
                if (doc.TemplateDocumentBytes is not null)
                {
                    DocumentStore docStore = new DocumentStore()
                    {
                        DocLibId = lib.Id,
                        DocId = doc.Id,
                        DocumentBytes = doc.TemplateDocumentBytes,
                        Name = doc.Name,

                    };

                    dbApp.UpSertRecordAsync<DominateDocsData.Models.DocumentStore>(docStore);

                    lib.DocumentIds.Add(doc.Id);

                    
                }
             
               

               
            }

       
            dbApp.UpSertRecordAsync<DominateDocsData.Models.DocumentLibrary>(lib);

       }


    }

    [RelayCommand]
    private async Task SendMail(List<EmailAttachment> attachments)
    {
        try
        {
            string ken = "";

            DominateDocsNotify.Models.EmailMsg email = new EmailMsg()
            {
                To = EditingMailMsg.To,
                ReplyTo = EditingMailMsg.To,
                Subject = EditingMailMsg.Subject,
                PostMarkTemplateId = (int)DominateDocsNotify.Enums.EmailEnums.Templates.MergeTest,
                TemplateModel = new
                {
                    login_url = "https://DominateDocs.law/account/login",
                    username = EditingMailMsg.Name ?? string.Empty,
                    product_name = "DominateDocs",
                    support_email = "https://DominateDocs.law/support",
                    help_url = "https://DominateDocs.law/help"
                },

                Attachments = attachments
            };

            NotifyState.EmailMsgProcessingQueue.Enqueue(email);
        }
        catch (System.Exception ex)
        {
            throw;
        }
    }

    [RelayCommand]
    private async Task<List<EmailAttachment>> MergeDocument()
    {
        List<EmailAttachment> attachments = new();

        Dictionary<string, byte[]> files = new();

        string docName = "";

        try
        {
            if (TestType == DominateDocsData.Enums.DocumentTypes.TestTypes.Document)
            {
                EmailAttachment attach;

                var file = await ProcessDocumentAsync(EditingDocument);

                if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.DOCX)
                {
                    docName = $"{EditingDocument.Name}.docm";
                }
                else
                {
                    docName = $"{EditingDocument.Name}.pdf";
                }

                if (IsZipFile)
                {
                    if (!files.ContainsKey(docName)) files.Add(docName, file.MergedDocumentBytes);

                    var zipStream = CreateZipStream(files);
                    zipStream.Position = 0;

                    attach = new EmailAttachment
                    {
                        FileName = $"{EditingDocument.Name}.zip",
                        ContentType = "application/zip",
                        OutputType = EmailAttachmentEnums.OutputType.ZipFile,

                        SourceType = EmailAttachmentEnums.Type.FileStream,
                        Stream = new MemoryStream(zipStream.ToArray())
                    };
                }
                else
                {
                    if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.DOCX)
                    {
                        attach = new EmailAttachment
                        {
                            FileName = $"{docName}",
                            ContentType = "application/docm",
                            OutputType = EmailAttachmentEnums.OutputType.WordDoc,

                            SourceType = EmailAttachmentEnums.Type.FileStream,
                            Stream = new MemoryStream(file.MergedDocumentBytes)
                        };
                    }
                    else
                    {
                        attach = new EmailAttachment
                        {
                            FileName = $"{docName}",
                            ContentType = "application/pdf",
                            OutputType = EmailAttachmentEnums.OutputType.PDF,
                            SourceType = EmailAttachmentEnums.Type.FileStream,
                            Stream = new MemoryStream(file.MergedDocumentBytes)
                        };
                    }
                }

                attachments.Add(attach);
            }
            else
            {
                //foreach (var doc in EditingDocumentSet.Documents)
                //{
                //    var file = await ProcessDocumentAsync(doc);

                //    if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.DOCX)
                //    {
                //        docName = $"{doc.Name}.docm";
                //    }
                //    else
                //    {
                //        docName = $"{doc.Name}.pdf";
                //    }

                //    if (!files.ContainsKey(docName)) files.Add(docName, file.MergedDocumentBytes);

                //}

                if (!IsZipFile)
                {
                    EmailAttachment attach;

                    //Make Attachment for each file
                    foreach (var file in files)
                    {
                        if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.PDF)
                        {
                            attach = new EmailAttachment
                            {
                                FileName = $"{file.Key}",
                                ContentType = "application/pdf",
                                OutputType = EmailAttachmentEnums.OutputType.PDF,
                                SourceType = EmailAttachmentEnums.Type.FileStream,
                                Stream = new MemoryStream(file.Value)
                            };
                        }
                        else
                        {
                            attach = new EmailAttachment
                            {
                                FileName = $"{file.Key}",
                                ContentType = "application/docm",
                                OutputType = EmailAttachmentEnums.OutputType.WordDoc,

                                SourceType = EmailAttachmentEnums.Type.FileStream,
                                Stream = new MemoryStream(file.Value)
                            };
                        }

                        attachments.Add(attach);
                    }
                }
                else
                {
                    //Make Compressed File
                    var zipStream = CreateZipStream(files);
                    zipStream.Position = 0;

                    //var attach = new EmailAttachment
                    //{
                    //    FileName = $"{EditingDocumentSet.Name}.zip",
                    //    ContentType = "application/zip",
                    //    OutputType = EmailAttachmentEnums.OutputType.ZipFile,

                    //    SourceType = EmailAttachmentEnums.Type.FileStream,
                    //    Stream = new MemoryStream(zipStream.ToArray())
                    //};

                    // attachments.Add(attach);
                }
            }

            SendMail(attachments);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error during document merge: {ex.Message}");
            attachments = null;
        }

        return attachments;
    }

    private async Task<DominateDocsData.Models.Document> ProcessDocumentAsync(DominateDocsData.Models.Document doc)
    {
        string dir = Path.Combine(env.WebRootPath, "TestMergedDocs");

        Directory.CreateDirectory(dir);

        try
        {
            if (doc.TemplateDocumentBytes is null) return null;

            byte[] mergedDocBytes;

            if (LoanAgreement == null) return null;

            using var ms = new MemoryStream(capacity: doc.TemplateDocumentBytes.Length + 4096); // give it some headroom

            ms.Write(doc.TemplateDocumentBytes, 0, doc.TemplateDocumentBytes.Length);

            ms.Position = 0;

            StringBuilder lenderNames = new StringBuilder();
            StringBuilder borrowerNames = new StringBuilder();
            StringBuilder brokerNames = new StringBuilder();
            StringBuilder propertyAddresses = new StringBuilder();

            LoanAgreement.LenderNames = await BuildLenderNamesAsync(LoanAgreement.Lenders);

            LoanAgreement.BorrowerNames = await BuildPartyNamesAsync<DominateDocsData.Models.Borrower>(LoanAgreement.Borrowers);

            LoanAgreement.BrokerNames = await BuildPartyNamesAsync<DominateDocsData.Models.Broker>(LoanAgreement.Brokers);

            LoanAgreement.GuarantorNames = await BuildPartyNamesAsync<DominateDocsData.Models.Guarantor>(LoanAgreement.Guarantors);

            LoanAgreement.PropertyAddresses = await BuildPropertyAddressAsync<DominateDocsData.Models.PropertyRecord>(LoanAgreement.Properties);

            LoanAgreement.DocumentTitle = doc.Name;

            //Process the document with RazorLite
            MemoryStream msResult = await razorLiteService.ProcessAsync(ms, LoanAgreement);

            if (msResult is not null)
            {
                //doc.MergedDocumentBytes = wordServices.RemoveBadCompatSetting(msResult.ToArray());

                ////Validate Format of Return Byte Array
                //var formatType = wordServices.DetectFormatBySignature(doc.MergedDocumentBytes);

                //logger.LogInformation($"FormatType: {formatType}");

                //using var za = new System.IO.Compression.ZipArchive(new MemoryStream(doc.MergedDocumentBytes), ZipArchiveMode.Read);

                //bool hasContentTypes = za.GetEntry("[Content_Types].xml") != null;

                //logger.LogInformation($"HasContentTypes: {hasContentTypes}");
                //bool hasDocumentXml = za.GetEntry("word/document.xml") != null;

                //logger.LogInformation($"HasDocumentXML: {hasDocumentXml}");

                //var errors = wordServices.ValidateDocx(doc.MergedDocumentBytes);

                //logger.LogInformation($"Validation Errors: {errors.Count()}");

                //wordServices.CheckForDuplicateZipEntries(doc.MergedDocumentBytes);

                //var outPath = @"C:\Temp\processasync-output.docx";
                //File.WriteAllBytes(outPath, doc.MergedDocumentBytes);

                if (OutputType == DominateDocsData.Enums.DocumentTypes.OutputTypes.DOCX)
                {
                    //no need to convert already in word format

                    msResult.Position = 0;
                    doc.Name = $"{doc.Name}";
                }
                else
                {
                    MemoryStream pdfStream = await wordServices.ConvertWordToPdfAsync(msResult);

                    pdfStream.Position = 0;

                    doc.MergedDocumentBytes = pdfStream.ToArray();
                    doc.Name = $"{doc.Name}";

                    //Convert to PDF

                    var pdfPath = Path.Combine(dir, $"{DisplayHelper.CapitalizeWordsNoSpaces(doc.Name)}-TestMerge.pdf");
                    await File.WriteAllBytesAsync(pdfPath, pdfStream.ToArray());
                }
            }
            else
            {
                logger.LogError($"RazorLite processing returned null stream. for {doc.Name}");
                return null;
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Error during document merge: {ex.Message}");
        }

        return doc;
    }

    public static MemoryStream CreateZipStream(Dictionary<string, byte[]> files)
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

    private Task<string> BuildPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IPartyNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

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

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildLenderNamesAsync(IEnumerable<DominateDocsData.Models.Lender> lender, CancellationToken cancellationToken = default)
    {
        if (lender is null) return Task.FromResult(string.Empty);

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

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildSigningPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : ISigningPartyNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

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

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildAliasPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IAliasNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

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

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildOwnershipPartyNamesAsync<T>(IEnumerable<T> parties, CancellationToken cancellationToken = default) where T : IOwnershipNames
    {
        if (parties is null) return Task.FromResult(string.Empty);

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

        return Task.FromResult(sb.ToString());
    }

    private Task<string> BuildPropertyAddressAsync<T>(IEnumerable<T> properties, CancellationToken cancellationToken = default) where T : IPropertyAddresses
    {
        if (properties is null) return Task.FromResult(string.Empty);

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

        return Task.FromResult(sb.ToString());
    }

    [RelayCommand]
    private void SelectDocumentRecord(DominateDocsData.Models.Document r)
    {
        if (r != null)
        {
            SelectedDocument = r;
        }
    }

    [RelayCommand]
    private void ClearDocumentSelection()
    {
        SelectedDocument = new();
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingMailMsg = new DominateDocsNotify.Models.EmailMsg();
    }
}