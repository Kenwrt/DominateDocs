using DominateDocsData.Models;
using System.Collections.Concurrent;

namespace DocumentManager.State;

public interface IDocumentManagerState
{
    ConcurrentDictionary<Guid, DocumentMerge> DocumentList { get; set; }
    ConcurrentQueue<DocumentMerge> DocumentProcessingQueue { get; set; }
    DateTime HousekeeperLastRunTime { get; set; }
    bool IsActive { get; set; }
    bool IsHousekeeperActive { get; set; }
    bool IsReadyForProcessing { get; set; }
    bool IsRunBackgroundDocumentMergeService { get; set; }
    bool IsRunBackgroundLoanApplicationService { get; set; }
    bool IsStartup { get; set; }
    ConcurrentDictionary<Guid, LoanAgreement> LoanList { get; set; }
    ConcurrentQueue<LoanAgreement> LoanProcessQueue { get; set; }
    DateTime ServiceLastRunTime { get; set; }

    event EventHandler<bool> IsRunBackgroundDocumentMergeServiceChanged;

    event EventHandler<bool> IsRunBackgroundHousekeeperServiceChanged;

    event EventHandler<bool> IsRunBackgroundLoanApplicationServiceChanged;

    event EventHandler StateChanged;

    Task IsHousekeeperActiveHasChanged(bool val);

    Task IsRunBackgroundDocumentMergeServiceHasChanged(bool val);

    Task IsRunBackgroundLoanApplicationServiceHasChanged(bool val);

    void StateHasChanged();
}