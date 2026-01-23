using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsSite.Components.Pages;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class QuickServicerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Servicer> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Servicer> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Servicer editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Servicer selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickServicerViewModel> logger;
    private IApplicationStateManager appState;

    public QuickServicerViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickServicerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.Servicer> list)
    {
        if (list is not null) MyList = list.ToObservableCollection();
    
        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Servicer>().ToList().ForEach(lf => RecordList.Add(lf));
    }

    [RelayCommand]
    private void SelectRecordById(Guid id)
    {

        EditingRecord = dbApp.GetRecords<DominateDocsData.Models.Servicer>().FirstOrDefault(x => x.Id == id);

    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

        //Update All Collections

        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        index = MyList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyList[index] = EditingRecord;
        }
        else
        {
            MyList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Servicer>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Servicer r)
    {
        int index = MyList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            MyList.RemoveAt(index);
        }

    }   

    

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Servicer r)
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
        EditingRecord = new DominateDocsData.Models.Servicer()
        {
            UserId = userId,
            ServicerCode = $"S-{DisplayHelper.GenerateIdCode()}"
        };
    }
}