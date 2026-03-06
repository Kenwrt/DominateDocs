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

public partial class LoanWizardGuarantorViewModel : ObservableObject
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
    private ValidationFailureField lastValidationFailureField = ValidationFailureField.None;

    [ObservableProperty]
    private ObservableCollection<Guarantor> recordList = new();

    [ObservableProperty]
    private Guarantor editingRecord = null;

    [ObservableProperty]
    private Guarantor selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanWizardGuarantorViewModel> logger;
    private ISnackbar snackbar;

    public LoanWizardGuarantorViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanWizardGuarantorViewModel> logger, UserSession userSession)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        userId = userSession.UserId;
    }

    public void SetSnackbar(ISnackbar snackbar) => this.snackbar = snackbar;

    [RelayCommand]
    public async Task InitializePage(Guarantor l)
    {
        if (l is not null)
        {
            SelectedRecord = l;
            EditingRecord = l;
        }
        else
        {
            ClearSelection();
        }

        if (EditingRecord is null)
            GetNewRecord();

        RecordList.Clear();
        var records = dbApp.GetRecords<Guarantor>().ToList();
        foreach (var r in records)
        {
            RecordList.Add(r);
        }
    }

    [RelayCommand]
    public async Task UpsertRecord()
    {
        LastUpsertSucceeded = false;
        LastValidationFailureField = ValidationFailureField.None;

        if (EditingRecord == null) return;

        if (EditingRecord.EntityType == Entity.Types.Individual)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.ContactName))
            {
                LastValidationFailureField = ValidationFailureField.ContactName;
                snackbar?.Add("Full Legal Name is required.", Severity.Error);
                return;
            }
            EditingRecord.EntityName = EditingRecord.ContactName;
        }
        else if (EditingRecord.EntityType == Entity.Types.Entity)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.EntityName))
            {
                LastValidationFailureField = ValidationFailureField.EntityName;
                snackbar?.Add("Entity Name is required.", Severity.Error);
                return;
            }
        }
        else if (EditingRecord.EntityType == Entity.Types.Trust)
        {
            if (string.IsNullOrWhiteSpace(EditingRecord.EntityName))
            {
                LastValidationFailureField = ValidationFailureField.TrustName;
                snackbar?.Add("Trust Name is required.", Severity.Error);
                return;
            }
        }

        if (string.IsNullOrWhiteSpace(EditingRecord.ContactEmail))
        {
            LastValidationFailureField = ValidationFailureField.ContactEmail;
            snackbar?.Add("Email is required.", Severity.Error);
            return;
        }

        await dbApp.UpSertRecordAsync<Guarantor>(EditingRecord);

        int idx = RecordList.FindIndex(x => x.Id == EditingRecord.Id);
        if (idx > -1) RecordList[idx] = EditingRecord; else RecordList.Add(EditingRecord);

        LastUpsertSucceeded = true;
        snackbar?.Add("Guarantor saved successfully.", Severity.Success);
    }

    [RelayCommand]
    public void GetNewRecord()
    {
        SelectedRecord = null;
        EditingRecord = new Guarantor()
        {
            UserId = userId,
            ReferenceCode = $"G-{DisplayHelper.GenerateIdCode()}",
            EntityType = Entity.Types.Entity
        };
    }

    [RelayCommand]
    public void ClearSelection()
    {
        SelectedRecord = null;
        GetNewRecord();
    }
}
