using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentManager.CalculatorsSchedulers;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsSite.Services;
using DominateDocsSite.State;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardNEWViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>? loanTypes = new();

    [ObservableProperty]
    private DominateDocsData.Models.Borrower selectedBorrower = null;

    [ObservableProperty]
    private DominateDocsData.Models.Broker selectedBroker = null;

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor selectedGuarantor = null;

    [ObservableProperty]
    private string? lastPipelineStatus;

    [ObservableProperty]
    private DominateDocsData.Models.Lender selectedLender = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord selectedProperty = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.PaymentSchedule? currentSchedule = new();

    [ObservableProperty]
    private DominateDocsData.Models.BalloonPayments currentBalloonSchedule = new();

    // Mirror fields bound by the UI (keep names obvious)
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
    [ObservableProperty] private DateTime? maturityDate;
    [ObservableProperty] private DominateDocsData.Models.PaymentSchedule paySchedule;
    [ObservableProperty] private DominateDocsData.Models.BalloonPayments payBalloonSchedule;
    [ObservableProperty] private DominateDocsData.Models.PaymentSchedule fixedPaymentSchedule;

    //[ObservableProperty] private DominateDocsData.Models.Loan _loan = new();
    [ObservableProperty] private string _activeSection = "loanSetup";
    [ObservableProperty] private HashSet<string> _openSections = ["loanSetup"];
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private bool _showGenerated;
    [ObservableProperty] private string _autoSaveStatus = "saved";
    [ObservableProperty] private Dictionary<string, List<string>> _validationErrors = [];

    private Guid userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanWizardNEWViewModel> logger;

   

    private DashboardViewModel dvm;
    private IDocumentManagerState docState;
    private IJobQueue<LoanJob> loanQueue;
    private IJobQueue<EmailJob> emailQueue;
    private ILoanScheduler loanScheduler;
    private IBalloonPaymentCalculater balloonPaymentCalculater;
    private IFetchCurrentIndexRatesAndSchedulesService indexRates;

    private int nextLoanNumber = 0;

    public LoanWizardNEWViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanWizardNEWViewModel> logger, UserSession userSession, IApplicationStateManager appState, DashboardViewModel dvm, IDocumentManagerState docState, ILoanScheduler loanScheduler, IBalloonPaymentCalculater balloonPaymentCalculater, IFetchCurrentIndexRatesAndSchedulesService indexRates, IJobQueue<LoanJob> loanQueue,
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

        LoanTypes = new ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>(dbApp.GetRecords<DominateDocsData.Models.LoanType>().Select(x => new DominateDocsData.Models.DTOs.LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)));

    }
          

    public static readonly (string Id, string Label, string Icon)[] Sections =
    [
        ("loanSetup", "Loan Type", "flash_on"),
        ("loanTerms", "Loan Terms", "attach_money"),
        ("parties", "Parties", "people"),
        ("property", "Property", "home"),
        ("fees", "Fees", "receipt_long"),
        ("features", "Loan Features", "settings")
    ];

    public int CompletionPercent
    {
        get
        {
            var checks = new[]
            {
                !string.IsNullOrEmpty(EditingAgreement.LoanTypeName),
                !string.IsNullOrEmpty(EditingAgreement.PrincipalAmount.ToString()) && !string.IsNullOrEmpty(EditingAgreement.InterestRate.ToString()),
                EditingAgreement.Borrowers.Any(b => !string.IsNullOrEmpty(b.EntityName)) && EditingAgreement.Lenders.Any(l => !string.IsNullOrEmpty(l.EntityName)),
                EditingAgreement.Properties.Any(p => !string.IsNullOrEmpty(p.FullAddress) || !string.IsNullOrEmpty(p.LegalDescription)),
                true, // fees always valid
                true  // features always valid
            };
            return (int)(checks.Count(c => c) / (double)checks.Length * 100);
        }
    }

    public bool IsSectionComplete(string sectionId) => sectionId switch
    {
        "loanSetup" => !string.IsNullOrEmpty(EditingAgreement.LoanTypeName),
        "loanTerms" => !string.IsNullOrEmpty(EditingAgreement.PrincipalAmount.ToString()) && !string.IsNullOrEmpty(EditingAgreement.InterestRate.ToString()) && !string.IsNullOrEmpty(EditingAgreement.TermInMonths.ToString()),
        "parties" => EditingAgreement.Borrowers.Any(b => !string.IsNullOrEmpty(b.EntityName)) && EditingAgreement.Lenders.Any(l => !string.IsNullOrEmpty(l.EntityName)),
        "property" => EditingAgreement.Properties.Any(p => !string.IsNullOrEmpty(p.FullAddress) || !string.IsNullOrEmpty(p.LegalDescription)),
        "fees" => true,
        "features" => true,
        _ => false
    };

    public void ToggleSection(string id)
    {
        if (OpenSections.Contains(id))
            OpenSections.Remove(id);
        else
            OpenSections.Add(id);

        ActiveSection = id;
        OnPropertyChanged(nameof(OpenSections));
    }

    public void ScrollToSection(string id)
    {
        OpenSections.Add(id);
        ActiveSection = id;
        OnPropertyChanged(nameof(OpenSections));
    }

    public void SelectLoanType(string type)
    {
        EditingAgreement.LoanTypeName = type;
        OnPropertyChanged(nameof(EditingAgreement));
        OnPropertyChanged(nameof(CompletionPercent));
        TriggerAutoSave();
    }

    // ── Party management ──
    public void AddBorrower() { EditingAgreement.Borrowers.Add(new DominateDocsData.Models.Borrower()); NotifyLoanChanged(); }
    public void RemoveBorrower(int index) { if (EditingAgreement.Borrowers.Count > 1) { EditingAgreement.Borrowers.RemoveAt(index); NotifyLoanChanged(); } }
    public void AddGuarantor() { EditingAgreement.Guarantors.Add(new DominateDocsData.Models.Guarantor { EntityType = Entity.Types.Individual }); NotifyLoanChanged(); }
    public void RemoveGuarantor(int index) { if (EditingAgreement.Guarantors.Count > 1) { EditingAgreement.Guarantors.RemoveAt(index); NotifyLoanChanged(); } }
    public void AddLender() { EditingAgreement.Lenders.Add(new Lender()); NotifyLoanChanged(); }
    public void RemoveLender(int index) { if (EditingAgreement.Lenders.Count > 1) { EditingAgreement.Lenders.RemoveAt(index); NotifyLoanChanged(); } }
    public void AddBroker() { EditingAgreement.Brokers.Add(new DominateDocsData.Models.Broker()); NotifyLoanChanged(); }
    public void RemoveBroker(int index) { if (EditingAgreement.Brokers.Count > 1) { EditingAgreement.Brokers.RemoveAt(index); NotifyLoanChanged(); } }

    // ── Property management ──
    public void AddProperty() { EditingAgreement.Properties.Add(new PropertyRecord()); NotifyLoanChanged(); }
    public void RemoveProperty(int index) { if (EditingAgreement.Properties.Count > 1) { EditingAgreement.Properties.RemoveAt(index); NotifyLoanChanged(); } }

    // ── Fee management ──
    public void AddFee(List<Fee> feeList) { feeList.Add(new Fee()); NotifyLoanChanged(); }
    public void RemoveFee(List<Fee> feeList, int index) { feeList.RemoveAt(index); NotifyLoanChanged(); }

    // ── Assignee management ──
    public void AddAssignee() { EditingAgreement.Assignees.Add(new DominateDocsData.Models.Assignee()); NotifyLoanChanged(); }
    public void RemoveAssignee(int index) { if (EditingAgreement.Assignees.Count > 1) { EditingAgreement.Assignees.RemoveAt(index); NotifyLoanChanged(); } }

    // ── Third party owner management ──
    public void AddThirdPartyOwner(PropertyRecord prop) { prop.EntityOwners.Add(new DominateDocsData.Models.EntityOwner()); NotifyLoanChanged(); }
    public void RemoveThirdPartyOwner(PropertyRecord prop, int index) { prop.EntityOwners.RemoveAt(index); NotifyLoanChanged(); }

    // ── Signatory management helpers ──
    public void AddSignatory(List<SigningAuthority> list) { list.Add(new SigningAuthority()); NotifyLoanChanged(); }
    public void RemoveSignatory(List<SigningAuthority> list, int index) { list.RemoveAt(index); NotifyLoanChanged(); }
    public void AddAlias(List<AkaName> list) { list.Add(new AkaName()); NotifyLoanChanged(); }
    public void RemoveAlias(List<AkaName> list, int index) { list.RemoveAt(index); NotifyLoanChanged(); }

    // ── Entity owner management ──
    public void AddOwner(List<DominateDocsData.Models.EntityOwner> list) { list.Add(new DominateDocsData.Models.EntityOwner()); NotifyLoanChanged(); }
    public void RemoveOwner(List<DominateDocsData.Models.EntityOwner> list, int index) { list.RemoveAt(index); NotifyLoanChanged(); }

    // ── Validation ──
    public bool Validate()
    {
        var errors = new Dictionary<string, List<string>>();
        if (!String.IsNullOrEmpty(EditingAgreement.LoanTypeName)) errors["loanSetup"] = ["Select a loan type"];
        if (string.IsNullOrEmpty(EditingAgreement.PrincipalAmount.ToString()) || string.IsNullOrEmpty(EditingAgreement.InterestRate.ToString()) || string.IsNullOrEmpty(EditingAgreement.TermInMonths.ToString()))
            errors["loanTerms"] = ["Complete required loan terms"];
        if (!EditingAgreement.Borrowers.Any(b => !string.IsNullOrEmpty(b.EntityName)))
            errors["parties"] = ["Borrower name required"];
        if (!EditingAgreement.Properties.Any(p => !string.IsNullOrEmpty(p.FullAddress) || !string.IsNullOrEmpty(p.LegalDescription)))
            errors["property"] = ["Property address required"];

        ValidationErrors = errors;
        if (errors.Count > 0)
            ScrollToSection(errors.Keys.First());

        return errors.Count == 0;
    }

    [RelayCommand]
    public async Task Generate()
    {
        if (!Validate()) return;
        IsGenerating = true;
        await Task.Delay(2800); // simulate generation
        IsGenerating = false;
        ShowGenerated = true;
    }

    public void BackToEdit() => ShowGenerated = false;

    public void Reset()
    {
        GetNewRecord();
        ShowGenerated = false;
        IsGenerating = false;
        ValidationErrors = [];
        OnPropertyChanged(nameof(EditingAgreement));
        OnPropertyChanged(nameof(CompletionPercent));
    }

    public void NotifyLoanChanged()
    {
        OnPropertyChanged(nameof(Loan));
        OnPropertyChanged(nameof(CompletionPercent));
        TriggerAutoSave();
    }

    private CancellationTokenSource? _autoSaveCts;
    private async void TriggerAutoSave()
    {
        _autoSaveCts?.Cancel();
        _autoSaveCts = new CancellationTokenSource();
        AutoSaveStatus = "saving";
        try
        {
            await Task.Delay(1200, _autoSaveCts.Token);
            dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
            //_loanService.Save(Loan);
            AutoSaveStatus = "saved";
        }
        catch (TaskCanceledException) { }
    }

    public string GenerateNewLoanNumberAsync()
    {
        nextLoanNumber++;
        string loanNumberPrefix = "LN-";
        string uniqueIdentifier = $"{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{nextLoanNumber}";

        
        return $"{loanNumberPrefix}{uniqueIdentifier}";
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingAgreement = new DominateDocsData.Models.LoanAgreement()
        {
            UserId = userId,
            LoanNumber = GenerateNewLoanNumberAsync()
        };

        DominateDocsData.Models.UserProfile userProfile = dbApp.GetRecords<DominateDocsData.Models.UserProfile>().FirstOrDefault(x => x.UserId == userId);

        if (userProfile is not null)
        {
            EditingAgreement.InterestRate = userProfile.LoanDefaults.InterestRate;
            EditingAgreement.TermInMonths = userProfile.LoanDefaults.TermInMonths;
            EditingAgreement.AmorizationType = userProfile.LoanDefaults.AmorizationType;
            EditingAgreement.PrincipalAmount = userProfile.LoanDefaults.PrincipalAmount;
            EditingAgreement.RateType = userProfile.LoanDefaults.RateType;
            EditingAgreement.RepaymentSchedule = userProfile.LoanDefaults.RepaymentSchedule;

            //  r = Record

            if (userProfile.LoanDefaults.LenderId != Guid.Empty)
            {
                Lender r = dbApp.GetRecords<Lender>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.LenderId);
                if (r != null)
                {
                    int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);

                    if (index == -1) EditingAgreement.Lenders.Add(r);
                }
            }

            if (userProfile.LoanDefaults.BrokerId != Guid.Empty)
            {
                DominateDocsData.Models.Broker r = dbApp.GetRecords<DominateDocsData.Models.Broker>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.BrokerId);

                if (r != null)
                {
                    int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);

                    if (index == -1) EditingAgreement.Brokers.Add(r);
                }
            }

            if (userProfile.LoanDefaults.ServicerId != Guid.Empty)
            {
                DominateDocsData.Models.Servicer r = dbApp.GetRecords<DominateDocsData.Models.Servicer>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.ServicerId);

                if (r != null)
                {
                    int index = EditingAgreement.Servicers.FindIndex(x => x.Id == r.Id);

                    if (index == -1) EditingAgreement.Servicers.Add(r);
                }
            }

            if (userProfile.LoanDefaults.OtherId != Guid.Empty)
            {
                //Lender Lender = dbApp.GetRecords<Lender>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.LenderId);
                //if (Lender != null)
                //{
                //    int index = EditingAgreement.Lenders.FindIndex(x => x.Id == Lender.Id);

                //    if (index == -1) EditingAgreement.Lenders.Add(Lender);
                //}
            }

            if (userProfile.LoanDefaults.UserType is not null) EditingAgreement.UserType = userProfile.LoanDefaults.UserType;

            if (userProfile.LoanDefaults.LoanTypeId != Guid.Empty)
            {
                EditingAgreement.LoanTypeId = userProfile.LoanDefaults.LoanTypeId;
                EditingAgreement.LoanTypeName = userProfile.LoanDefaults.LoanTypeName;

            }

        }



    }
}
