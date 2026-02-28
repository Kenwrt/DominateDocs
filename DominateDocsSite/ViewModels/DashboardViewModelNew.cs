using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Helpers;
using DominateDocsData.Models;
using DominateDocsSite.Services;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class DashboardViewModelNew : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.LoanAgreement>? agreementList = new();

    //[ObservableProperty]
    //private ObservableCollection<DominateDocsData.Models.DocumentSet>? documentSets = new();

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty]
    private string totalPort = null; 

    [ObservableProperty]
    private string activeLoanCount = null;

    [ObservableProperty]
    private string pendingLoanCount = null;

    [ObservableProperty]
    private string averageInterestRate = null;

    [ObservableProperty] private string _statusFilter = "All";

    public string Greeting
    {
        get
        {
            var hour = DateTime.Now.Hour;
            var period = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
            return $"{period}, Matt";
        }
    }

    //private readonly ILoanService _loanService;
    private Guid userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<DashboardViewModelNew> logger;

    private int nextLoanNumber = 0;

    public DashboardViewModelNew(IMongoDatabaseRepo dbApp, ILogger<DashboardViewModelNew> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        //_loanService = loanService;
        userId = userSession.UserId;
    }


    [RelayCommand]
    private async Task InitDashboard()
    {
        if (userSession.UserRole == UserEnums.Roles.Admin.ToString() || userSession.UserRole == UserEnums.Roles.DevAdmin.ToString())
        {
            AgreementList = dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().ToObservableCollection();
        }
        else
        {
            AgreementList = dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().Where(x => x.UserId == userId).ToObservableCollection();
        }

        //  DocumentSets = new ObservableCollection<DominateDocsData.Models.DocumentSet>(dbApp.GetRecords<DominateDocsData.Models.DocumentSet>().Where(x => x.UserId == Guid.Parse(userSession.UserId)));

        if (userSession.UserRole == UserEnums.Roles.Admin.ToString() || userSession.UserRole == UserEnums.Roles.DevAdmin.ToString())
        {
            if (AgreementList.Count > 0)
            {
                TotalPort = DisplayHelper.FormatDollarsCompact(AgreementList.Sum(c => c.PrincipalAmount));

                ActiveLoanCount = AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Approved).Count().ToString("#,0");

                PendingLoanCount = AgreementList.Where(x => x.Status == DominateDocsData.Enums.Loan.Status.Pending).Count().ToString("#,0");

                AverageInterestRate = ((AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled).Sum(s => s.InterestRate) / AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled).Count())).ToString("N2");
            }
        }
        else
        {
            if (AgreementList.Count > 0)
            {
                TotalPort = DisplayHelper.FormatDollarsCompact(AgreementList.Where(x => x.UserId == userSession.UserId).Sum(c => c.PrincipalAmount));

                ActiveLoanCount = AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Approved && x.UserId == userSession.UserId).Count().ToString("#,0");

                PendingLoanCount = AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Pending && x.UserId == userSession.UserId).Count().ToString("#,0");

                AverageInterestRate = ((AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled && x.UserId == userSession.UserId).Sum(s => s.InterestRate) / AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled).Count())).ToString("N2");
            }
        }
    }



    // public IReadOnlyList<DominateDocsData.Models.LoanAgreement> AllLoans => _loanService.GetAll();

    public IReadOnlyList<DominateDocsData.Models.LoanAgreement> FilteredLoans => StatusFilter switch
    {
        "Active" => AgreementList.Where(l => l.Status == Loan.Status.Active).ToList(),
        "Pending" => AgreementList.Where(l => l.Status == Loan.Status.Pending).ToList(),
        _ => AgreementList.ToList()
    };

    public decimal TotalPortfolio => AgreementList?.Sum(l => l.PrincipalAmount) ?? 0;

    public string TotalPortfolioDisplay
    {
        get
        {
            var total = TotalPortfolio;
            return total >= 1_000_000 ? $"${total / 1_000_000:F1}M" : $"${total:N0}";
        }
    }

    public string AverageRate
    {
        get
        {
            if (AgreementList == null || !AgreementList.Any()) return "-";

            // Select the decimal values directly, filter for those > 0
            var rates = AgreementList
                .Select(l => l.InterestRate)
                .Where(r => r > 0)
                .ToList();

            return rates.Count != 0 ? $"{rates.Average():F1}%" : "-";
        }
    }

    public int ActiveCount => AgreementList.Count(l => l.Status == Loan.Status.Active);
    public int PendingCount => AgreementList.Count(l => l.Status == Loan.Status.Pending);

    public void SetFilter(string filter)
    {
        StatusFilter = filter;
        OnPropertyChanged(nameof(FilteredLoans));
    }
}
