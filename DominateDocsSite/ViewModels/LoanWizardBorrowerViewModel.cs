using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsData.Models;
using DominateDocsSite.State;
using MudBlazor;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardBorrowerViewModel : ObservableObject
{
    public enum ValidationFailureField
    {
        None = 0,
        ContactName = 1,
        SSN = 2,
        EntityName = 3,
        EIN = 4,
        TrustName = 5,
        ContactEmail = 6
    }

    [ObservableProperty]
    private bool lastUpsertSucceeded = false;

    [ObservableProperty]
    private DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField lastValidationFailureField =
        DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.None;

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Borrower> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Borrower> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Borrower editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Borrower selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private ISnackbar snackbar;

    public LoanWizardBorrowerViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanWizardBorrowerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.userSession = userSession;
        userId = userSession.UserId;
    }

    public void SetSnackbar(ISnackbar snackbar) => this.snackbar = snackbar;

    [RelayCommand]
    public async Task InitializePage(DominateDocsData.Models.Borrower b)
    {
        if (b is not null)
        {
            SelectedRecord = b;
            EditingRecord = b;
        }
        else
        {
            ClearSelection();
        }

        if (EditingRecord is null)
            GetNewRecord();

        RecordList.Clear();
        var records = dbApp.GetRecords<DominateDocsData.Models.Borrower>().ToList();
        foreach (var r in records)
        {
            RecordList.Add(r);
        }
    }

    [RelayCommand]
    public async Task UpsertRecord()
    {
        LastUpsertSucceeded = false;
        LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.None;

        if (EditingRecord == null) return;

        // 1. REQUIRED FIELD VALIDATION
        if (EditingRecord.EntityType == Entity.Types.Individual)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.ContactName))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.ContactName;
                snackbar?.Add("Full Legal Name is required.", Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingRecord.SSN))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.SSN;
                snackbar?.Add("SSN is required for Individuals.", Severity.Error);
                return;
            }

            EditingRecord.EntityName = EditingRecord.ContactName;
        }
        else if (EditingRecord.EntityType == Entity.Types.Entity)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.EntityName))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.EntityName;
                snackbar?.Add("Entity Name is required.", Severity.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingRecord.EIN))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.EIN;
                snackbar?.Add("EIN is required for Entities.", Severity.Error);
                return;
            }
        }
        else if (EditingRecord.EntityType == Entity.Types.Trust)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.EntityName))
            {
                LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.TrustName;
                snackbar?.Add("Trust Name is required.", Severity.Error);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(EditingRecord.ContactEmail))
        {
            LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.ContactEmail;
            snackbar?.Add("Email is required.", Severity.Error);
            return;
        }

        // 2. UNIQUENESS CHECK
        var all = dbApp.GetRecords<DominateDocsData.Models.Borrower>();
        bool duplicate = all.Any(b => b.Id != EditingRecord.Id && (
            (EditingRecord.EntityType == Entity.Types.Individual && b.SSN == EditingRecord.SSN) ||
            (EditingRecord.EntityType == Entity.Types.Entity && b.EIN == EditingRecord.EIN) ||
            (EditingRecord.EntityType == Entity.Types.Trust && b.EntityName != null && EditingRecord.EntityName != null && b.EntityName.ToLower() == EditingRecord.EntityName.ToLower())
        ));

        if (duplicate)
        {
            snackbar?.Add("A borrower with this identifying information already exists.", Severity.Warning);
            return;
        }

        // 3. SAVE
        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Borrower>(EditingRecord);

        // Update local list for UI
        int idx = RecordList.FindIndex(x => x.Id == EditingRecord.Id);
        if (idx > -1) RecordList[idx] = EditingRecord; else RecordList.Add(EditingRecord);

        LastUpsertSucceeded = true;
        LastValidationFailureField = DominateDocsSite.ViewModels.LoanWizardBorrowerViewModel.ValidationFailureField.None;

        snackbar?.Add("Borrower saved successfully.", Severity.Success);
    }

    [RelayCommand]
    public void GetNewRecord()
    {
        SelectedRecord = null;

        EditingRecord = new DominateDocsData.Models.Borrower()
        {
            UserId = userId,
            ReferenceCode = $"B-{DisplayHelper.GenerateIdCode()}",
            EntityType = Entity.Types.Individual
        };
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedRecord = null;
        GetNewRecord();
    }
}