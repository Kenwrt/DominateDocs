using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class QuickPropertyViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyRecord> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyRecord> myPropertyList = new();

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private IApplicationStateManager appState;
    private readonly ILogger<PropertyViewModel> logger;

    public QuickPropertyViewModel(IMongoDatabaseRepo dbApp, ILogger<PropertyViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        SelectedRecord = null;

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.PropertyRecord>().Where(x => x.UserId == userId).ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        //Update All Collections

        int recordListIndex = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (recordListIndex > -1)
        {
            RecordList[recordListIndex] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        int myPropertyIndex = MyPropertyList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myPropertyIndex > -1)
        {
            MyPropertyList[myPropertyIndex] = EditingRecord;
        }
        else
        {
            MyPropertyList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.PropertyRecord>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.PropertyRecord r)
    {
        int myPropertyIndex = MyPropertyList.FindIndex(x => x.Id == r.Id);

        if (myPropertyIndex > -1)
        {
            MyPropertyList.RemoveAt(myPropertyIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.PropertyRecord r)
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
        EditingRecord = new DominateDocsData.Models.PropertyRecord()
        {
            UserId = userId
        };
    }
}