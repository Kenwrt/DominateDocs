using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class QuickBorrowerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Borrower> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Borrower> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Borrower editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Borrower selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickBorrowerViewModel> logger;

    public QuickBorrowerViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickBorrowerViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.Borrower> list)
    {
        if (list is not null)
        {
            MyList = list.ToObservableCollection();
        }
        else
        {
            MyList = new ObservableCollection<DominateDocsData.Models.Borrower>();
        }


        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Borrower>().ToList().ForEach(lf => RecordList.Add(lf));
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

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Borrower>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Borrower r)
    {
        int index = MyList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            MyList.RemoveAt(index);
        }
                
    }

    //[RelayCommand]
    //private void AddRecord(DominateDocsData.Models.Borrower r)
    //{
    //    int index = MyList.FindIndex(x => x.Id == r.Id);

    //    if (index > -1)
    //    {
    //        MyList[index] = r;
    //    }
    //    else
    //    {
    //        MyList.Add(r);
    //    }


    //}

    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Borrower r)
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
        EditingRecord = new DominateDocsData.Models.Borrower()
        {
            UserId = userId
            
        };
    }
}