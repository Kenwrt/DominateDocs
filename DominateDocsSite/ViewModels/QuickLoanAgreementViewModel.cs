using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using MudBlazor;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;

namespace DominateDocsSite.ViewModels;

public partial class QuickLoanAgreementViewModel : ObservableObject
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
    private LoanTypeListDTO? selectedLoanType;

    private Guid userId;

    private UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanAgreementViewModel> logger;

    private int nextLoanNumber = 0;

    public QuickLoanAgreementViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanAgreementViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;

        LoanTypes = new ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>(dbApp.GetRecords<DominateDocsData.Models.LoanType>().Select(x => new LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)));
    }


    public bool CanGoNextFromLoanTypeStep => SelectedLoanType is not null;

    public string GetIconForLoanType(LoanTypeListDTO lt)
    {
        // Prefer your IconKey if you’re storing one in DB (you are: LoanTypeListDTO(..., IconKey))
        // Map it to actual MudBlazor icons here.
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

    partial void OnSelectedLoanTypeChanged(LoanTypeListDTO? value)
    {
        // Optional: store selection into the EditingAgreement if your model supports it.
        // I’m not guessing your exact property names. Add one line here once you confirm:
        // EditingAgreement.LoanTypeId = value?.Id ?? Guid.Empty;
        // EditingAgreement.LoanTypeName = value?.Name;
        OnPropertyChanged(nameof(CanGoNextFromLoanTypeStep));
    }


    [RelayCommand]
    private void SelectLoanType(LoanTypeListDTO lt)
    {
        SelectedLoanType = lt;
    }

    [RelayCommand]
    private void NextStep()
    {
        if (ActiveStepIndex == 0 && !CanGoNextFromLoanTypeStep)
            return;

        if (ActiveStepIndex < 2)
            ActiveStepIndex++;
    }

    [RelayCommand]
    private void PreviousStep()
    {
        if (ActiveStepIndex > 0)
            ActiveStepIndex--;
    }

    [RelayCommand]
    private async Task InitializePage()
    {
        try
        {
            GetNewRecord();

            //Assign User's Profiles
            UserProfile userProfile = dbApp.GetRecords<UserProfile>().FirstOrDefault(x => x.UserId == userId);

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
                    Broker r = dbApp.GetRecords<Broker>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.BrokerId);

                    if (r != null)
                    {
                        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);

                        if (index == -1) EditingAgreement.Brokers.Add(r);
                    }
                }

                if (userProfile.LoanDefaults.ServicerId != Guid.Empty)
                {
                    Servicer r = dbApp.GetRecords<Servicer>().FirstOrDefault(x => x.Id == userProfile.LoanDefaults.ServicerId);
                    
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

            AgreementList.Clear();

            AgreementList = new ObservableCollection<DominateDocsData.Models.LoanAgreement>(dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().Where(x => x.UserId == userId));

            if (AgreementList.Count > 0)
            {
                nextLoanNumber = AgreementList.Max(x => Convert.ToInt32(x.LoanNumber.Substring(8))); //"LN-2024-0";
            }

          

            //DocumentSets.Clear();

            //if (userSession.UserRole == UserEnums.Roles.Admin.ToString())
            //{
            //    DocumentSets = new ObservableCollection<DominateDocsData.Models.DocumentSet>(dbApp.GetRecords<DominateDocsData.Models.DocumentSet>());
            //}
            //else
            //{
            //    DocumentSets = new ObservableCollection<DominateDocsData.Models.DocumentSet>(dbApp.GetRecords<DominateDocsData.Models.DocumentSet>().Where(x => x.UserId == Guid.Parse(userId)));
            //}

            await GenerateNewLoanNumberAsync();
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        int index = AgreementList.FindIndex(x => x.Id == EditingAgreement.Id);

        if (index > -1)
        {
            AgreementList[index] = EditingAgreement;
        }
        else
        {
            AgreementList.Add(EditingAgreement);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.LoanAgreement r)
    {
        int recordListIndex = AgreementList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {
            AgreementList.RemoveAt(recordListIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.LoanAgreement>(r);
    }

    [RelayCommand]
    private void SelectAgreement(DominateDocsData.Models.LoanAgreement r)
    {
        SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedAgreement != null)
        {
            SelectedAgreement = null;
            GetNewRecord();
        }
    }

    public async Task GenerateNewLoanNumberAsync()
    {
        nextLoanNumber++;
        string loanNumberPrefix = "LN-";
        string uniqueIdentifier = $"{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{nextLoanNumber}";

        EditingAgreement.LoanNumber = $"{loanNumberPrefix}{uniqueIdentifier}";
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingAgreement = new DominateDocsData.Models.LoanAgreement()
        {
            UserId = userId
        };
    }

    public decimal EstimatedDownPayment => Math.Round(EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m), 2);
}