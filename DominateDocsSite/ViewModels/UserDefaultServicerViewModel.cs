using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class UserDefaultServicerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Servicer> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Servicer> myServicerList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Servicer editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Servicer selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<UserDefaultServicerViewModel> logger;
    private IApplicationStateManager appState;

    public UserDefaultServicerViewModel(IMongoDatabaseRepo dbApp, ILogger<UserDefaultServicerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        dbApp.GetRecords<DominateDocsData.Models.Servicer>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

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

        int myServicerIndex = MyServicerList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myServicerIndex > -1)
        {
            MyServicerList[myServicerIndex] = EditingRecord;
        }
        else
        {
            MyServicerList.Add(EditingRecord);
        }

        if (EditingRecord.ServicerCode is null)
        {
            EditingRecord.ServicerCode = $"S-{DominateDocsSite.Helpers.DisplayHelper.GenerateIdCode().ToString()}";
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Servicer>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Servicer r)
    {
        int myServicerIndex = MyServicerList.FindIndex(x => x.Id == r.Id);

        if (myServicerIndex > -1)
        {
            MyServicerList.RemoveAt(myServicerIndex);
        }
    }

    [RelayCommand]
    private void SelectRecordById(Guid id)
    {
        if (id != Guid.Empty)
        {
            EditingRecord = dbApp.GetRecordById<DominateDocsData.Models.Servicer>(id);
        }
        else
        {
            GetNewRecord();
        }


    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Servicer r)
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
        EditingRecord = new DominateDocsData.Models.Servicer()
        {
            UserId = userId
        };
    }
}