using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Enums;
using DominateDocsData.Database;
using DominateDocsData.Helpers;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class GuarantorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Guarantor> recordList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Guarantor> myGuarantorList = new();

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor editingRecord = null;

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor selectedRecord = null;

    [ObservableProperty]
    private Guid? loanAgreementId = null;

    private Guid userId;

    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<GuarantorViewModel> logger;

    public GuarantorViewModel(IMongoDatabaseRepo dbApp, ILogger<GuarantorViewModel> logger, UserSession userSession, IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;

        userId = userSession.UserId;
    }

    [RelayCommand]
    private async Task InitializePage(DominateDocsData.Models.Guarantor guarantor)
    {
        if (guarantor != null)
        {
            var r = dbApp.GetRecordById<DominateDocsData.Models.Guarantor>(guarantor.Id);

            if (r is not null)
            {
                SelectedRecord = r;
                EditingRecord = r;
            }
        }

        if (EditingRecord is null) GetNewRecord();

        RecordList.Clear();

        dbApp.GetRecords<DominateDocsData.Models.Guarantor>().ToList().ForEach(lf => RecordList.Add(lf));

        //if (guarantorList is not null)
        //{
        //    MyGuarantorList.Clear();

        //    MyGuarantorList = guarantorList.ToObservableCollection();
        //}

        
    }

    [RelayCommand]
    private async Task UpsertRecord()
    {
        if (EditingRecord.EntityType == Entity.Types.Individual && !String.IsNullOrEmpty(EditingRecord.ContactName))
        {
            EditingRecord.EntityName = EditingRecord.ContactName;
        }

        int index = RecordList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            RecordList[index] = EditingRecord;
        }
        else
        {
            RecordList.Add(EditingRecord);
        }

        index = MyGuarantorList.FindIndex(x => x.Id == EditingRecord.Id);

        if (index > -1)
        {
            MyGuarantorList[index] = EditingRecord;
        }
        else
        {
            MyGuarantorList.Add(EditingRecord);
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.Guarantor>(EditingRecord);
    }

    [RelayCommand]
    private async Task DeleteRecord(DominateDocsData.Models.Guarantor r)
    {
        int myGuarantorIndex = RecordList.FindIndex(x => x.Id == r.Id);

        if (myGuarantorIndex > -1)
        {
            RecordList.RemoveAt(myGuarantorIndex);
        }

        dbApp.DeleteRecord<DominateDocsData.Models.Guarantor>(r);
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