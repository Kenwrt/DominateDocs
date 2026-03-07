using CommunityToolkit.Mvvm.ComponentModel;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using MudBlazor;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardPropertyOwnerViewModel : ObservableObject
{
    public enum ValidationFailureField
    {
        None = 0,
        ContactName = 1,
        SSN = 2,
        EntityName = 3,
        EIN = 4,
        TrustName = 5
    }

    [ObservableProperty]
    private bool lastUpsertSucceeded = false;

    [ObservableProperty]
    private DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField lastValidationFailureField =
        DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.None;

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyOwner> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.PropertyOwner> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.PropertyOwner editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyOwner selectedRecord = null;

    private readonly Guid userId;
    private readonly IMongoDatabaseRepo dbApp;
    private ISnackbar snackbar;

    public LoanWizardPropertyOwnerViewModel(
        IMongoDatabaseRepo dbApp,
        ILogger<LoanWizardPropertyOwnerViewModel> logger,
        UserSession userSession,
        IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        userId = userSession.UserId;
    }

    public void SetSnackbar(ISnackbar snackbar) => this.snackbar = snackbar;

    public async Task InitializeEditorAsync(
        DominateDocsData.Models.PropertyOwner selectedOwner,
        IEnumerable<DominateDocsData.Models.PropertyOwner> currentOwners)
    {
        MyList = currentOwners?.ToObservableCollection() ?? new ObservableCollection<DominateDocsData.Models.PropertyOwner>();

        if (selectedOwner is not null)
        {
            SelectedRecord = selectedOwner;
            EditingRecord = DeepCopy(selectedOwner);
        }
        else
        {
            ClearSelection();
        }

        if (EditingRecord is null)
            GetNewRecord();

        RecordList.Clear();
        foreach (var owner in dbApp.GetRecords<DominateDocsData.Models.PropertyOwner>().Where(x => x.UserId == userId).ToList())
        {
            RecordList.Add(owner);
        }

        await Task.CompletedTask;
    }

    public async Task<bool> UpsertRecordAsync(IEnumerable<DominateDocsData.Models.PropertyOwner> existingOwners)
    {
        LastUpsertSucceeded = false;
        LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.None;

        if (EditingRecord is null)
            return false;

        if (EditingRecord.EntityType == Entity.Types.Individual)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.ContactName))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.ContactName;
                snackbar?.Add("Owner name is required.", Severity.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditingRecord.SSN))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.SSN;
                snackbar?.Add("SSN is required for individual owners.", Severity.Error);
                return false;
            }

            EditingRecord.EntityName = EditingRecord.ContactName;
        }
        else if (EditingRecord.EntityType == Entity.Types.Entity)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.EntityName))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.EntityName;
                snackbar?.Add("Entity name is required.", Severity.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditingRecord.EIN))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.EIN;
                snackbar?.Add("EIN is required for entity owners.", Severity.Error);
                return false;
            }
        }
        else if (EditingRecord.EntityType == Entity.Types.Trust)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.EntityName))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardPropertyOwnerViewModel.ValidationFailureField.TrustName;
                snackbar?.Add("Trust name is required.", Severity.Error);
                return false;
            }
        }

        EditingRecord.UserId = userId;
        EditingRecord.EnforceTypeIntegrity();

        bool duplicate = existingOwners.Any(x =>
            x.Id != EditingRecord.Id &&
            (
                (EditingRecord.EntityType == Entity.Types.Individual && !string.IsNullOrWhiteSpace(EditingRecord.SSN) && x.SSN == EditingRecord.SSN) ||
                (EditingRecord.EntityType == Entity.Types.Entity && !string.IsNullOrWhiteSpace(EditingRecord.EIN) && x.EIN == EditingRecord.EIN) ||
                (EditingRecord.EntityType == Entity.Types.Trust && !string.IsNullOrWhiteSpace(EditingRecord.EntityName) && string.Equals(x.EntityName, EditingRecord.EntityName, StringComparison.OrdinalIgnoreCase))
            ));

        if (duplicate)
        {
            snackbar?.Add("That owner is already attached to this property.", Severity.Warning);
            return false;
        }

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
        snackbar?.Add("Property owner saved.", Severity.Success);
        await Task.CompletedTask;
        return true;
    }

    public void ClearSelection()
    {
        SelectedRecord = null;
        GetNewRecord();
    }

    public void GetNewRecord()
    {
        EditingRecord = new DominateDocsData.Models.PropertyOwner()
        {
            UserId = userId,
            ReferenceCode = $"PO-{DisplayHelper.GenerateIdCode()}",
            EntityType = Entity.Types.Individual,
            PercentageOfOwnership = 100
        };
    }

    private static DominateDocsData.Models.PropertyOwner DeepCopy(DominateDocsData.Models.PropertyOwner source)
    {
        var json = JsonSerializer.Serialize(source);
        return JsonSerializer.Deserialize<DominateDocsData.Models.PropertyOwner>(json)!;
    }
}
