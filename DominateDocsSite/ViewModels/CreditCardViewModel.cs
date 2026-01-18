using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Database;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class CreditCardViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.UserProfile> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.CreditCard> recordCCList = new();

    [ObservableProperty]
    private DominateDocsData.Models.UserProfile? editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.UserProfile selectedRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.CreditCard selectedCCRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.CreditCard editingCCRecord = null;

    private Guid userId;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly ILogger<CreditCardViewModel> logger;

    public CreditCardViewModel(IMongoDatabaseRepo dbApp, ILogger<CreditCardViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;

        if (appState.IsUseFakeData)
        {
            //No Fake data at this time
        }
        else
        {
            dbApp.GetRecords<DominateDocsData.Models.UserProfile>().Where(x => x.UserId == userId).ToList().ForEach(lf => RecordList.Add(lf));
        }
    }

    [RelayCommand]
    private async Task AddRecord()
    {
        RecordList.Add(EditingRecord);

        dbApp.UpSertRecord<DominateDocsData.Models.UserProfile>(EditingRecord);

        EditingRecord = new DominateDocsData.Models.UserProfile();
    }

    [RelayCommand]
    private async Task LoadRecord(string userName)
    {
        EditingRecord = await dbApp.GetRecordByUserNameAsync<DominateDocsData.Models.UserProfile>(userName);

        if (EditingRecord is null) EditingRecord = new DominateDocsData.Models.UserProfile();
    }

    [RelayCommand]
    private void EditRecord()
    {
        dbApp.UpSertRecord<DominateDocsData.Models.UserProfile>(EditingRecord);

        var record = RecordList.FirstOrDefault(x => x.Id == EditingRecord.Id);

        if (record != null)
        {
            var index = RecordList.IndexOf(record);
            RecordList[index] = EditingRecord;
        }

        SelectedRecord = null;
    }

    [RelayCommand]
    private void DeleteRecord()
    {
        if (SelectedRecord != null)
        {
            RecordList.Remove(SelectedRecord);

            dbApp.DeleteRecord<DominateDocsData.Models.UserProfile>(SelectedRecord);

            SelectedRecord = null;
            EditingRecord = new DominateDocsData.Models.UserProfile();
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.UserProfile r)
    {
        if (r != null)
        {
            SelectedRecord = r;
            EditingRecord = r;
        }
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedRecord != null)
        {
            SelectedRecord = null;
            EditingRecord = new DominateDocsData.Models.UserProfile();
        }
    }

    [RelayCommand]
    private void ClearCCSelection()
    {
        if (SelectedCCRecord != null)
        {
            SelectedCCRecord = null;
            EditingCCRecord = new DominateDocsData.Models.CreditCard();
        }
    }

    [RelayCommand]
    private async Task AddCCRecord()
    {
        RecordCCList.Add(EditingCCRecord);

        dbApp.UpSertRecord<DominateDocsData.Models.CreditCard>(EditingCCRecord);

        EditingCCRecord = new DominateDocsData.Models.CreditCard();
    }

    [RelayCommand]
    private void SelectCCRecord(DominateDocsData.Models.CreditCard r)
    {
        if (r != null)
        {
            SelectedCCRecord = r;

            //if (EditingRecord.Subscriotion.CreditCard is null)
            //{
            //    EditingRecord.Subscriotion.CreditCard = new DominateDocsData.Models.CreditCard();
            //}

            //EditingRecord.Subscriotion.CreditCard = r;
        }
    }

    [RelayCommand]
    private void DeleteCCRecord()
    {
        if (SelectedCCRecord != null)
        {
            RecordCCList.Remove(SelectedCCRecord);

            dbApp.DeleteRecord<DominateDocsData.Models.CreditCard>(SelectedCCRecord);

            SelectedCCRecord = null;
            EditingCCRecord = new DominateDocsData.Models.CreditCard();
        }
    }
}