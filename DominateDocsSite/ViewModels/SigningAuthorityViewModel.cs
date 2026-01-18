using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Helpers;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class SigningAuthorityViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.SigningAuthority> myRecordList = new();

    [ObservableProperty]
    private DominateDocsData.Models.SigningAuthority editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.SigningAuthority selectedRecord = null;

    private readonly ILogger<SigningAuthorityViewModel> logger;

    public SigningAuthorityViewModel(ILogger<SigningAuthorityViewModel> logger)
    {
        this.logger = logger;
    }

    [RelayCommand]
    private async Task InitializeLoadPage()
    {
        if (EditingRecord is null)
        {
            GetNewRecord();
        }
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
    private async Task DeleteRecord(DominateDocsData.Models.SigningAuthority r)
    {
        int recordListIndex = MyRecordList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {
            MyRecordList.RemoveAt(recordListIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.SigningAuthority r)
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
        EditingRecord = new DominateDocsData.Models.SigningAuthority();
    }
}