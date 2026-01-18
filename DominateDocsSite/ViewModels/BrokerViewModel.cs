using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;

using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class BrokerViewModel : ObservableObject
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

    [ObservableProperty]
    private Guid? loanAgreementId = null;

    [ObservableProperty]
    private int counter = 0;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly UserSession userSession;
    private readonly ILogger<BrokerViewModel> logger;
    private IApplicationStateManager appState;
    private static readonly Random rng = Random.Shared;

    public BrokerViewModel(IMongoDatabaseRepo dbApp, ILogger<BrokerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Broker>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Broker r)
    {
        int myBrokerIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (myBrokerIndex > -1)
        {
            RecordList.RemoveAt(myBrokerIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.Broker>(r);
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

    public int GenerateLenderCode()
    {
        return rng.Next(10000, 100000); // upper bound is exclusive
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingRecord = new DominateDocsData.Models.Broker()
        {
            UserId = userId,
            BrokerCode = GenerateLenderCode()
        };
    }
}