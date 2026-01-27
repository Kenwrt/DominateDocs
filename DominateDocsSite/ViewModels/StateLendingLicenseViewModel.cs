using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Helpers;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class StateLendingLicenseViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.StateLendingLicense> myRecordList = new();

    [ObservableProperty]
    private DominateDocsData.Models.StateLendingLicense editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.StateLendingLicense selectedRecord = null;

    private readonly ILogger<StateLendingLicenseViewModel> logger;

    public StateLendingLicenseViewModel(ILogger<StateLendingLicenseViewModel> logger)
    {
        this.logger = logger;
    }

    [RelayCommand]
    private async Task InitializeLoadPage(List<DominateDocsData.Models.StateLendingLicense> stateLicList = null)
    {
        if (EditingRecord is null)
        {
            GetNewRecord();
        }

        if (stateLicList is not null) MyRecordList = new ObservableCollection<DominateDocsData.Models.StateLendingLicense>(stateLicList);
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        //Update All Collections

        int recordListIndex = MyRecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (recordListIndex > -1)
        {
            MyRecordList[recordListIndex] = EditingRecord;
        }
        else
        {
            MyRecordList.Add(EditingRecord);
        }
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.StateLendingLicense r)
    {
        int recordListIndex = MyRecordList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {
            MyRecordList.RemoveAt(recordListIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.StateLendingLicense r)
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
            GetNewRecord();
        }
    }

    [RelayCommand]
    private void GetNewRecord()
    {
        EditingRecord = new DominateDocsData.Models.StateLendingLicense();
    }
}