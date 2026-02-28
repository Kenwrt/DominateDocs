using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager.CalculatorsSchedulers;
using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Helpers;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsSite.State;
using MudBlazor;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardAddEditViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private int activeStepIndex = 0;

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>? loanTypes = new();

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.Broker selectedBroker = null;

    [ObservableProperty]
    private DominateDocsData.Models.Borrower selectedBorrower = null;

    [ObservableProperty]
    private DominateDocsData.Models.Lender selectedLender = null;

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor selectedGuarantor = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord selectedProperty = null;

    [ObservableProperty]
    private LoanType selectedLoanType;

    [ObservableProperty]
    private DominateDocsData.Models.PaymentSchedule? currentSchedule = new();

    [ObservableProperty]
    private DominateDocsData.Models.BalloonPayments currentBalloonSchedule = new();

    [ObservableProperty] private decimal principalAmount;
    [ObservableProperty] private decimal interestRate;
    [ObservableProperty] private decimal initialMargin;
    [ObservableProperty] private decimal estimatedDwnPaymentAmount;
    [ObservableProperty] private int termInMonths;
    [ObservableProperty] private decimal downPaymentPercentage;
    [ObservableProperty] private decimal balloonAmount;
    [ObservableProperty] private int balloonTermMonths;
    [ObservableProperty] private DominateDocsData.Enums.Payment.AmortizationTypes amorizationType;
    [ObservableProperty] private DominateDocsData.Enums.Payment.Schedules repaymentSchedule;
    [ObservableProperty] private DominateDocsData.Enums.Payment.RateTypes rateType;
    [ObservableProperty] private DominateDocsData.Enums.Payment.Schedules adjustmentInterval;
    [ObservableProperty] private DominateDocsData.Enums.Payment.IndexPaths assumedIndexPath;
    [ObservableProperty] private DominateDocsData.Enums.Payment.RateIndexes rateIndex;
    [ObservableProperty] private DateOnly? maturityDate;
    [ObservableProperty] private DominateDocsData.Models.PaymentSchedule paySchedule;
    [ObservableProperty] private DominateDocsData.Models.BalloonPayments payBalloonSchedule;
    [ObservableProperty] private DominateDocsData.Models.PaymentSchedule fixedPaymentSchedule;
    [ObservableProperty] private string? lastPipelineStatus;
    [ObservableProperty] private string activeSection = "loanSetup";
    [ObservableProperty] private HashSet<string> openSections = ["loanSetup"];
    [ObservableProperty] private bool isGenerating;
    [ObservableProperty] private bool showGenerated;
    [ObservableProperty] private string autoSaveStatus = "saved";
    [ObservableProperty] private Dictionary<string, List<string>> validationErrors = [];

    // ✅ Surfaces the last duplicate/validation error to the UI
    [ObservableProperty] private string? borrowerError = null;

    private Guid userId;
    private CancellationTokenSource? autoSaveCts;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanWizardAddEditViewModel> logger;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private DashboardViewModel dvm;
    private IDocumentManagerState docState;
    private IJobQueue<LoanJob> loanQueue;
    private IJobQueue<EmailJob> emailQueue;
    private ILoanScheduler loanScheduler;
    private IBalloonPaymentCalculater balloonPaymentCalculater;
    private IFetchCurrentIndexRatesAndSchedulesService indexRates;

    private int nextLoanNumber = 0;

    public LoanWizardAddEditViewModel(
        IMongoDatabaseRepo dbApp,
        ILogger<LoanWizardAddEditViewModel> logger,
        UserSession userSession,
        IApplicationStateManager appState,
        DashboardViewModel dvm,
        IDocumentManagerState docState,
        ILoanScheduler loanScheduler,
        IBalloonPaymentCalculater balloonPaymentCalculater,
        IFetchCurrentIndexRatesAndSchedulesService indexRates,
        IJobQueue<LoanJob> loanQueue,
        IJobQueue<EmailJob> emailQueue)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.dvm = dvm;
        this.docState = docState;
        this.loanScheduler = loanScheduler;
        this.balloonPaymentCalculater = balloonPaymentCalculater;
        this.indexRates = indexRates;
        this.loanQueue = loanQueue;
        this.emailQueue = emailQueue;

        userId = userSession.UserId;

        LoanTypes = new ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>(
            dbApp.GetRecords<DominateDocsData.Models.LoanType>()
                 .Select(x => new DominateDocsData.Models.DTOs.LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)));
    }

    // ─────────────────────────────────────────────
    // INITIALIZE
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task InitializePage(LoanAgreement loan)
    {
        try
        {
            if (loan is null)
                GetNewRecord();
            else
                EditingAgreement = loan;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InitializePage failed.");
        }
    }

    // ─────────────────────────────────────────────
    // LOAN TYPE
    // ─────────────────────────────────────────────

    public bool CanGoNextFromLoanTypeStep => SelectedLoanType is not null;

    public string GetIconForLoanType(LoanType lt)
    {
        var key = (lt.IconKey ?? string.Empty).Trim().ToLowerInvariant();
        return key switch
        {
            "construction" or "rehab" => Icons.Material.Filled.Construction,
            "bridge" or "fixflip" or "fix&flip" => Icons.Material.Filled.HomeRepairService,
            "dsc" or "dscr" or "rental" => Icons.Material.Filled.CreditCard,
            "commercial" => Icons.Material.Filled.Business,
            "multifamily" => Icons.Material.Filled.Apartment,
            "land" or "lot" => Icons.Material.Filled.Landscape,
            _ => Icons.Material.Filled.Description
        };
    }

    partial void OnSelectedLoanTypeChanged(LoanType value)
    {
        OnPropertyChanged(nameof(CanGoNextFromLoanTypeStep));
    }

    public static readonly (string Id, string Label, string Icon)[] Sections =
    [
        ("loanSetup",  "Loan Type",     "flash_on"),
        ("loanTerms",  "Loan Terms",    "attach_money"),
        ("parties",    "Parties",       "people"),
        ("property",   "Property",      "home"),
        ("fees",       "Fees",          "receipt_long"),
        ("features",   "Loan Features", "settings")
    ];

    // ─────────────────────────────────────────────
    // COMPLETION / SECTION HELPERS
    // ─────────────────────────────────────────────

    public int CompletionPercent
    {
        get
        {
            var checks = new[]
            {
                !string.IsNullOrEmpty(EditingAgreement.LoanTypeName),
                EditingAgreement.PrincipalAmount > 0 && EditingAgreement.InterestRate > 0,
                EditingAgreement.Borrowers.Any(b => !string.IsNullOrEmpty(b.EntityName)) &&
                    EditingAgreement.Lenders.Any(l => !string.IsNullOrEmpty(l.EntityName)),
                EditingAgreement.Properties.Any(p =>
                    !string.IsNullOrEmpty(p.FullAddress) || !string.IsNullOrEmpty(p.LegalDescription)),
                true,
                true
            };
            return (int)(checks.Count(c => c) / (double)checks.Length * 100);
        }
    }

    public bool IsSectionComplete(string sectionId) => sectionId switch
    {
        "loanSetup" => !string.IsNullOrEmpty(EditingAgreement.LoanTypeName),
        "loanTerms" => EditingAgreement.PrincipalAmount > 0 &&
                        EditingAgreement.InterestRate > 0 &&
                        EditingAgreement.TermInMonths > 0,
        "parties" => EditingAgreement.Borrowers.Any(b => !string.IsNullOrEmpty(b.EntityName)) &&
                        EditingAgreement.Lenders.Any(l => !string.IsNullOrEmpty(l.EntityName)),
        "property" => EditingAgreement.Properties.Any(p =>
                            !string.IsNullOrEmpty(p.FullAddress) || !string.IsNullOrEmpty(p.LegalDescription)),
        "fees" => true,
        "features" => true,
        _ => false
    };

    public void ToggleSection(string id)
    {
        if (OpenSections.Contains(id)) OpenSections.Remove(id);
        else OpenSections.Add(id);
        ActiveSection = id;
        OnPropertyChanged(nameof(OpenSections));
    }

    public void ScrollToSection(string id)
    {
        OpenSections.Add(id);
        ActiveSection = id;
        OnPropertyChanged(nameof(OpenSections));
    }

    public void SelectLoanType(string name)
    {
        EditingAgreement.LoanTypeName = name;
        OnPropertyChanged(nameof(EditingAgreement));
        OnPropertyChanged(nameof(CompletionPercent));
        TriggerAutoSave();
    }

    // ─────────────────────────────────────────────
    // ✅ BORROWER — UNIQUENESS ENFORCED
    // ─────────────────────────────────────────────

    /// <summary>
    /// Adds or updates a borrower in the LoanAgreement.Borrowers list.
    /// Enforces uniqueness within the loan by SSN (Individual), EIN (Entity),
    /// or Trust Name (Trust). Returns true on success, false if duplicate.
    /// </summary>
    [RelayCommand]
    private async Task UpsertAgreementBorrower(DominateDocsData.Models.Borrower r)
    {
        BorrowerError = null;

        // ✅ Enforce required identifying fields
        var requiredError = GetBorrowerRequiredFieldError(r);
        if (requiredError is not null)
        {
            BorrowerError = requiredError;
            return;
        }

        // ✅ Enforce uniqueness within this loan agreement
        bool duplicateInLoan = EditingAgreement.Borrowers.Any(b =>
            b.Id != r.Id && IsSameBorrower(b, r));

        if (duplicateInLoan)
        {
            BorrowerError = GetDuplicateBorrowerMessage(r);
            return;
        }

        int index = EditingAgreement.Borrowers.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Borrowers[index] = r;
        else
            EditingAgreement.Borrowers.Add(r);

        TriggerAutoSave();
    }

    [RelayCommand]
    private async Task DeleteAgreementBorrower(DominateDocsData.Models.Borrower r)
    {
        BorrowerError = null;
        int index = EditingAgreement.Borrowers.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Borrowers.RemoveAt(index);

        SelectedBorrower = null;
        TriggerAutoSave();
    }

    // ─────────────────────────────────────────────
    // BORROWER UNIQUENESS HELPERS
    // ─────────────────────────────────────────────

    private static bool IsSameBorrower(
        DominateDocsData.Models.Borrower a,
        DominateDocsData.Models.Borrower b)
    {
        return b.EntityType switch
        {
            Entity.Types.Individual =>
                a.EntityType == Entity.Types.Individual &&
                !string.IsNullOrWhiteSpace(a.SSN) &&
                NormalizeSSN(a.SSN) == NormalizeSSN(b.SSN),

            Entity.Types.Entity =>
                a.EntityType == Entity.Types.Entity &&
                !string.IsNullOrWhiteSpace(a.EIN) &&
                NormalizeEIN(a.EIN) == NormalizeEIN(b.EIN),

            Entity.Types.Trust =>
                a.EntityType == Entity.Types.Trust &&
                !string.IsNullOrWhiteSpace(a.EntityName) &&
                string.Equals(a.EntityName.Trim(), b.EntityName?.Trim(),
                    StringComparison.OrdinalIgnoreCase),

            _ => false
        };
    }

    private static string? GetBorrowerRequiredFieldError(DominateDocsData.Models.Borrower r)
    {
        return r.EntityType switch
        {
            Entity.Types.Individual when string.IsNullOrWhiteSpace(r.SSN)
                => "SSN is required for Individual borrowers.",
            Entity.Types.Entity when string.IsNullOrWhiteSpace(r.EIN)
                => "EIN is required for Entity borrowers.",
            Entity.Types.Trust when string.IsNullOrWhiteSpace(r.EntityName)
                => "Trust Name is required for Trust borrowers.",
            _ => null
        };
    }

    private static string GetDuplicateBorrowerMessage(DominateDocsData.Models.Borrower r)
    {
        return r.EntityType switch
        {
            Entity.Types.Individual => $"A borrower with SSN {r.SSN} is already on this loan.",
            Entity.Types.Entity => $"A borrower with EIN {r.EIN} is already on this loan.",
            Entity.Types.Trust => $"The trust \"{r.EntityName}\" is already on this loan.",
            _ => "This borrower is already on this loan."
        };
    }

    // Normalize SSN/EIN to digits-only for comparison so "123-45-6789" == "123456789"
    private static string NormalizeSSN(string? ssn) =>
        new string((ssn ?? "").Where(char.IsDigit).ToArray());

    private static string NormalizeEIN(string? ein) =>
        new string((ein ?? "").Where(char.IsDigit).ToArray());

    // ─────────────────────────────────────────────
    // LENDER
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task UpsertAgreementLender(DominateDocsData.Models.Lender r)
    {
        int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Lenders[index] = r;
        else
            EditingAgreement.Lenders.Add(r);

        TriggerAutoSave();
    }

    [RelayCommand]
    private async Task DeleteAgreementLender(DominateDocsData.Models.Lender r)
    {
        int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Lenders.RemoveAt(index);

        SelectedLender = null;
        TriggerAutoSave();
    }

    // ─────────────────────────────────────────────
    // BROKER
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task UpsertAgreementBroker(DominateDocsData.Models.Broker r)
    {
        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Brokers[index] = r;
        else
            EditingAgreement.Brokers.Add(r);

        TriggerAutoSave();
    }

    [RelayCommand]
    private async Task DeleteAgreementBroker(DominateDocsData.Models.Broker r)
    {
        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Brokers.RemoveAt(index);

        SelectedBroker = null;
        TriggerAutoSave();
    }

    // ─────────────────────────────────────────────
    // GUARANTOR
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task UpsertAgreementGuarantor(DominateDocsData.Models.Guarantor r)
    {
        int index = EditingAgreement.Guarantors.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Guarantors[index] = r;
        else
            EditingAgreement.Guarantors.Add(r);

        TriggerAutoSave();
    }

    [RelayCommand]
    private async Task DeleteAgreementGuarantor(DominateDocsData.Models.Guarantor r)
    {
        int index = EditingAgreement.Guarantors.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Guarantors.RemoveAt(index);

        SelectedGuarantor = null;
        TriggerAutoSave();
    }

    // ─────────────────────────────────────────────
    // PROPERTY
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task UpsertAgreementProperty(DominateDocsData.Models.PropertyRecord r)
    {
        int index = EditingAgreement.Properties.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Properties[index] = r;
        else
            EditingAgreement.Properties.Add(r);

        TriggerAutoSave();
    }

    [RelayCommand]
    private void DeleteAgreementProperty(DominateDocsData.Models.PropertyRecord r)
    {
        int index = EditingAgreement.Properties.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            EditingAgreement.Properties.RemoveAt(index);

        SelectedProperty = null;
        TriggerAutoSave();
    }

    // ─────────────────────────────────────────────
    // REMOVE HELPERS (index-based, used by UI loops)
    // ─────────────────────────────────────────────

    public void RemoveBorrower(int index)
    {
        if (index >= 0 && index < EditingAgreement.Borrowers.Count)
        {
            EditingAgreement.Borrowers.RemoveAt(index);
            NotifyLoanChanged();
        }
    }

    public void RemoveGuarantor(int index)
    {
        if (index >= 0 && index < EditingAgreement.Guarantors.Count)
        {
            EditingAgreement.Guarantors.RemoveAt(index);
            NotifyLoanChanged();
        }
    }

    public void RemoveLender(int index)
    {
        if (index >= 0 && index < EditingAgreement.Lenders.Count)
        {
            EditingAgreement.Lenders.RemoveAt(index);
            NotifyLoanChanged();
        }
    }

    public void RemoveBroker(int index)
    {
        if (index >= 0 && index < EditingAgreement.Brokers.Count)
        {
            EditingAgreement.Brokers.RemoveAt(index);
            NotifyLoanChanged();
        }
    }

    public void RemoveProperty(int index)
    {
        if (index >= 0 && index < EditingAgreement.Properties.Count)
        {
            EditingAgreement.Properties.RemoveAt(index);
            NotifyLoanChanged();
        }
    }

    // ─────────────────────────────────────────────
    // FEE / ASSIGNEE / THIRD PARTY
    // ─────────────────────────────────────────────

    public void AddFee(List<Fee> feeList) { feeList.Add(new Fee()); NotifyLoanChanged(); }
    public void RemoveFee(List<Fee> feeList, int index) { feeList.RemoveAt(index); NotifyLoanChanged(); }

    public void AddAssignee() { EditingAgreement.Assignees.Add(new DominateDocsData.Models.Assignee()); NotifyLoanChanged(); }
    public void RemoveAssignee(int index)
    {
        if (index >= 0 && index < EditingAgreement.Assignees.Count)
        {
            EditingAgreement.Assignees.RemoveAt(index);
            NotifyLoanChanged();
        }
    }

    public void AddThirdPartyOwner(PropertyRecord prop) { NotifyLoanChanged(); }
    public void RemoveThirdPartyOwner(PropertyRecord prop, int index) { NotifyLoanChanged(); }

    // ─────────────────────────────────────────────
    // VALIDATION
    // ─────────────────────────────────────────────

    public bool Validate()
    {
        var errors = new Dictionary<string, List<string>>();

        if (string.IsNullOrEmpty(EditingAgreement.LoanTypeName))
            errors["loanSetup"] = ["Select a loan type"];

        if (EditingAgreement.PrincipalAmount <= 0 ||
            EditingAgreement.InterestRate <= 0 ||
            EditingAgreement.TermInMonths <= 0)
            errors["loanTerms"] = ["Complete required loan terms"];

        if (!EditingAgreement.Borrowers.Any(b => !string.IsNullOrEmpty(b.EntityName)))
            errors["parties"] = ["Borrower name required"];

        if (!EditingAgreement.Properties.Any(p =>
                !string.IsNullOrEmpty(p.FullAddress) || !string.IsNullOrEmpty(p.LegalDescription)))
            errors["property"] = ["Property address required"];

        ValidationErrors = errors;
        if (errors.Count > 0)
            ScrollToSection(errors.Keys.First());

        return errors.Count == 0;
    }

    // ─────────────────────────────────────────────
    // AGREEMENT CRUD
    // ─────────────────────────────────────────────

    [RelayCommand]
    private async Task UpsertAgreement()
    {
        int index = AgreementList.FindIndex(x => x.Id == EditingAgreement.Id);
        if (index > -1)
            AgreementList[index] = EditingAgreement;
        else
            AgreementList.Add(EditingAgreement);

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
    }

    [RelayCommand]
    private async Task EditAgreement()
    {
        int index = AgreementList.FindIndex(x => x.Id == EditingAgreement.Id);
        if (index > -1)
            AgreementList[index] = EditingAgreement;

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
        SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private async Task DeleteAgreement(DominateDocsData.Models.LoanAgreement r)
    {
        int index = AgreementList.FindIndex(x => x.Id == r.Id);
        if (index > -1)
            AgreementList.RemoveAt(index);

        dbApp.DeleteRecord<DominateDocsData.Models.LoanAgreement>(r);
    }

    [RelayCommand]
    private void SelectAgreement(DominateDocsData.Models.LoanAgreement r)
    {
        SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void SelectBroker(DominateDocsData.Models.Broker r) => SelectedBroker = r;

    [RelayCommand]
    private void SelectBorrower(DominateDocsData.Models.Borrower r) => SelectedBorrower = r;

    [RelayCommand]
    private void SelectLender(DominateDocsData.Models.Lender r) => SelectedLender = r;

    [RelayCommand]
    private void SelectGuarantor(DominateDocsData.Models.Guarantor r) => SelectedGuarantor = r;

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedAgreement = null;
    }

    // ─────────────────────────────────────────────
    // GENERATE
    // ─────────────────────────────────────────────

    [RelayCommand]
    public async Task Generate()
    {
        if (!Validate()) return;
        IsGenerating = true;
        await Task.Delay(2800);
        IsGenerating = false;
        ShowGenerated = true;
    }

    public void BackToEdit() => ShowGenerated = false;

    public void Reset()
    {
        ShowGenerated = false;
        IsGenerating = false;
        ValidationErrors = [];
        BorrowerError = null;
        OnPropertyChanged(nameof(EditingAgreement));
        OnPropertyChanged(nameof(CompletionPercent));
    }

    // ─────────────────────────────────────────────
    // NEW RECORD
    // ─────────────────────────────────────────────

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingAgreement = new DominateDocsData.Models.LoanAgreement()
        {
            UserId = userId,
            LoanNumber = GenerateNewLoanNumber()
        };

        DominateDocsData.Models.UserProfile userProfile =
            dbApp.GetRecords<DominateDocsData.Models.UserProfile>()
                 .FirstOrDefault(x => x.UserId == userId);

        if (userProfile is not null)
        {
            EditingAgreement.InterestRate = userProfile.LoanDefaults.InterestRate;
            EditingAgreement.TermInMonths = userProfile.LoanDefaults.TermInMonths;
            EditingAgreement.AmorizationType = userProfile.LoanDefaults.AmorizationType;
            EditingAgreement.PrincipalAmount = userProfile.LoanDefaults.PrincipalAmount;
            EditingAgreement.RateType = userProfile.LoanDefaults.RateType;
            EditingAgreement.RepaymentSchedule = userProfile.LoanDefaults.RepaymentSchedule;

            if (userProfile.LoanDefaults.LenderId != Guid.Empty)
            {
                var r = dbApp.GetRecords<Lender>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.LenderId);
                if (r != null && EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id) == -1)
                    EditingAgreement.Lenders.Add(r);
            }

            if (userProfile.LoanDefaults.BrokerId != Guid.Empty)
            {
                var r = dbApp.GetRecords<DominateDocsData.Models.Broker>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.BrokerId);
                if (r != null && EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id) == -1)
                    EditingAgreement.Brokers.Add(r);
            }

            if (userProfile.LoanDefaults.ServicerId != Guid.Empty)
            {
                var r = dbApp.GetRecords<DominateDocsData.Models.Servicer>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.ServicerId);
                if (r != null && EditingAgreement.Servicers.FindIndex(x => x.Id == r.Id) == -1)
                    EditingAgreement.Servicers.Add(r);
            }

            if (userProfile.LoanDefaults.UserType is not null)
                EditingAgreement.UserType = userProfile.LoanDefaults.UserType;

            if (userProfile.LoanDefaults.LoanTypeId != Guid.Empty)
            {
                EditingAgreement.LoanTypeId = userProfile.LoanDefaults.LoanTypeId;
                EditingAgreement.LoanTypeName = userProfile.LoanDefaults.LoanTypeName;
            }
        }
    }

    [RelayCommand]
    private void SelectLoanType(DominateDocsData.Models.LoanType lt) => SelectedLoanType = lt;

    [RelayCommand]
    private void NextStep()
    {
        if (ActiveStepIndex == 0 && !CanGoNextFromLoanTypeStep) return;
        if (ActiveStepIndex < 2) ActiveStepIndex++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (ActiveStepIndex > 0) ActiveStepIndex--;
    }

    // ─────────────────────────────────────────────
    // NOTIFY / AUTOSAVE
    // ─────────────────────────────────────────────

    public void NotifyLoanChanged()
    {
        OnPropertyChanged(nameof(CompletionPercent));
        TriggerAutoSave();
    }

    private async void TriggerAutoSave()
    {
        autoSaveCts?.Cancel();
        autoSaveCts = new CancellationTokenSource();
        AutoSaveStatus = "saving";
        try
        {
            dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
            AutoSaveStatus = "saved";
        }
        catch (TaskCanceledException) { }
    }

    // ─────────────────────────────────────────────
    // LOAN NUMBER
    // ─────────────────────────────────────────────

    public string GenerateNewLoanNumber()
    {
        AgreementList.Clear();
        AgreementList = new ObservableCollection<DominateDocsData.Models.LoanAgreement>(
            dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().Where(x => x.UserId == userId));

        if (AgreementList.Count > 0)
            nextLoanNumber = AgreementList.Max(x => Convert.ToInt32(x.LoanNumber.Substring(8)));

        nextLoanNumber++;
        return $"LN-{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{nextLoanNumber}";
    }

    // ─────────────────────────────────────────────
    // FINANCIAL HELPERS
    // ─────────────────────────────────────────────

    public decimal EstimatedDownPayment =>
        Math.Round(EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m), 2);

    public DateOnly? GetLoanMaturityDate(int termsInMonths)
    {
        if (termsInMonths == 0) return MaturityDate;

        var date = EditingAgreement.SignedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
        MaturityDate = DateOnly.FromDateTime(date.AddMonths(termsInMonths));
        return MaturityDate;
    }

    public DateOnly GetBalloonDate(int termsInMonths)
    {
        var date = EditingAgreement.SignedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Now;
        if (termsInMonths != 0)
            EditingAgreement.BalloonPayments.DueDate = DateOnly.FromDateTime(date.AddMonths(termsInMonths));
        return EditingAgreement.BalloonPayments.DueDate;
    }

    // ─────────────────────────────────────────────
    // SYNC / PARTIAL PROPERTY HANDLERS
    // ─────────────────────────────────────────────

    partial void OnEditingAgreementChanged(DominateDocsData.Models.LoanAgreement value) => SyncFromEditingAgreement();

    private void SyncFromEditingAgreement()
    {
        if (EditingAgreement is null) return;

        PrincipalAmount = EditingAgreement.PrincipalAmount;
        InterestRate = EditingAgreement.InterestRate;
        TermInMonths = EditingAgreement.TermInMonths;
        AmorizationType = EditingAgreement.AmorizationType;
        RepaymentSchedule = EditingAgreement.RepaymentSchedule;
        MaturityDate = EditingAgreement.MaturityDate;
        BalloonTermMonths = EditingAgreement.BalloonPayments.BalloonTermMonths;
        BalloonAmount = EditingAgreement.BalloonPayments.BalloonAmount;
        PayBalloonSchedule = EditingAgreement.BalloonPayments;
        InitialMargin = EditingAgreement.InitialMargin;
        DownPaymentPercentage = EditingAgreement.DownPaymentPercentage;
        EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);
        RateType = EditingAgreement.RateType;
        FixedPaymentSchedule = EditingAgreement.FixedPaymentSchedule;
    }

    partial void OnPrincipalAmountChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.PrincipalAmount = value;
        RecomputeScheduleFromAgreement();
    }

    partial void OnInterestRateChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.InterestRate = value;
        RecomputeScheduleFromAgreement();
    }

    partial void OnTermInMonthsChanged(int value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.TermInMonths = value;
        RecomputeScheduleFromAgreement();
    }

    partial void OnAmorizationTypeChanged(DominateDocsData.Enums.Payment.AmortizationTypes value)
    {
        if (EditingAgreement is null) return;
        if (value == Payment.AmortizationTypes.PartiallyAmortized || value == Payment.AmortizationTypes.Other)
            EditingAgreement.IsBalloonPayment = true;
        EditingAgreement.AmorizationType = value;
        RecomputeScheduleFromAgreement();
    }

    partial void OnRepaymentScheduleChanged(DominateDocsData.Enums.Payment.Schedules value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.RepaymentSchedule = value;
        RecomputeScheduleFromAgreement();
    }

    partial void OnRateTypeChanged(DominateDocsData.Enums.Payment.RateTypes value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.RateType = value;
        RecomputeScheduleFromAgreement();
    }

    partial void OnRateIndexChanged(DominateDocsData.Enums.Payment.RateIndexes value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.RateIndex = value;
        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
            RecomputeScheduleFromAgreement();
    }

    partial void OnAdjustmentIntervalChanged(DominateDocsData.Enums.Payment.Schedules value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.AdjustmentInterval = value;
        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
            RecomputeScheduleFromAgreement();
    }

    partial void OnAssumedIndexPathChanged(DominateDocsData.Enums.Payment.IndexPaths value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.AssumedIndexPath = value;
        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
            RecomputeScheduleFromAgreement();
    }

    partial void OnInitialMarginChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.InitialMargin = value;
    }

    partial void OnDownPaymentPercentageChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.DownPaymentPercentage = value;
    }

    partial void OnMaturityDateChanged(DateOnly? value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.MaturityDate = value;
    }

    partial void OnPayBalloonScheduleChanged(DominateDocsData.Models.BalloonPayments value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments = value;
    }

    partial void OnPayScheduleChanged(DominateDocsData.Models.PaymentSchedule value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.PaymentSchedule = value;
    }

    partial void OnFixedPaymentScheduleChanged(DominateDocsData.Models.PaymentSchedule value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.FixedPaymentSchedule = value;
    }

    partial void OnBalloonAmountChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments.BalloonAmount = value;
        RecomputeBalloonSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate,
            EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    partial void OnBalloonTermMonthsChanged(int value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments.BalloonTermMonths = value;
        RecomputeBalloonSchedule(value, EditingAgreement.InterestRate,
            EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    // ─────────────────────────────────────────────
    // SCHEDULE COMPUTATION
    // ─────────────────────────────────────────────

    private void RecomputeScheduleFromAgreement()
    {
        if (EditingAgreement is null) return;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate,
                EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType,
                EditingAgreement.RateChangeList);
        else
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate,
                EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    private void RecomputeSchedule(int termsInMonths, decimal interestRate,
        Payment.Schedules paymentSchedule, Payment.AmortizationTypes amortizationType,
        List<DominateDocsData.Models.RateChange>? rateChangeList = null)
    {
        try
        {
            if (EditingAgreement is null) { CurrentSchedule = new(); return; }

            var start = EditingAgreement.SignedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
            var end = EditingAgreement.MaturityDate?.ToDateTime(TimeOnly.MinValue)
                     ?? EditingAgreement.OriginationDate?.ToDateTime(TimeOnly.MinValue).AddMonths(termsInMonths > 0 ? termsInMonths : 0)
                     ?? DateTime.Today;

            if (EditingAgreement.PrincipalAmount <= 0 || termsInMonths <= 0 || start >= end)
            {
                CurrentSchedule = new();
                if (EditingAgreement.RateType == Payment.RateTypes.Variable)
                    EditingAgreement.PaymentSchedule = CurrentSchedule;
                else
                    EditingAgreement.FixedPaymentSchedule = CurrentSchedule;
                return;
            }

            if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
            {
                var schedule = loanScheduler.GenerateFixed(
                    principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                    annualRatePercent: interestRate,
                    downPaymentPercent: EditingAgreement.DownPaymentPercentage,
                    startDate: start,
                    endDate: end,
                    amortizationType: amortizationType,
                    amortizationTermMonths: termsInMonths);
                EditingAgreement.FixedPaymentSchedule = schedule ?? new();
            }
            else
            {
                var schedule = loanScheduler.GenerateVariable(
                    principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                    downPaymentPercent: EditingAgreement.DownPaymentPercentage,
                    startDate: start,
                    endDate: end,
                    amortizationType: amortizationType,
                    rateSchedule: rateChangeList,
                    amortizationTermMonths: termsInMonths);
                EditingAgreement.PaymentSchedule = schedule ?? new();
            }
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    private void RecomputeBalloonSchedule(int termsInMonths, decimal interestRate,
        Payment.Schedules paymentSchedule, Payment.AmortizationTypes amortizationType)
    {
        try
        {
            if (EditingAgreement is null) { CurrentBalloonSchedule = new(); return; }

            var start = EditingAgreement.SignedDate?.ToDateTime(TimeOnly.MinValue) ?? DateTime.Today;
            var end = EditingAgreement.MaturityDate?.ToDateTime(TimeOnly.MinValue)
                     ?? EditingAgreement.OriginationDate?.ToDateTime(TimeOnly.MinValue).AddMonths(termsInMonths > 0 ? termsInMonths : 0)
                     ?? DateTime.Today;

            if (EditingAgreement.PrincipalAmount <= 0 || termsInMonths <= 0 || start >= end) return;

            DateOnly firstPayment = EditingAgreement.SignedDate ?? DateOnly.FromDateTime(DateTime.Now);

            var schedule = balloonPaymentCalculater.Generate(
                principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                annualRatePercent: InterestRate,
                amortizationTermMonths: TermInMonths,
                balloonTermMonths: BalloonTermMonths,
                firstPaymentDate: firstPayment.AddMonths(1),
                paymentsPerYear: 12);

            PayBalloonSchedule = schedule ?? new();
            PayBalloonSchedule.DueDate = GetBalloonDate(BalloonTermMonths);
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    // ─────────────────────────────────────────────
    // BILLING / PIPELINE
    // ─────────────────────────────────────────────

    private async System.Threading.Tasks.Task<(bool ok, string reason)> EnforceBillingForDocGenerationAsync(System.Guid userId, System.Guid loanId)
    {
        DominateDocsData.Models.UserProfile? profile =
            System.Linq.Enumerable.FirstOrDefault(
                this.dbApp.GetRecords<DominateDocsData.Models.UserProfile>(),
                x => x.UserId == userId);

        if (profile is null)
            return (false, "UserProfile missing. Billing cannot be validated.");

        profile.Billing ??= new DominateDocsData.Models.UserProfile.BillingAccount();
        profile.BillingEvents ??= new System.Collections.Generic.List<DominateDocsData.Models.UserProfile.BillingEventRecord>();
        profile.BillingCharges ??= new System.Collections.Generic.List<DominateDocsData.Models.UserProfile.BillingChargeRecord>();

        profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
        {
            EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.DocumentGenerationAttempted,
            UserId = userId,
            LoanId = loanId,
            Message = "User initiated doc generation pipeline."
        });

        if (profile.Billing.IsAccountDisabled)
        {
            profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
            {
                EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.DocumentGenerationBlockedAccountDisabled,
                UserId = userId,
                LoanId = loanId,
                Message = $"Blocked: account disabled. Reason={profile.Billing.DisabledReason ?? "<​none>"}"
            });
            await this.dbApp.UpSertRecordAsync<DominateDocsData.Models.UserProfile>(profile).ConfigureAwait(false);
            return (false, "Account disabled.");
        }

        var nowUtc = System.DateTime.UtcNow;
        var bypassSub = profile.Billing.BypassSubscriptionCharges;
        var validUntil = profile.Billing.SubscriptionValidUntilUtc;

        profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
        {
            EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.SubscriptionStatusChecked,
            UserId = userId,
            LoanId = loanId,
            Message = $"Checked subscription. Bypass={bypassSub} ValidUntilUtc={(validUntil.HasValue ? validUntil.Value.ToString("u") : "<​null>")}"
        });

        if (!bypassSub)
        {
            if (!validUntil.HasValue || validUntil.Value <= nowUtc)
            {
                profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
                {
                    EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.SubscriptionExpiredBlocked,
                    UserId = userId,
                    LoanId = loanId,
                    Message = "Blocked: subscription expired or missing."
                });
                await this.dbApp.UpSertRecordAsync<DominateDocsData.Models.UserProfile>(profile).ConfigureAwait(false);
                return (false, "Subscription expired. Renew to generate documents.");
            }
        }
        else
        {
            profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
            {
                EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.SubscriptionBypassedAllowed,
                UserId = userId,
                LoanId = loanId,
                Message = "Subscription bypass active."
            });
        }

        profile.Billing.LoanStates ??= new System.Collections.Generic.List<DominateDocsData.Models.UserProfile.LoanBillingState>();
        var state = System.Linq.Enumerable.FirstOrDefault(profile.Billing.LoanStates, x => x.LoanId == loanId);
        if (state is null) { state = new DominateDocsData.Models.UserProfile.LoanBillingState { LoanId = loanId }; profile.Billing.LoanStates.Add(state); }

        var bypassProc = profile.Billing.BypassDocumentProcessingCharges;

        if (!state.ProcessingFeeSatisfied)
        {
            if (bypassProc)
            {
                state.ProcessingFeeSatisfied = true;
                state.FirstSatisfiedAtUtc = nowUtc;
                profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
                {
                    EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.DocumentProcessingBypassedFirstTime,
                    UserId = userId,
                    LoanId = loanId,
                    Message = "First-time $200 processing fee bypassed by admin flag."
                });
                profile.BillingCharges.Add(new DominateDocsData.Models.UserProfile.BillingChargeRecord
                {
                    ChargeType = DominateDocsData.Models.UserProfile.BillingChargeTypes.DocumentProcessingPerLoan,
                    Status = DominateDocsData.Models.UserProfile.BillingChargeStatus.Bypassed,
                    Amount = 200m,
                    Currency = "USD",
                    UserId = userId,
                    LoanId = loanId,
                    Notes = "Bypassed first-time processing fee."
                });
            }
            else
            {
                profile.BillingCharges.Add(new DominateDocsData.Models.UserProfile.BillingChargeRecord
                {
                    ChargeType = DominateDocsData.Models.UserProfile.BillingChargeTypes.DocumentProcessingPerLoan,
                    Status = DominateDocsData.Models.UserProfile.BillingChargeStatus.Pending,
                    Amount = 200m,
                    Currency = "USD",
                    UserId = userId,
                    LoanId = loanId,
                    Notes = "Payment required before first doc generation for this loan."
                });
                profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
                {
                    EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.DocumentProcessingChargedFirstTime,
                    UserId = userId,
                    LoanId = loanId,
                    Message = "Blocked: first-time $200 processing fee not paid yet (recorded as pending)."
                });
                await this.dbApp.UpSertRecordAsync<DominateDocsData.Models.UserProfile>(profile).ConfigureAwait(false);
                return (false, "Payment required: $200 processing fee for first-time document generation on this loan.");
            }
        }
        else
        {
            profile.BillingEvents.Add(new DominateDocsData.Models.UserProfile.BillingEventRecord
            {
                EventType = DominateDocsData.Models.UserProfile.BillingEventTypes.DocumentGenerationFreeRepeat,
                UserId = userId,
                LoanId = loanId,
                Message = "Repeat generation: free."
            });
        }

        state.GenerationCount++;
        state.LastGeneratedAtUtc = nowUtc;
        await this.dbApp.UpSertRecordAsync<DominateDocsData.Models.UserProfile>(profile).ConfigureAwait(false);
        return (true, "Billing OK.");
    }

    public async Task ProcessDocsMergeEmailAsync()
    {
        if (EditingAgreement is null || EditingAgreement.Id == Guid.Empty)
        {
            LastPipelineStatus = "No loan loaded.";
            return;
        }

        var loanId = EditingAgreement.Id;
        DominateDocsData.Models.LoanAgreement? freshLoan = null;
        string? to = null;
        string? originalEmailTo = null;

        try
        {
            freshLoan = dbApp.GetRecordById<DominateDocsData.Models.LoanAgreement>(loanId);
            if (freshLoan is null) { LastPipelineStatus = "Loan not found in DB."; return; }

            var billingGate = await EnforceBillingForDocGenerationAsync(this.userId, loanId).ConfigureAwait(false);
            if (!billingGate.ok) { LastPipelineStatus = billingGate.reason; return; }

            if (freshLoan.AdminBench != null)
            {
                var enabledProp = freshLoan.AdminBench.GetType().GetProperty("Enabled");
                if (enabledProp?.CanWrite == true && enabledProp.PropertyType == typeof(bool))
                    enabledProp.SetValue(freshLoan.AdminBench, false);
                freshLoan.AdminBench = null;
            }

            if (EditingAgreement.LoanTypeId != Guid.Empty)
            {
                freshLoan.LoanTypeId = EditingAgreement.LoanTypeId;
                freshLoan.LoanTypeName = EditingAgreement.LoanTypeName;
            }

            to = ResolveEmailTo(freshLoan);
            if (string.IsNullOrWhiteSpace(to)) { LastPipelineStatus = "No email address found on the loan (EmailTo)."; return; }

            originalEmailTo = freshLoan.EmailTo;
            freshLoan.EmailTo = null;
            await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(freshLoan).ConfigureAwait(false);

            LastPipelineStatus = "Queued evaluation + merge pipeline…";
            await loanQueue.EnqueueAsync(new DocumentManager.Jobs.LoanJob(freshLoan), CancellationToken.None).ConfigureAwait(false);

            var expectedCount = await WaitForDeliveriesCountAsync(loanId, timeoutSeconds: 25).ConfigureAwait(false);
            LastPipelineStatus = expectedCount > 0
                ? $"Waiting for merges to finish… (expected {expectedCount})"
                : "Waiting for merges to finish… (delivery count not yet visible)";

            var mergesOk = await WaitForMergesCompleteAsync(loanId, expectedCount > 0 ? expectedCount : 1, timeoutSeconds: 150).ConfigureAwait(false);
            if (!mergesOk)
                logger.LogWarning("ProcessDocsMergeEmailAsync: merge wait timed out. LoanId={LoanId}", loanId);

            var subject = $"Loan Documents: {freshLoan.ReferenceName ?? "Loan"} | Deliveries={expectedCount}";
            await emailQueue.EnqueueAsync(
                new DocumentManager.Jobs.EmailJob(loanId, to, subject, DocumentManager.Email.EmailEnums.AttachmentOutput.ZipFile, ZipMaxWaitSeconds: 45),
                CancellationToken.None).ConfigureAwait(false);

            LastPipelineStatus = mergesOk
                ? $"Queued ZIP email to {to} with {expectedCount} document(s)."
                : $"Queued ZIP email to {to}. (Merge wait timed out; ZIP may be incomplete.)";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessDocsMergeEmailAsync failed for LoanId={LoanId}", loanId);
            LastPipelineStatus = "Pipeline failed. Check logs.";
        }
        finally
        {
            if (freshLoan is not null)
            {
                try
                {
                    freshLoan.EmailTo = originalEmailTo;
                    await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(freshLoan).ConfigureAwait(false);
                }
                catch (Exception restoreEx)
                {
                    logger.LogWarning(restoreEx, "Failed to restore EmailTo on LoanId={LoanId}", loanId);
                }
            }
        }
    }

    private async Task<bool> WaitForMergesCompleteAsync(Guid loanId, int expectedCount, int timeoutSeconds)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < stopAt)
        {
            var merges = docState.DocumentList.Values
                .Where(m => m?.LoanAgreement?.Id == loanId).ToList();

            if (merges.Count == 0) { await Task.Delay(300).ConfigureAwait(false); continue; }

            var anyRunning = merges.Any(m => m is not null &&
                (m.Status == DocumentMergeState.Status.Queued || m.Status == DocumentMergeState.Status.Submittied));

            var completedWithBytes = merges.Count(m => m is not null &&
                m.Status == DocumentMergeState.Status.Complete &&
                m.MergedDocumentBytes is not null && m.MergedDocumentBytes.Length > 0);

            if (!anyRunning && completedWithBytes >= expectedCount) return true;
            await Task.Delay(400).ConfigureAwait(false);
        }
        return false;
    }

    private async Task<int> WaitForDeliveriesCountAsync(Guid loanId, int timeoutSeconds)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < stopAt)
        {
            try
            {
                var fresh = dbApp.GetRecordById<DominateDocsData.Models.LoanAgreement>(loanId);
                var count = fresh?.DocumentDeliverys?.Count ?? 0;
                if (count > 0) return count;
            }
            catch { }
            await Task.Delay(250).ConfigureAwait(false);
        }
        return 0;
    }

    private string ResolveEmailTo(DominateDocsData.Models.LoanAgreement loan)
    {
        if (!string.IsNullOrWhiteSpace(loan.EmailTo)) return loan.EmailTo.Trim();
        return userSession?.Email?.Trim() ?? "";
    }
}