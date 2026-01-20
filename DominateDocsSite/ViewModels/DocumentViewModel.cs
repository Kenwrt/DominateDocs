using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager.Services;
using DominateDocsData.Models;
using DominateDocsSite.Database;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    private IWordServices wordServices;

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Document> recordList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Document? selectedRecord;

    [ObservableProperty]
    private DominateDocsData.Models.Document editingRecord;

    [ObservableProperty]
    private DominateDocsData.Models.DocumentLibrary? selectedLibrary;

    //[ObservableProperty]
    //private DominateDocsData.Models.DocumentSet? selectedDocumentSet;

    public string? SessionToken { get; set; }

    private Guid userId;

    private readonly IMongoDatabaseRepo dbApp;
    private DocumentLibraryViewModel dlVm;
    private DocumentSetViewModel dsVm;
    private UserSession userSession;
    private Session session;
    private string downloadFileName = string.Empty;
    private readonly HttpClient httpClient = new HttpClient();
    private IApplicationStateManager appState;
    private IWebHostEnvironment webEnv;

    public DocumentViewModel(IMongoDatabaseRepo dbApp, IWordServices wordServices, UserSession userSession, IApplicationStateManager appState, DocumentLibraryViewModel dlVm, DocumentSetViewModel dsVm, IWebHostEnvironment webEnv)
    {
        this.dsVm = dsVm;
        this.dlVm = dlVm;
        this.wordServices = wordServices;
        this.userSession = userSession;
        this.appState = appState;
        this.webEnv = webEnv;

        userId = userSession.UserId;

        this.dbApp = dbApp;

        this.wordServices = wordServices;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        if (EditingRecord is null) GetNewRecordAsync();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Document>().Where(doc => doc.DocLibId == userSession.DocLibId).ToList().ForEach(doc => RecordList.Add(doc));
                
    }

    [RelayCommand]
    private async Task AddRecord()
    {
        //string path;

        //byte[] masterTemplateBytes = SelectedLibrary.MasterTemplateBytes;

        //string fileName = $"{EditingRecord.Name}--{System.DateTime.UtcNow.ToString("MM-dd-yyyy-HH-MM")}.docm";

        //using var ms = new MemoryStream(masterTemplateBytes);

        //DocumentTag documentTag = new DocumentTag
        //{
        //    DocumentId = EditingRecord.Id,
        //    LibraryId = SelectedLibrary.Id,
        //    DocumentName = fileName,
        //    //DocumentCollectionId = EditingRecord.DocumentSetId,
        //    BaseTemplateId = SelectedLibrary.MasterTemplate,
        //    DocumentStoreId = Guid.Empty
        //};

        // var editedMs = await wordServices.InsertHiddenTagAsync(ms, "DominateDocsTag", JsonConvert.SerializeObject(documentTag, Formatting.Indented));

        //editedMs.Position = 0; // Reset the stream position to the beginning

        //var fileBytes = editedMs.ToArray();

        //if (SelectedDocumentSet is not null)
        //{
        //    //path = Path.Combine(webEnv.WebRootPath, @$"UploadedTemplates\{SelectedLibrary.Name}\{SelectedDocumentSet.Name}");
        //}
        //else
        //{
        //    path = Path.Combine(webEnv.WebRootPath, @$"UploadedTemplates\{SelectedLibrary.Name}");
        //}

        ////Directory.CreateDirectory(path);

        //var filePathAndName = Path.Combine(path, fileName);

        //System.IO.File.WriteAllBytesAsync(filePathAndName, fileBytes).Wait();

        //// EditingRecord.UpdatedAt = DateTime.UtcNow;
        //EditingRecord.TemplateDocumentBytes = fileBytes;
        //EditingRecord.HiddenTagValue = JsonConvert.SerializeObject(documentTag, Formatting.Indented);

        //RecordList.Add(EditingRecord); // Add to the local collection

        //dbApp.UpSertRecord<DominateDocsData.Models.Document>((DominateDocsData.Models.Document)EditingRecord);

        //if (SelectedDocumentSet is not null)
        //{
        //    //Add it to the DocumentSet's collection
        //    //SelectedDocumentSet.Documents.Add(EditingRecord);

        //    //dbApp.UpSertRecord<DocumentSet>(SelectedDocumentSet); // Update the DocumentSet in the database
        //}
        //else
        //{
        //    if (!SelectedLibrary.Documents.Any(x => x.Name == EditingRecord.Name))
        //    {
        //        SelectedLibrary.Documents.Add(EditingRecord);
        //    }

        //    dbApp.UpSertRecord<DominateDocsData.Models.DocumentLibrary>(SelectedLibrary); // Update the DocumentTemplate in the database
        //}

        EditingRecord = await GetNewRecordAsync();

        SelectedRecord = null;
    }

    [RelayCommand]
    private async Task EditRecord()
    {
        //if (SelectedRecord != null)
        //{
        //    var index = RecordList.IndexOf(SelectedRecord);
        //    if (index >= 0)
        //    {
        //        RecordList[index] = EditingRecord;
        //        dbApp.UpSertRecord<DominateDocsData.Models.Document>((DominateDocsData.Models.Document)EditingRecord);

        //        //Edit in the Document in Library
        //        DocumentLibrary docLib = dbApp.GetRecords<DominateDocsData.Models.DocumentLibrary>().FirstOrDefault(lib => lib.Id == SelectedRecord.DocLibId);

        //        if (docLib is not null)
        //        {
        //            var libDocIndex = docLib.Documents.FindIndex(d => d.Id == EditingRecord.Id);

        //            if (libDocIndex >= 0)
        //            {
        //                docLib.Documents[libDocIndex] = EditingRecord;
        //                dbApp.UpSertRecord<DominateDocsData.Models.DocumentLibrary>(docLib);
        //            }
        //        }

        //        //Edit in the Document in DocumentSet

        //        //DocumentSet docSet = dbApp.GetRecords<DominateDocsData.Models.DocumentSet>().FirstOrDefault(lib => lib.Id == SelectedRecord.DocSetId);

        //        //if (docSet is not null)
        //        //{
        //        //    var docSetDocIndex = SelectedDocumentSet.Documents.FindIndex(d => d.Id == EditingRecord.Id);

        //        //    if (docSetDocIndex >= 0)
        //        //    {
        //        //        SelectedDocumentSet.Documents[docSetDocIndex] = EditingRecord;
        //        //        dbApp.UpSertRecord<DominateDocsData.Models.DocumentSet>(SelectedDocumentSet);
        //        //    }

        //        //}

        //        SelectedRecord = null;

        //        EditingRecord = await GetNewRecordAsync();
        //    }
        //}
    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        EditingRecord = await GetNewRecordAsync();
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Document doc)
    {
        //if (doc != null)
        //{
        //    RecordList.RemoveWhere(x => x.Id == doc.Id);

        //    dbApp.DeleteRecord<DominateDocsData.Models.Document>((DominateDocsData.Models.Document)doc);

        //    SelectedLibrary.Documents.RemoveAll(x => x.Id == doc.Id);

        //   // if (SelectedDocumentSet is not null && SelectedDocumentSet.Documents.Any(x => x.Id == doc.Id)) SelectedDocumentSet.Documents.RemoveAll(x => x.Id == doc.Id);

        //    SelectedRecord = null;

        //    EditingRecord = await GetNewRecordAsync();
        //}
    }

    [RelayCommand]
    private async Task SelectRecord(DominateDocsData.Models.Document? doc)
    {
        //if (doc != null)
        //{
        //    SelectedRecord = doc;
        //    EditingRecord = doc;
        //}
    }

    [RelayCommand]
    private async Task ClearEditing()
    {
        SelectedRecord = null;
        EditingRecord = await GetNewRecordAsync();
    }

    private async Task<DominateDocsData.Models.Document> GetNewRecordAsync()
    {
        EditingRecord = new DominateDocsData.Models.Document()
        {
           
            DocLibId = userSession.DocLibId,
           
        };

        return EditingRecord;
    }
}