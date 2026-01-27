using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Helpers;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class UserDefaultLenderViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Lender> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Lender> myLenderList = new();

  
    [ObservableProperty]
    private DominateDocsData.Models.Lender editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Lender selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<UserDefaultLenderViewModel> logger;

    public UserDefaultLenderViewModel(IMongoDatabaseRepo dbApp, ILogger<UserDefaultLenderViewModel> logger, UserSession userSession, IApplicationStateManager appState)
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
        RecordList.Clear();

       // dbApp.GetRecords<DominateDocsData.Models.Lender>().ToList().ForEach(lf => RecordList.Add(lf));
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

        int myLenderIndex = MyLenderList.FindIndex(x => x.Id == EditingRecord.Id);

        if (myLenderIndex > -1)
        {
            MyLenderList[myLenderIndex] = EditingRecord;
        }
        else
        {
            MyLenderList.Add(EditingRecord);
        }

        if (EditingRecord.LenderCode is null)
        {
            EditingRecord.LenderCode = $"L-{DisplayHelper.GenerateIdCode().ToString()}";
        }

    await dbApp.UpSertRecordAsync<DominateDocsData.Models.Lender>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Lender r)
    {
        int myLenderIndex = MyLenderList.FindIndex(x => x.Id == r.Id);

        if (myLenderIndex > -1)
        {
            MyLenderList.RemoveAt(myLenderIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Lender r)
    {
        if (r != null)
        {
            SelectedRecord = r;
            EditingRecord = r;
        }
    }

    [RelayCommand]
    private void SelectRecordById(Guid id)
    {
        if (id != Guid.Empty)
        {
            EditingRecord = dbApp.GetRecordById<DominateDocsData.Models.Lender>(id);
        }
        else
        {
            GetNewRecord();
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
        EditingRecord = new DominateDocsData.Models.Lender()
        {
            UserId = userId
        };
    }
}