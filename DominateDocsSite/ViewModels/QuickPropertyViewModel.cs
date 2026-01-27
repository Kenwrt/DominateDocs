using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class QuickPropertyViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyRecord> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyRecord> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private IApplicationStateManager appState;
    private readonly ILogger<QuickPropertyViewModel> logger;

    public QuickPropertyViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickPropertyViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.PropertyRecord> list )
    {
        if (list is not null) MyList = list.ToObservableCollection();   

        if (EditingRecord is null) GetNewRecord();

        SelectedRecord = null;

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.PropertyRecord>().Where(x => x.UserId == userId).ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        //Update All Collections

        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        index = MyList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyList[index] = EditingRecord;
        }
        else
        {
            MyList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.PropertyRecord>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.PropertyRecord r)
    {
        int index = MyList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            MyList.RemoveAt(index);
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