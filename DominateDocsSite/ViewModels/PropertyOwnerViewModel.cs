using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class PropertyOwnerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyOwner> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyOwner> myOwnerList = new();

    [ObservableProperty]
    private DominateDocsData.Models.PropertyOwner editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyOwner selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<PropertyOwnerViewModel> logger;

    public PropertyOwnerViewModel(IMongoDatabaseRepo dbApp, ILogger<PropertyOwnerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.PropertyOwner> ownerList = null)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.PropertyOwner>().ToList().ForEach(lf => RecordList.Add(lf));

        if (ownerList is not null)
        {
            MyOwnerList.Clear();
            MyOwnerList = ownerList.ToObservableCollection();
        }
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

        index = MyOwnerList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyOwnerList[index] = EditingRecord;
        }
        else
        {
            MyOwnerList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.PropertyOwner>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.PropertyOwner r)
    {
        int myOwnerIndex = MyOwnerList.FindIndex(x => x.Id == r.Id);

        if (myOwnerIndex > -1)
        {
            MyOwnerList.RemoveAt(myOwnerIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.PropertyOwner>(r);
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.PropertyOwner r)
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
        EditingRecord = new DominateDocsData.Models.PropertyOwner()
        {
            UserId = userId
        };
    }
}