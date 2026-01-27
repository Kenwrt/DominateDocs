using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Helpers;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class LienViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Lien> myRecordList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Lien editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Lien selectedRecord = null;

    private readonly ILogger<LienViewModel> logger;

    public LienViewModel(ILogger<LienViewModel> logger)
    {
        this.logger = logger;
    }

    [RelayCommand]
    private async Task InitializeLoadPage(List<DominateDocsData.Models.Lien> lienList = null)
    {
        if (EditingRecord is null)
        {
            GetNewRecord();
        }

        if (lienList is not null)
        {
            MyRecordList.Clear();
            MyRecordList = lienList.ToObservableCollection();
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
    private async Task DeleteRecord(DominateDocsData.Models.Lien r)
    {
        int recordListIndex = MyRecordList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {
            MyRecordList.RemoveAt(recordListIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Lien r)
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
        EditingRecord = new DominateDocsData.Models.Lien();
    }
}