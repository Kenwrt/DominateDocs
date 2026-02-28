using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardEntityOwnerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.EntityOwner> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.EntityOwner> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.EntityOwner editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.AkaName selectedAliaName = null;

    [ObservableProperty]
    private DominateDocsData.Models.EntityOwner selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanWizardEntityOwnerViewModel> logger;

    public LoanWizardEntityOwnerViewModel(
        IMongoDatabaseRepo dbApp,
        ILogger<LoanWizardEntityOwnerViewModel> logger,
        UserSession userSession,
        IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private void InitializeLoadPage(DominateDocsData.Models.EntityOwner selectedOwner)
    {
        if (selectedOwner is not null)
        {
            SelectedRecord = selectedOwner;
            EditingRecord = DeepCopy(selectedOwner);
        }
        else
        {
            SelectedRecord = null;
            GetNewRecord();
        }
    }

    [RelayCommand]
    private async Task InitializePage(DominateDocsData.Models.EntityOwner r)
    {
        if (r is not null)
        {
            SelectedRecord = r;
            EditingRecord = r;
        }

        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();
        dbApp.GetRecords<DominateDocsData.Models.EntityOwner>()
             .ToList()
             .ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        EditingRecord.EnforceTypeIntegrity();

        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);
        if (index > -1)
            RecordList[index] = EditingRecord;
        else
            RecordList.Add(EditingRecord);

        index = MyList.FindIndex(x => x.Id == EditingRecord.Id);
        if (index > -1)
            MyList[index] = EditingRecord;
        else
            MyList.Add(EditingRecord);

        // ✅ Removed standalone DB write. 
        // EntityOwner is persisted via the parent Borrower document.
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.EntityOwner r)
    {
        int index = MyList.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            MyList.RemoveAt(index);
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.EntityOwner r)
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
        EditingRecord = new DominateDocsData.Models.EntityOwner();
    }

    private static DominateDocsData.Models.EntityOwner DeepCopy(
        DominateDocsData.Models.EntityOwner source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<DominateDocsData.Models.EntityOwner>(json)!;
    }
}