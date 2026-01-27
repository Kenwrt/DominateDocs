using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class UserDefaultBrokerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Broker> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Broker> myBrokerList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Broker editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Broker selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<UserDefaultBrokerViewModel> logger;
    private IApplicationStateManager appState;

    public UserDefaultBrokerViewModel(IMongoDatabaseRepo dbApp, ILogger<UserDefaultBrokerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        dbApp.GetRecords<DominateDocsData.Models.Broker>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private void SelectRecordById(Guid id)
    {
        if (id != Guid.Empty)
        {
            EditingRecord = dbApp.GetRecordById<DominateDocsData.Models.Broker>(id);
        }
        else
        {
            GetNewRecord();
        }


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

        int myBrokerIndex = MyBrokerList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myBrokerIndex > -1)
        {
            MyBrokerList[myBrokerIndex] = EditingRecord;
        }
        else
        {
            MyBrokerList.Add(EditingRecord);
        }

        if (EditingRecord.BrokerCode is null)
        {
            EditingRecord.BrokerCode = $"B-{DisplayHelper.GenerateIdCode().ToString()}";
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Broker>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Broker r)
    {
        int myBrokerIndex = MyBrokerList.FindIndex(x => x.Id == r.Id);

        if (myBrokerIndex > -1)
        {
            MyBrokerList.RemoveAt(myBrokerIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Broker r)
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
        EditingRecord = new DominateDocsData.Models.Broker()
        {
            UserId = userId
        };
    }
}