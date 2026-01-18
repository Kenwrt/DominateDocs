using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Database;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class RegistrationViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.UserProfile> recordList = new();

    [ObservableProperty]
    private DominateDocsData.Models.UserProfile editingRecord = new();

    [ObservableProperty]
    private DominateDocsData.Models.UserProfile selectedRecord = null;

    [ObservableProperty]
    private string userId;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<RegistrationViewModel> logger;

    public RegistrationViewModel(IMongoDatabaseRepo dbApp, ILogger<RegistrationViewModel> logger)
    {
        this.dbApp = dbApp;
        this.logger = logger;

        //  dbApp.GetRecords<DominateDocsData.Models.UserProfile>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private async Task AddRecord(DominateDocsData.Models.UserProfile er)
    {
        RecordList.Add(er);

        dbApp.UpSertRecord<DominateDocsData.Models.UserProfile>(er);

        EditingRecord = new DominateDocsData.Models.UserProfile();
    }

    [RelayCommand]
    private void EditRecord()
    {
        dbApp.UpSertRecord<DominateDocsData.Models.UserProfile>(EditingRecord);

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.UserProfile>().ToList().ForEach(r => RecordList.Add(r));

        SelectedRecord = null;
        EditingRecord = new DominateDocsData.Models.UserProfile();
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
}