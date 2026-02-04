using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentFormat.OpenXml.Office2010.PowerPoint;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsSite.Data;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DominateDocsSite.ViewModels;

public partial class UserProfileViewModel : ObservableObject
{
   
    [ObservableProperty]
    private ObservableCollection<DominateDocsSite.Data.ApplicationUser>? recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.UserProfile>? userProfileList = new();

    [ObservableProperty]
    private DominateDocsData.Models.UserProfile editingUserProfile = null;

    [ObservableProperty]
    private DominateDocsData.Models.UserProfile selectedUserProfile = null;

    [ObservableProperty]
    private DominateDocsSite.Data.ApplicationUser editingUser = null;

    [ObservableProperty]
    private DominateDocsSite.Data.ApplicationUser selectedUser = null;

    private UserSession userSession;
    private string userId => userSession.UserId.ToString() ?? string.Empty;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<UserDefaultProfileViewModel> logger;
    private readonly ApplicationDbContext dbContext;

    private readonly UserManager<ApplicationUser> userManager;

    private int nextLoanNumber = 0;

    public UserProfileViewModel(IMongoDatabaseRepo dbApp, ILogger<UserDefaultProfileViewModel> logger, UserSession userSession, IApplicationStateManager appState, ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.dbContext = dbContext;
        this.userManager = userManager; 

        recordList = new ObservableCollection<DominateDocsSite.Data.ApplicationUser>(dbContext.ApplicationUsers.ToList());

        //foreach (var user in recordList)
        //{
        //    UserProfile userProfile = dbApp.GetRecords<DominateDocsData.Models.UserProfile>().FirstOrDefault(x => x.UserId == Guid.Parse(user.Id));

        //    if (userProfile is null)
        //    {
        //        UserProfile newUserProfile = new UserProfile
        //        {
        //            UserId = Guid.Parse(user.Id),
        //            UserName = user.UserName,
        //            Email = user.Email,
        //            Name = user.Name,
        //            Password = user.PasswordHash,
        //            ConfirmedPassword = string.Empty,
        //            ProfilePictureUrl = string.Empty,
        //            UserRole = DominateDocsData.Enums.UserEnums.Roles.User,
                   
        //        };

        //        dbApp.UpSertRecordAsync<DominateDocsData.Models.UserProfile>(newUserProfile);
        //    }

        //}

        UserProfileList = new ObservableCollection<DominateDocsData.Models.UserProfile>(dbApp.GetRecords<DominateDocsData.Models.UserProfile>().ToList());


    }

    [RelayCommand]
    private async Task InitializePage()
    {
        try
        {
           

            if (EditingUserProfile is null) GetNewRecord();
        
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpsertUserProfile()
    {
     
        await dbApp.UpSertRecordAsync<DominateDocsData.Models.UserProfile>(EditingUserProfile);
    }

    [RelayCommand]
    private async Task UpdateApplicationUser()
    {
        await dbContext.SaveChangesAsync();
    }

    [RelayCommand]
    private void SelectProfile(DominateDocsData.Models.UserProfile r)
    {
        SelectedUserProfile = EditingUserProfile;
    }

    [RelayCommand]
    private async Task ResetPassword(DominateDocsSite.Data.ApplicationUser r)
    {
        var user = await userManager.FindByEmailAsync(r.Email);

        if (user != null)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            if (token != null)
            {
                var result = await userManager.ResetPasswordAsync(
                    user,
                    token,
                    "TempPassword123!"
                );
            }
        }

        //if (!result.Succeeded)
        //{
        //    // inspect result.Errors
        //}
    }

    [RelayCommand]
    private void SelectUser(DominateDocsSite.Data.ApplicationUser r)
    {
        SelectedUser = r;
    }


    [RelayCommand]
    private async Task DeleteUser(DominateDocsSite.Data.ApplicationUser r)
    {
        int index = RecordList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            RecordList.RemoveAt(index);
        }

        dbContext.ApplicationUsers.Remove(r);

        dbApp.DeleteRecordById<DominateDocsData.Models.UserProfile>(Guid.Parse(r.Id));
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedUserProfile != null)
        {
            SelectedUserProfile = null;
            GetNewRecord();
        }

        if (SelectedUser != null)
        {
            SelectedUser = null;
           
        }
    }

  


    [RelayCommand]
    private void GetNewRecord()
    {
        EditingUserProfile = new DominateDocsData.Models.UserProfile()
        {
            UserId = Guid.Parse(userId)
        };
    }

   
}