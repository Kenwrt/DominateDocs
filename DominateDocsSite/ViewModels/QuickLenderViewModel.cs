using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class QuickLenderViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Lender> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Lender> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Lender editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Lender selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickLenderViewModel> logger;

    public QuickLenderViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickLenderViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.Lender> list)
    {
        if (list != null) MyList = list.ToObservableCollection();

        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Lender>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

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

        if (EditingRecord.LenderCode is null)
        {
            EditingRecord.LenderCode = $"L-{DominateDocsSite.Helpers.DisplayHelper.GenerateIdCode().ToString()}";
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Lender>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Lender r)
    {
        int index = MyList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            MyList.RemoveAt(index);
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

        EditingRecord = dbApp.GetRecords<DominateDocsData.Models.Lender>().FirstOrDefault(x => x.Id == id);
        
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