using CommunityToolkit.Mvvm.ComponentModel;
using DominateDocsSite.Database;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class DocumentSetAssignmentViewModel : ObservableObject
{
    [ObservableProperty]
    private HashSet<DominateDocsData.Models.Document> selectedAssignedDocuments = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Document> documents = new();

    [ObservableProperty]
    private DominateDocsData.Models.Document? selectedDocument;

    //[ObservableProperty]
    //private DominateDocsData.Models.DocumentSet? selectedDocumentSet;

    private DocumentSetViewModel dsVm;
    private UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;

    public DocumentSetAssignmentViewModel(IMongoDatabaseRepo dbApp, UserSession userSession, DocumentSetViewModel dsVm)
    {
        this.userSession = userSession;
        this.dsVm = dsVm;
        this.dbApp = dbApp;

        //SelectedDocumentSet = dsVm.SelectedDocumentSet;

        dbApp.GetRecords<DominateDocsData.Models.Document>().ToList().ForEach(d => Documents.Add(d));
    }
}