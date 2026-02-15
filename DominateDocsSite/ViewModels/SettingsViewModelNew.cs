using CommunityToolkit.Mvvm.ComponentModel;
using DominateDocsSite.Models;
using DominateDocsSite.Models.Enums;
using DominateDocsSite.Services;

namespace DominateDocsSite.ViewModels;

public partial class SettingsViewModelNew : ObservableObject
{
    private readonly ISeedDataService _seedData;

    public SettingsViewModelNew(ISeedDataService seedData)
    {
        _seedData = seedData;
        Profile = seedData.GetDefaultProfile();
    }

    [ObservableProperty] private UserProfile profile = new();
    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private bool _saved;

    public int TotalSteps => 4;
    public int ProgressPercent => (int)(((CurrentStep + 1) / (double)TotalSteps) * 100);
    public string ProgressLabel => $"Step {CurrentStep + 1} of {TotalSteps}";

    public static readonly (string Title, string Desc)[] Steps =
    [
        ("Your Details", "Role & entity information"),
        ("Loan Basics", "Default loan type"),
        ("User Defaults", "Features & document prefs"),
        ("Billing Details", "Account & subscription")
    ];

    public void GoToStep(int step)
    {
        CurrentStep = Math.Clamp(step, 0, TotalSteps - 1);
        OnPropertyChanged(nameof(ProgressPercent));
        OnPropertyChanged(nameof(ProgressLabel));
    }

    public void NextStep() => GoToStep(CurrentStep + 1);
    public void PrevStep() => GoToStep(CurrentStep - 1);

    public void SelectRole(UserRole role)
    {
        Profile.Role = role;
        OnPropertyChanged(nameof(Profile));
    }

    public void SelectDefaultLoanType(LoanType type)
    {
        Profile.DefaultLoanType = type;
        OnPropertyChanged(nameof(Profile));
    }

    public void AddLicense()
    {
        Profile.Licenses.Add(new License());
        OnPropertyChanged(nameof(Profile));
    }

    public void RemoveLicense(int index)
    {
        if (index < Profile.Licenses.Count)
        {
            Profile.Licenses.RemoveAt(index);
            OnPropertyChanged(nameof(Profile));
        }
    }

    public void Save()
    {
        Saved = true;
        Task.Delay(2000).ContinueWith(_ => { Saved = false; OnPropertyChanged(nameof(Saved)); });
    }
}
