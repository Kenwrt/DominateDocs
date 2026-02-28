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

public partial class ReportsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty] private string searchQuery = string.Empty;
    [ObservableProperty] private string statusFilter = "All";

    private Guid userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<ReportsViewModel> logger;

    private int nextLoanNumber = 0;

    public ReportsViewModel(IMongoDatabaseRepo dbApp, ILogger<ReportsViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

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
                //TotalPort = DisplayHelper.FormatDollarsCompact(AgreementList.Sum(c => c.PrincipalAmount));

                //ActiveLoanCount = AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Approved).Count().ToString("#,0");

                //PendingLoanCount = AgreementList.Where(x => x.Status == DominateDocsData.Enums.Loan.Status.Pending).Count().ToString("#,0");

                //AverageInterestRate = ((AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled).Sum(s => s.InterestRate) / AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled).Count())).ToString("N2");
            }
        }
        else
        {
            if (AgreementList.Count > 0)
            {
                //TotalPort = DisplayHelper.FormatDollarsCompact(AgreementList.Where(x => x.UserId == userSession.UserId).Sum(c => c.PrincipalAmount));

                //ActiveLoanCount = AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Approved && x.UserId == userSession.UserId).Count().ToString("#,0");

                //PendingLoanCount = AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Pending && x.UserId == userSession.UserId).Count().ToString("#,0");

                //AverageInterestRate = ((AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled && x.UserId == userSession.UserId).Sum(s => s.InterestRate) / AgreementList.Where(x => x.Status != DominateDocsData.Enums.Loan.Status.Cancelled).Count())).ToString("N2");
            }
        }
    }

    public IReadOnlyList<LoanAgreement> FilteredLoans
    {
        get
        {
            // 1. Use IEnumerable for the filtering process
            IEnumerable<LoanAgreement> loans = AgreementList;

            if (loans is null)
            {
                return new List<LoanAgreement>();
            }

            // 2. Apply Status Filter
            if (StatusFilter != "All")
            {
                loans = StatusFilter switch
                {
                    "Active" => loans.Where(l => l.Status == DominateDocsData.Enums.Loan.Status.Active),
                    "Closed" => loans.Where(l => l.Status == DominateDocsData.Enums.Loan.Status.Closed),
                    _ => loans
                };
            }

            // 3. Apply Search Query Filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                loans = loans.Where(l =>
                    (l.ReferenceName?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (l.LoanNumber?.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    l.Borrowers.Any(b => b.EntityName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));
            }

            // 4. Order and convert to List at the very end
            return loans.OrderByDescending(l => l.Id).ToList();
        }
    }

    public void SetFilter(string filter)
    {
        StatusFilter = filter;
        OnPropertyChanged(nameof(FilteredLoans));
    }

    public void Search(string query)
    {
        SearchQuery = query;
        OnPropertyChanged(nameof(FilteredLoans));
    }
}
