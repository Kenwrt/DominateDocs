using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsSite.Database;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class UserDefaultProfileViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LoanTypeListDTO> loanTypes = new();

    [ObservableProperty]
    private UserProfile editingUserProfile = new();

    [ObservableProperty]
    private Lender selectedLender = null;

    [ObservableProperty]
    private Broker selectedBroker = null;

    [ObservableProperty]
    private Servicer selectedServicer = null;

    [ObservableProperty]
    private UserProfile? selectedProfile;

    [ObservableProperty]
    private bool hasExistingProfile;

    private readonly UserSession userSession;
    private readonly IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<UserDefaultProfileViewModel> logger;

    private Guid CurrentUserId => userSession.UserId;

    public UserDefaultProfileViewModel(
        IMongoDatabaseRepo dbApp,
        ILogger<UserDefaultProfileViewModel> logger,
        UserSession userSession,
        IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        try
        {
            // 1) Load loan types (used regardless of profile existence)
            var types = dbApp.GetRecords<DominateDocsData.Models.LoanType>()
                .Select(x => new LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey))
                .ToList();

            LoanTypes = new ObservableCollection<LoanTypeListDTO>(types);

            // 2) Load current user's profile (by UserId). Create if missing.
            var profile = dbApp.GetRecords<UserProfile>()
                .FirstOrDefault(x => x.UserId == CurrentUserId);

            

            HasExistingProfile = profile is not null;

            if (profile is null)
            {
                profile = new UserProfile
                {
                    UserId = CurrentUserId,
                    UserDefaultProfile = new UserDefaultProfile()
                };

                // Persist immediately so the UI/components can rely on it existing.
                await dbApp.UpSertRecordAsync(profile);
            }

            // Defensive: ensure nested objects exist
            profile.UserDefaultProfile ??= new UserDefaultProfile();
            profile.UserDefaultProfile.LoanTerms ??= new LoanTerms();

            EditingUserProfile = profile;
            SelectedProfile = profile;

            // Optional: if you keep user defaults in app state, sync it here
            // appState.SetUserProfile(profile);  // <-- only if you actually have this method
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize UserDefaultProfile page for UserId={UserId}", CurrentUserId);
        }
    }

    [RelayCommand]
    private async Task UpsertUserProfile()
    {
        try
        {
            if (EditingUserProfile is null) return;

            // Defensive: ensure nested objects exist
            EditingUserProfile.UserDefaultProfile ??= new UserDefaultProfile();
            EditingUserProfile.UserDefaultProfile.LoanTerms ??= new LoanTerms();

            await dbApp.UpSertRecordAsync(EditingUserProfile);
            HasExistingProfile = true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to upsert UserProfile for UserId={UserId}", CurrentUserId);
        }
    }

    [RelayCommand]
    private void SelectProfile(UserProfile r)
    {
        SelectedProfile = r;
        EditingUserProfile = r;

        // Defensive
        EditingUserProfile.UserDefaultProfile ??= new UserDefaultProfile();
        EditingUserProfile.UserDefaultProfile.LoanTerms ??= new LoanTerms();
    }

    [RelayCommand]
    private void SelectLoanType(LoanTypeListDTO r)
    {
        EditingUserProfile.UserDefaultProfile ??= new UserDefaultProfile();
        EditingUserProfile.UserDefaultProfile.LoanTerms ??= new LoanTerms();

        EditingUserProfile.UserDefaultProfile.LoanTypeId = r.Id;
        EditingUserProfile.UserDefaultProfile.LoanTypeName = r.Name;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        SelectedProfile = null;
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingUserProfile = new UserProfile
        {
            UserId = CurrentUserId,
            UserDefaultProfile = new UserDefaultProfile()
        };

        EditingUserProfile.UserDefaultProfile.LoanTerms ??= new LoanTerms();
    }
}
