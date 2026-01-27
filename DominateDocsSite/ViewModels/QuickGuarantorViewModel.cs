using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class QuickGuarantorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Guarantor> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Guarantor> myList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor selectedRecord = null;

    private Guid userId;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<QuickGuarantorViewModel> logger;

    public QuickGuarantorViewModel(IMongoDatabaseRepo dbApp, ILogger<QuickGuarantorViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(List<DominateDocsData.Models.Guarantor> list)
    {
        if (list is not null) MyList = list.ToObservableCollection();

        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Guarantor>().ToList().ForEach(lf => RecordList.Add(lf));
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

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Guarantor>(EditingRecord);
    }

    [RelayCommand]
    private void DeleteRecord(DominateDocsData.Models.Guarantor r)
    {
        int index = MyList.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            MyList.RemoveAt(index);
        }

       
    }

    
    [RelayCommand]
    private void SelectRecord(DominateDocsData.Models.Guarantor r)
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
        EditingRecord = new DominateDocsData.Models.Guarantor()
        {
            UserId = userId,
            GuarantorCode = $"G-{DisplayHelper.GenerateIdCode()}"
        };
    }
}