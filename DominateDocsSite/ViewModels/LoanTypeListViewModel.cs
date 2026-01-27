using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsData.Database;
using DominateDocsSite.State;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class LoanTypeListViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LoanTypeListDTO> recordList = new();

    [ObservableProperty]
    private LoanTypeListDTO? selectedRecord;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanTypeListViewModel> logger;
    private readonly IApplicationStateManager appState;
    private readonly UserSession userSession;

    private Guid UserGuid => userSession.UserId;

    public LoanTypeListViewModel(
        IMongoDatabaseRepo dbApp,
        ILogger<LoanTypeListViewModel> logger,
        UserSession userSession,
        IApplicationStateManager appState)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
    }

    [RelayCommand]
    private Task InitializePage()
    {
        try
        {
            var uid = UserGuid;

            var items = dbApp.GetRecords<LoanType>().Where(x => x.DocLibId == userSession.DocLibId).Select(x => new LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)).ToList();

            RecordList = new ObservableCollection<LoanTypeListDTO>(items);
                       
            // keep selection if still exists
            if (SelectedRecord is not null && RecordList.Any(x => x.Id == SelectedRecord.Id) == false)
                SelectedRecord = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InitializePage failed in LoanTypeListViewModel");
            RecordList = new ObservableCollection<LoanTypeListDTO>();
            SelectedRecord = null;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SelectRecord(LoanTypeListDTO? dto)
    {
        SelectedRecord = dto;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task ClearSelection()
    {
        SelectedRecord = null;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task GetNewRecord()
    {
        SelectedRecord = null;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task DeleteRecord(LoanTypeListDTO? dto)
    {
        if (dto is null) return Task.CompletedTask;

        try
        {
            dbApp.DeleteRecordById<LoanType>(dto.Id);

            var idx = RecordList.ToList().FindIndex(x => x.Id == dto.Id);
            if (idx >= 0) RecordList.RemoveAt(idx);

            if (SelectedRecord?.Id == dto.Id)
                SelectedRecord = null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteRecord failed for LoanType Id={Id}", dto.Id);
        }

        return Task.CompletedTask;
    }
}
