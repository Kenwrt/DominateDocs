using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Models;
using DominateDocsSite.Models.Enums;
using DominateDocsSite.Services;

namespace DominateDocsSite.ViewModels;

public partial class LoanWizardViewModelNew : ObservableObject
{
    private readonly ILoanService _loanService;
    private readonly ISeedDataService _seedData;

    public LoanWizardViewModelNew(ILoanService loanService, ISeedDataService seedData)
    {
        _loanService = loanService;
        _seedData = seedData;
        Loan = new Loan { Lenders = [seedData.GetDefaultLender()] };
    }

    [ObservableProperty] private Loan _loan = new();
    [ObservableProperty] private string _activeSection = "loanSetup";
    [ObservableProperty] private HashSet<string> _openSections = ["loanSetup"];
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private bool _showGenerated;
    [ObservableProperty] private string _autoSaveStatus = "saved";
    [ObservableProperty] private Dictionary<string, List<string>> _validationErrors = [];

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
                Loan.LoanType.HasValue,
                !string.IsNullOrEmpty(Loan.Terms.Principal) && !string.IsNullOrEmpty(Loan.Terms.InterestRate),
                Loan.Borrowers.Any(b => !string.IsNullOrEmpty(b.Name)) && Loan.Lenders.Any(l => !string.IsNullOrEmpty(l.Name)),
                Loan.Properties.Any(p => !string.IsNullOrEmpty(p.Address) || !string.IsNullOrEmpty(p.Description)),
                true, // fees always valid
                true  // features always valid
            };
            return (int)(checks.Count(c => c) / (double)checks.Length * 100);
        }
    }

    public bool IsSectionComplete(string sectionId) => sectionId switch
    {
        "loanSetup" => Loan.LoanType.HasValue,
        "loanTerms" => !string.IsNullOrEmpty(Loan.Terms.Principal) && !string.IsNullOrEmpty(Loan.Terms.InterestRate) && !string.IsNullOrEmpty(Loan.Terms.Term),
        "parties" => Loan.Borrowers.Any(b => !string.IsNullOrEmpty(b.Name)) && Loan.Lenders.Any(l => !string.IsNullOrEmpty(l.Name)),
        "property" => Loan.Properties.Any(p => !string.IsNullOrEmpty(p.Address) || !string.IsNullOrEmpty(p.Description)),
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

    public void SelectLoanType(LoanType type)
    {
        Loan.LoanType = type;
        OnPropertyChanged(nameof(Loan));
        OnPropertyChanged(nameof(CompletionPercent));
        TriggerAutoSave();
    }

    // ── Party management ──
    public void AddBorrower() { Loan.Borrowers.Add(new Party()); NotifyLoanChanged(); }
    public void RemoveBorrower(int index) { if (Loan.Borrowers.Count > 1) { Loan.Borrowers.RemoveAt(index); NotifyLoanChanged(); } }
    public void AddGuarantor() { Loan.Guarantors.Add(new Party { EntityType = EntityType.Individual }); NotifyLoanChanged(); }
    public void RemoveGuarantor(int index) { if (Loan.Guarantors.Count > 1) { Loan.Guarantors.RemoveAt(index); NotifyLoanChanged(); } }
    public void AddLender() { Loan.Lenders.Add(new Party()); NotifyLoanChanged(); }
    public void RemoveLender(int index) { if (Loan.Lenders.Count > 1) { Loan.Lenders.RemoveAt(index); NotifyLoanChanged(); } }
    public void AddBroker() { Loan.Brokers.Add(new Broker()); NotifyLoanChanged(); }
    public void RemoveBroker(int index) { if (Loan.Brokers.Count > 1) { Loan.Brokers.RemoveAt(index); NotifyLoanChanged(); } }

    // ── Property management ──
    public void AddProperty() { Loan.Properties.Add(new Property()); NotifyLoanChanged(); }
    public void RemoveProperty(int index) { if (Loan.Properties.Count > 1) { Loan.Properties.RemoveAt(index); NotifyLoanChanged(); } }

    // ── Fee management ──
    public void AddFee(List<Fee> feeList) { feeList.Add(new Fee()); NotifyLoanChanged(); }
    public void RemoveFee(List<Fee> feeList, int index) { feeList.RemoveAt(index); NotifyLoanChanged(); }

    // ── Assignee management ──
    public void AddAssignee() { Loan.Features.Assignees.Add(new Assignee()); NotifyLoanChanged(); }
    public void RemoveAssignee(int index) { if (Loan.Features.Assignees.Count > 1) { Loan.Features.Assignees.RemoveAt(index); NotifyLoanChanged(); } }

    // ── Third party owner management ──
    public void AddThirdPartyOwner(Property prop) { prop.ThirdPartyOwners.Add(new ThirdPartyOwner()); NotifyLoanChanged(); }
    public void RemoveThirdPartyOwner(Property prop, int index) { prop.ThirdPartyOwners.RemoveAt(index); NotifyLoanChanged(); }

    // ── Signatory management helpers ──
    public void AddSignatory(List<Signatory> list) { list.Add(new Signatory()); NotifyLoanChanged(); }
    public void RemoveSignatory(List<Signatory> list, int index) { list.RemoveAt(index); NotifyLoanChanged(); }
    public void AddAlias(List<Alias> list) { list.Add(new Alias()); NotifyLoanChanged(); }
    public void RemoveAlias(List<Alias> list, int index) { list.RemoveAt(index); NotifyLoanChanged(); }

    // ── Entity owner management ──
    public void AddOwner(List<EntityOwner> list) { list.Add(new EntityOwner()); NotifyLoanChanged(); }
    public void RemoveOwner(List<EntityOwner> list, int index) { list.RemoveAt(index); NotifyLoanChanged(); }

    // ── Validation ──
    public bool Validate()
    {
        var errors = new Dictionary<string, List<string>>();
        if (!Loan.LoanType.HasValue) errors["loanSetup"] = ["Select a loan type"];
        if (string.IsNullOrEmpty(Loan.Terms.Principal) || string.IsNullOrEmpty(Loan.Terms.InterestRate) || string.IsNullOrEmpty(Loan.Terms.Term))
            errors["loanTerms"] = ["Complete required loan terms"];
        if (!Loan.Borrowers.Any(b => !string.IsNullOrEmpty(b.Name)))
            errors["parties"] = ["Borrower name required"];
        if (!Loan.Properties.Any(p => !string.IsNullOrEmpty(p.Address) || !string.IsNullOrEmpty(p.Description)))
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
        Loan = new Loan { Lenders = [_seedData.GetDefaultLender()] };
        ShowGenerated = false;
        IsGenerating = false;
        ValidationErrors = [];
        OnPropertyChanged(nameof(Loan));
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
            _loanService.Save(Loan);
            AutoSaveStatus = "saved";
        }
        catch (TaskCanceledException) { }
    }
}
