using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsSite.Helpers;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class EntityOwnerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.EntityOwner> myRecordList = new();

    [ObservableProperty]
    private DominateDocsData.Models.EntityOwner editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.EntityOwner selectedRecord = null;

    private readonly ILogger<EntityOwnerViewModel> logger;

    public EntityOwnerViewModel(ILogger<EntityOwnerViewModel> logger)
    {
        this.logger = logger;
    }

    [RelayCommand]
    private async Task InitializeLoadPage(List<DominateDocsData.Models.EntityOwner> entityOwnerList = null)
    {
        if (EditingRecord is null)
        {
            GetNewRecord();
        }

        if (entityOwnerList is not null)
        {
            MyRecordList.Clear();
            MyRecordList = entityOwnerList.ToObservableCollection();
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
    private async Task DeleteRecord(DominateDocsData.Models.EntityOwner r)
    {
        int recordListIndex = MyRecordList.FindIndex(x => x.Id == r.Id);

        if (recordListIndex > -1)
        {
            MyRecordList.RemoveAt(recordListIndex);
        }
    }

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.EntityOwner r)
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
        EditingRecord = new DominateDocsData.Models.EntityOwner();
    }
}