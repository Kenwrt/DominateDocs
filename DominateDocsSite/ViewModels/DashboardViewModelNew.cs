using CommunityToolkit.Mvvm.ComponentModel;
using DominateDocsSite.Models;
using DominateDocsSite.Models.Enums;
using DominateDocsSite.Services;

namespace DominateDocsSite.ViewModels;

public partial class DashboardViewModelNew : ObservableObject
{
    private readonly ILoanService _loanService;

    public DashboardViewModelNew(ILoanService loanService) => _loanService = loanService;

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

    public IReadOnlyList<Loan> AllLoans => _loanService.GetAll();

    public IReadOnlyList<Loan> FilteredLoans => StatusFilter switch
    {
        "Active" => AllLoans.Where(l => l.Status == LoanStatus.Active).ToList(),
        "Pending" => AllLoans.Where(l => l.Status == LoanStatus.Pending).ToList(),
        "Draft" => AllLoans.Where(l => l.Status == LoanStatus.Draft).ToList(),
        _ => AllLoans.ToList()
    };

    public decimal TotalPortfolio => AllLoans
        .Sum(l => decimal.TryParse(l.Terms.Principal.Replace(",", ""), out var v) ? v : 0);

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
            var rates = AllLoans.Select(l => decimal.TryParse(l.Terms.InterestRate, out var r) ? r : 0).Where(r => r > 0).ToList();
            return rates.Count != 0 ? $"{rates.Average():F1}%" : "—";
        }
    }

    public int ActiveCount => AllLoans.Count(l => l.Status == LoanStatus.Active);
    public int PendingCount => AllLoans.Count(l => l.Status == LoanStatus.Pending || l.Status == LoanStatus.InReview);

    public void SetFilter(string filter)
    {
        StatusFilter = filter;
        OnPropertyChanged(nameof(FilteredLoans));
    }
}
