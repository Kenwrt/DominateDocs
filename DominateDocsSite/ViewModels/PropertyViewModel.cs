using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class PropertyViewModel : ObservableObject
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
    private IApplicationStateManager appState;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<PropertyViewModel> logger;

    public PropertyViewModel(IMongoDatabaseRepo dbApp, ILogger<PropertyViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(DominateDocsData.Models.PropertyRecord property)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();
        if (property != null)
        {
            var r = dbApp.GetRecordById<DominateDocsData.Models.PropertyRecord>(property.Id);

            if (r is not null)
            {
                SelectedRecord = r;
                EditingRecord = r;
            }
        }
          

        dbApp.GetRecords<DominateDocsData.Models.PropertyRecord>().Where(x => x.UserId == userId).ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        index = MyPropertyList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyPropertyList[index] = EditingRecord;
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
        int myPropertyIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (myPropertyIndex > -1)
        {
            RecordList.RemoveAt(myPropertyIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.PropertyRecord>(r);
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
        EditingRecord = new DominateDocsData.Models.PropertyRecord();
    }
}