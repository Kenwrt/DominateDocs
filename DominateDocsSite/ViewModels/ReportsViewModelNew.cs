using CommunityToolkit.Mvvm.ComponentModel;
using DominateDocsSite.Models;
using DominateDocsSite.Models.Enums;
using DominateDocsSite.Services;

namespace DominateDocsSite.ViewModels;

public partial class ReportsViewModelNew : ObservableObject
{
    private readonly ILoanService _loanService;

    public ReportsViewModelNew(ILoanService loanService) => _loanService = loanService;

    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _statusFilter = "All";

    public IReadOnlyList<Loan> AllLoans => _loanService.GetAll();

    public IReadOnlyList<Loan> FilteredLoans
    {
        get
        {
            var loans = AllLoans.AsEnumerable();

            if (StatusFilter != "All")
            {
                loans = StatusFilter switch
                {
                    "Active" => loans.Where(l => l.Status == LoanStatus.Active),
                    "Closed" => loans.Where(l => l.Status == LoanStatus.Closed),
                    "Archived" => loans.Where(l => l.Status == LoanStatus.Archived),
                    _ => loans
                };
            }

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                loans = loans.Where(l =>
                    l.DisplayName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    l.DisplayAddress.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    l.Borrowers.Any(b => b.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)));
            }

            return loans.OrderByDescending(l => l.CreatedDate).ToList();
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
