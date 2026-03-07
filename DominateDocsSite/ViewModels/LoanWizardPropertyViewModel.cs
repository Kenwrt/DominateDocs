using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardPropertyViewModel : ObservableObject
{
    public enum ValidationFailureField
    {
        None = 0,
        FullAddress = 1
    }

    [ObservableProperty]
    private bool lastUpsertSucceeded = false;

    [ObservableProperty]
    private DominateDocsSite.ViewModels.LoanWizardPropertyViewModel.ValidationFailureField lastValidationFailureField =
        DominateDocsSite.ViewModels.LoanWizardPropertyViewModel.ValidationFailureField.None;

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyRecord> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyRecord> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord selectedRecord = null;

    private readonly Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanWizardPropertyViewModel> logger;
    private ISnackbar snackbar;

    public LoanWizardPropertyViewModel(
        IMongoDatabaseRepo dbApp,
        ILogger<LoanWizardPropertyViewModel> logger,
        UserSession userSession,
        IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        userId = userSession.UserId;
    }

    public void SetSnackbar(ISnackbar snackbar) => this.snackbar = snackbar;

    public async Task InitializeEditorAsync(
        DominateDocsData.Models.PropertyRecord selectedProperty,
        IEnumerable<DominateDocsData.Models.PropertyRecord> currentLoanProperties)
    {
        MyList = currentLoanProperties?.ToObservableCollection() ?? new ObservableCollection<DominateDocsData.Models.PropertyRecord>();

        if (selectedProperty is not null)
        {
            SelectedRecord = selectedProperty;
            EditingRecord = DeepCopy(selectedProperty);
        }
        else
        {
            ClearSelection();
        }

        if (EditingRecord is null)
            GetNewRecord();

        RecordList.Clear();
        var records = dbApp.GetRecords<DominateDocsData.Models.PropertyRecord>()
            .Where(x => x.UserId == userId)
            .ToList();

        foreach (var record in records)
        {
            RecordList.Add(record);
        }

        await Task.CompletedTask;
    }

    [RelayCommand]
    public async Task UpsertRecord()
    {
        LastUpsertSucceeded = false;
        LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyViewModel.ValidationFailureField.None;

        if (EditingRecord is null)
            return;

        if (string.IsNullOrWhiteSpace(EditingRecord.FullAddress))
        {
            LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyViewModel.ValidationFailureField.FullAddress;
            snackbar?.Add("Property address is required.", Severity.Error);
            return;
        }

        EditingRecord.PropertyOwners ??= new List<DominateDocsData.Models.PropertyOwner>();
        EditingRecord.EntityOwners ??= new List<DominateDocsData.Models.EntityOwner>();
        EditingRecord.Liens ??= new List<DominateDocsData.Models.Lien>();
        EditingRecord.UserId = userId;

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.PropertyRecord>(EditingRecord);

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

        LastUpsertSucceeded = true;
        LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyViewModel.ValidationFailureField.None;
        snackbar?.Add("Property saved successfully.", Severity.Success);
    }

    [RelayCommand]
    public void SelectRecord(DominateDocsData.Models.PropertyRecord property)
    {
        if (property is null)
            return;

        SelectedRecord = property;
        EditingRecord = DeepCopy(property);
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedRecord = null;
        GetNewRecord();
    }

    [RelayCommand]
    public void GetNewRecord()
    {
        EditingRecord = new DominateDocsData.Models.PropertyRecord()
        {
            UserId = userId,
            PropertyType = Property.Types.SingleFamily,
            PropertyOwners = new List<DominateDocsData.Models.PropertyOwner>(),
            EntityOwners = new List<DominateDocsData.Models.EntityOwner>(),
            Liens = new List<DominateDocsData.Models.Lien>(),
            IsPropertyOwnerSameAsBorrower = true,
            IsPropertyOwnerSameAsGuarantor = false,
            IsPropertyOwnerThridPartyOwner = false
        };
    }

    private static DominateDocsData.Models.PropertyRecord DeepCopy(DominateDocsData.Models.PropertyRecord source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<DominateDocsData.Models.PropertyRecord>(json)!;
    }
}
