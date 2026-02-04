using DocumentManager.Email;
using DominateDocsData.Models;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace DocumentManager.State;

public class DocumentManagerState : IDocumentManagerState
{
    private IOptions<DocumentManagerConfigOptions> config;

    public bool IsReadyForProcessing { get; set; } = false;

    public ConcurrentDictionary<Guid, DocumentMerge> DocumentList { get; set; } = new();
    public ConcurrentDictionary<Guid, LoanAgreement> LoanList { get; set; } = new();
    public ConcurrentQueue<EmailMsg> EmailMsgProcessingQueue { get; set; } = new();
    public ConcurrentQueue<DocumentMerge> DocumentProcessingQueue { get; set; } = new();
    public ConcurrentQueue<LoanAgreement> LoanProcessQueue { get; set; } = new();

    private bool isRunBackgroundDocumentMergeService = false;

    public bool IsRunBackgroundDocumentMergeService
    {
        get
        {
            return isRunBackgroundDocumentMergeService;
        }
        set
        {
            isRunBackgroundDocumentMergeService = value;

            IsRunBackgroundDocumentMergeServiceHasChanged(value);
        }
    }

    private bool isRunBackgroundLoanApplicationService = false;

    public bool IsRunBackgroundLoanApplicationService
    {
        get
        {
            return isRunBackgroundLoanApplicationService;
        }
        set
        {
            isRunBackgroundLoanApplicationService = value;

            IsRunBackgroundLoanApplicationServiceHasChanged(value);
        }
    }

    private bool isHousekeeperActive = false;

    public bool IsHousekeeperActive
    {
        get
        {
            return isHousekeeperActive;
        }
        set
        {
            isHousekeeperActive = value;

            IsHousekeeperActiveHasChanged(value);
        }
    }

    public DateTime HousekeeperLastRunTime { get; set; } = default(DateTime);

    public DateTime ServiceLastRunTime { get; set; } = default(DateTime);

    public bool IsActive { get; set; }

    public bool IsStartup { get; set; } = false;

    public event EventHandler StateChanged;

    public event EventHandler<bool>? IsRunBackgroundDocumentMergeServiceChanged;

    public event EventHandler<bool>? IsRunBackgroundLoanApplicationServiceChanged;

    public event EventHandler<bool>? IsRunBackgroundHousekeeperServiceChanged;

    public DocumentManagerState(IOptions<DocumentManagerConfigOptions> config)
    {
        this.config = config;

        IsRunBackgroundLoanApplicationService = config.Value.IsRunBackgroundLoanApplicationService;
        IsRunBackgroundDocumentMergeService = config.Value.IsRunBackgroundDocumentMergeService;
        IsActive = config.Value.IsActive;
        IsHousekeeperActive = config.Value.IsHousekeeperActive;

        IsReadyForProcessing = true;

        StateHasChanged();
    }

    public async Task IsHousekeeperActiveHasChanged(bool val)
    {
        IsRunBackgroundHousekeeperServiceChanged?.Invoke(this, val);
    }

    public async Task IsRunBackgroundDocumentMergeServiceHasChanged(bool val)
    {
        IsRunBackgroundDocumentMergeServiceChanged?.Invoke(this, val);
    }

    public async Task IsRunBackgroundLoanApplicationServiceHasChanged(bool val)
    {
        IsRunBackgroundLoanApplicationServiceChanged?.Invoke(this, val);
    }

    public void StateHasChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}