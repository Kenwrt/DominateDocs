using DocumentManager.Services;
using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;

namespace DominateDocsSite.ViewModels;

public sealed class TestMergeViewModel
{
    public enum TestMode
    {
        OutputEvaluation,
        SingleDocumentMerge
    }

    private readonly DocumentOutputService outputService;
    private readonly ILogger<TestMergeViewModel> logger;

    public TestMergeViewModel(
        DocumentOutputService outputService,
        ILogger<TestMergeViewModel> logger)
    {
        this.outputService = outputService;
        this.logger = logger;
    }

    public bool IsBusy { get; private set; }
    public string? Status { get; private set; }

    public TestMode Mode { get; set; } = TestMode.OutputEvaluation;

    // Library selection (DocLibId)
    public List<Guid> DocLibIds { get; private set; } = new();
    public Guid SelectedDocLibId { get; set; }

    // Email/Merge options
    public string EmailTo { get; set; } = "";
    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    // Lists filtered by SelectedDocLibId (except LoanAgreements)
    public List<Document> Documents { get; private set; } = new();
    public List<LoanType> LoanTypes { get; private set; } = new();

    // Agreements are NOT keyed by DocLibId in your model, so keep unfiltered (admin bench)
    public List<LoanAgreement> LoanAgreements { get; private set; } = new();

    // Selections
    public Document? SelectedDocument { get; set; }
    public LoanAgreement? SelectedLoanAgreement { get; set; }
    public LoanType? SelectedLoanType { get; set; }

    // Results
    public List<Document> EvaluatedDocs { get; private set; } = new();

    public bool CanEvaluate =>
        SelectedDocLibId != Guid.Empty &&
        SelectedLoanAgreement != null &&
        SelectedLoanType != null;

    public bool CanMergeEvaluated =>
        CanEvaluate &&
        EvaluatedDocs.Count > 0 &&
        !string.IsNullOrWhiteSpace(EmailTo);

    public bool CanMergeSingle =>
        SelectedDocLibId != Guid.Empty &&
        SelectedLoanAgreement != null &&
        SelectedDocument != null &&
        !string.IsNullOrWhiteSpace(EmailTo);

    public async Task InitializeAsync()
    {
        try
        {
            Status = "Loading admin bench…";

            DocLibIds = outputService.GetDocLibIds();

            if (SelectedDocLibId == Guid.Empty && DocLibIds.Count > 0)
                SelectedDocLibId = DocLibIds[0];

            LoanAgreements = outputService.GetLoanAgreements();

            await ReloadAsync();

            Status = "Ready.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "InitializeAsync failed");
            Status = "Init failed. Check logs.";
        }
    }

    public Task ReloadAsync()
    {
        try
        {
            Status = "Reloading lists…";

            Documents = outputService.GetDocuments(SelectedDocLibId);
            LoanTypes = outputService.GetLoanTypes(SelectedDocLibId);

            EvaluatedDocs = new List<Document>();

            Status = $"Loaded: {Documents.Count} docs, {LoanTypes.Count} loan types. Agreements: {LoanAgreements.Count} (unfiltered).";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ReloadAsync failed");
            Status = "Reload failed. Check logs.";
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        SelectedDocument = null;
        SelectedLoanAgreement = null;
        SelectedLoanType = null;
        EvaluatedDocs = new List<Document>();
        Status = null;
    }

    public Task EvaluateAsync()
    {
        if (!CanEvaluate) return Task.CompletedTask;

        try
        {
            EvaluatedDocs = outputService.EvaluateDocuments(
                loanType: SelectedLoanType!,
                loanAgreement: SelectedLoanAgreement!,
                docPool: Documents);

            Status = $"Evaluated: {EvaluatedDocs.Count} docs.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "EvaluateAsync failed");
            Status = "Evaluate failed. Check logs.";
        }

        return Task.CompletedTask;
    }

    public async Task MergeEvaluatedAsync()
    {
        if (!CanMergeEvaluated) return;

        IsBusy = true;
        try
        {
            Status = $"Queueing {EvaluatedDocs.Count} merge job(s)…";

            await outputService.MergeAndEmailAsync(
                docs: EvaluatedDocs,
                loanAgreement: SelectedLoanAgreement!,
                outputType: OutputType,
                emailTo: EmailTo,
                subject: $"Evaluated Output ({SelectedLoanType!.Name})");

            Status = "Email queued (best effort).";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MergeEvaluatedAsync failed");
            Status = "Merge/email failed. Check logs.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task MergeSingleAsync()
    {
        if (!CanMergeSingle) return;

        IsBusy = true;
        try
        {
            Status = "Queueing 1 merge job…";

            await outputService.MergeAndEmailAsync(
                docs: new List<Document> { SelectedDocument! },
                loanAgreement: SelectedLoanAgreement!,
                outputType: OutputType,
                emailTo: EmailTo,
                subject: $"Single Doc Test ({SelectedDocument!.Name})");

            Status = "Email queued (best effort).";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "MergeSingleAsync failed");
            Status = "Merge/email failed. Check logs.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public string GetLoanLabel(LoanAgreement loan) => outputService.GetLoanLabel(loan);
}
