using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager;
using DominateDocsSite.Database;
using DominateDocsSite.Helpers;
using DominateDocsSite.State;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System.Collections.ObjectModel;

namespace DominateDocsSite.ViewModels;

public partial class DocumentLibraryViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.DocumentLibrary> libraryList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.Document> documentList = new();

    [ObservableProperty]
    private DominateDocsData.Models.DocumentLibrary editingLibrary = new();

    [ObservableProperty]
    private DominateDocsData.Models.DocumentLibrary? selectedLibrary;

    private Guid userId;

    private readonly IMongoDatabaseRepo dbApp;

    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private readonly IWebHostEnvironment webEnv;
    private readonly IFileProvider files;
    private readonly IOptions<DocumentManagerConfigOptions> options;

    private readonly ILogger<DocumentLibraryViewModel> logger;

    public DocumentLibraryViewModel(IMongoDatabaseRepo dbApp, ILogger<DocumentLibraryViewModel> logger, UserSession userSession, IApplicationStateManager appState, IWebHostEnvironment webEnv, IOptions<DocumentManagerConfigOptions> options)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.webEnv = webEnv;
        this.options = options;

        files = webEnv.WebRootFileProvider; // points at wwwroot

        userId = userSession.UserId;

        try
        {
            dbApp.GetRecords<DominateDocsData.Models.DocumentLibrary>().ToList().ForEach(r => LibraryList.Add(r));
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }
    }

    [RelayCommand]
    private async Task UpsertLibrary()
    {
        if (EditingLibrary.MasterTemplateBytes is null)
        {
            EditingLibrary.MasterTemplate = options.Value.MasterTemplate;
            EditingLibrary.MasterTemplateBytes = await ReadMasterTeplateAsync(EditingLibrary.MasterTemplate);
        }

        LibraryList.Add(EditingLibrary);

        dbApp.UpSertRecord<DominateDocsData.Models.DocumentLibrary>(EditingLibrary);

        EditingLibrary = await GetNewLibraryRecordAsync();
    }

    [RelayCommand]
    private async Task EditLibrary()
    {
        EditingLibrary.UpdatedAt = DateTime.UtcNow;

        dbApp.UpSertRecord<DominateDocsData.Models.DocumentLibrary>(EditingLibrary);

        var record = LibraryList.FirstOrDefault(x => x.Id == EditingLibrary.Id);

        if (record != null)
        {
            var index = LibraryList.IndexOf(record);
            LibraryList[index] = EditingLibrary;
        }

        SelectedLibrary = null;

        EditingLibrary = await GetNewLibraryRecordAsync();
    }

    [RelayCommand]
    private async Task UpdateMasterTemplate()
    {
        EditingLibrary.UpdatedAt = DateTime.UtcNow;
        EditingLibrary.MasterTemplateBytes = null;

        if (EditingLibrary.MasterTemplateBytes is null)
        {
            EditingLibrary.MasterTemplateBytes = await ReadMasterTeplateAsync(EditingLibrary.MasterTemplate);
        }

        dbApp.UpSertRecord<DominateDocsData.Models.DocumentLibrary>(EditingLibrary);

        SelectedLibrary = null;
    }

    [RelayCommand]
    private async Task InitializeLibrary()
    {
        EditingLibrary = await GetNewLibraryRecordAsync();
    }

    [RelayCommand]
    private async Task DeleteLibrary()
    {
        if (SelectedLibrary != null)
        {
            LibraryList.RemoveWhere(x => x.Id == SelectedLibrary.Id);

            dbApp.DeleteRecord<DominateDocsData.Models.DocumentLibrary>(SelectedLibrary);

            SelectedLibrary = null;
            EditingLibrary = await GetNewLibraryRecordAsync();
        }
    }

    [RelayCommand]
    private void SelectLibrary(DominateDocsData.Models.DocumentLibrary r)
    {
        if (r != null)
        {
            SelectedLibrary = r;
            EditingLibrary = r;
        }
    }

    [RelayCommand]
    private async Task ClearLibrarySelectionAsync()
    {
        if (SelectedLibrary != null)
        {
            SelectedLibrary = null;
            EditingLibrary = await GetNewLibraryRecordAsync();
        }
    }

    public async Task<byte[]> ReadMasterTeplateAsync(string fileName)
    {
        try
        {
            // basic sanitization; don’t allow path traversal
            fileName = Path.GetFileName(fileName);

            var relative = Path.Combine("MasterTemplate", fileName).Replace('\\', '/');
            var info = files.GetFileInfo(relative);
            if (!info.Exists)
                throw new FileNotFoundException($"Template not found: wwwroot/{relative}");

            await using var stream = info.CreateReadStream();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return null;
        }
    }

    private async Task<DominateDocsData.Models.DocumentLibrary> GetNewLibraryRecordAsync()
    {
        EditingLibrary = new DominateDocsData.Models.DocumentLibrary()
        {
           
        };

        return EditingLibrary;
    }
}