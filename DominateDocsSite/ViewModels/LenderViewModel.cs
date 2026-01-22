using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class LenderViewModel : ObservableObject
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

    [ObservableProperty]
    private Guid? loanAgreementId = null;

    private readonly IMongoDatabaseRepo dbApp;

    private readonly ILogger<LenderViewModel> logger;

    private IApplicationStateManager appState;
  
    private readonly UserSession userSession;

    public LenderViewModel(IMongoDatabaseRepo dbApp, ILogger<LenderViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;

        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.Lender> lenderList = null)
    {
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Lender>().ToList().ForEach(lf => RecordList.Add(lf));

        if (lenderList is not null)
        {
            MyLenderList.Clear();
            MyLenderList = lenderList.ToObservableCollection();
        }
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !string.IsNullOrEmpty(EditingRecord.ContactName))
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

        index = MyLenderList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyLenderList[index] = EditingRecord;
        }
        else
        {
            MyLenderList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Lender>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Lender r)
    {
        int lenderIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (lenderIndex > -1)
        {
            RecordList.RemoveAt(lenderIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.Lender>(r);
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
            UserId = userId,
            LenderCode = $"L-{DisplayHelper.GenerateIdCode()}"

        };
    }
}