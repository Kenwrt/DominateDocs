using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using DominateDocsData.Enums;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class BorrowerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Borrower> recordList = new();

    //[ObservableProperty]
    //private ObservableCollection<DominateDocsData.Models.Borrower> myBorrowerList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Borrower editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Borrower selectedRecord = null;

    [ObservableProperty]
    private int counter = 0;

    private List<DominateDocsData.Models.Borrower> loanBorrowers;

    private Guid userId;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly UserSession userSession;
    private readonly ILogger<BorrowerViewModel> logger;
    private IApplicationStateManager appState;

    public BorrowerViewModel(IMongoDatabaseRepo dbApp, ILogger<BorrowerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Borrower>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Borrower>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Borrower r)
    {
        int myBorrowerIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (myBorrowerIndex > -1)
        {
            RecordList.RemoveAt(myBorrowerIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.Borrower>(r);
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Borrower r)
    {
        if (r != null)
        {
            SelectedRecord = r;
            EditingRecord = r;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedRecord != null)
        {
            SelectedRecord = null;
            GetNewRecord();
        }
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingRecord = new DominateDocsData.Models.Borrower()
        {
            UserId = userId
        };
    }
}