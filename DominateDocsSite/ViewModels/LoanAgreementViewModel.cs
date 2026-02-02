using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DocumentManager.CalculatorsSchedulers;
using DocumentManager.Email;
using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DocumentManager.Services;
using DocumentManager.State;
using DominateDocsData.Database;
using DominateDocsData.Enums;
using DominateDocsData.Helpers;
using DominateDocsData.Models;
using DominateDocsData.Models.DTOs;
using DominateDocsSite.State;
using LiquidDocsData.Models;
using Nextended.Core.Extensions;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Forms.VisualStyles;

namespace DominateDocsSite.ViewModels;

public partial class LoanAgreementViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.LoanAgreement>? agreementList = new();

    [ObservableProperty]
    private ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>? loanTypes = new();

    [ObservableProperty]
    private DominateDocsData.Models.Borrower selectedBorrower = null;

    [ObservableProperty]
    private DominateDocsData.Models.Broker selectedBroker = null;

    [ObservableProperty]
    private DominateDocsData.Models.Guarantor selectedGuarantor = null;

    [ObservableProperty]
    private string? lastPipelineStatus;

    [ObservableProperty]
    private DominateDocsData.Models.Lender selectedLender = null;

    [ObservableProperty]
    private DominateDocsData.Models.PropertyRecord selectedProperty = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement editingAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.LoanAgreement selectedAgreement = null;

    [ObservableProperty]
    private DominateDocsData.Models.PaymentSchedule? currentSchedule = new();

    [ObservableProperty]
    private DominateDocsData.Models.BalloonPayments currentBalloonSchedule = new();

    // Mirror fields bound by the UI (keep names obvious)
    [ObservableProperty] private decimal principalAmount;

    [ObservableProperty] private decimal interestRate;
    [ObservableProperty] private decimal initialMargin;
    [ObservableProperty] private decimal estimatedDwnPaymentAmount;
    [ObservableProperty] private int termInMonths;
    [ObservableProperty] private decimal downPaymentPercentage;
    [ObservableProperty] private decimal balloonAmount;
    [ObservableProperty] private int balloonTermMonths;
    [ObservableProperty] private DominateDocsData.Enums.Payment.AmortizationTypes amorizationType;
    [ObservableProperty] private DominateDocsData.Enums.Payment.Schedules repaymentSchedule;
    [ObservableProperty] private DominateDocsData.Enums.Payment.RateTypes rateType;
    [ObservableProperty] private DominateDocsData.Enums.Payment.Schedules adjustmentInterval;
    [ObservableProperty] private DominateDocsData.Enums.Payment.IndexPaths assumedIndexPath;
    [ObservableProperty] private DominateDocsData.Enums.Payment.RateIndexes rateIndex;
    [ObservableProperty] private DateTime? maturityDate;
    [ObservableProperty] private DominateDocsData.Models.PaymentSchedule paySchedule;
    [ObservableProperty] private DominateDocsData.Models.BalloonPayments payBalloonSchedule;
    [ObservableProperty] private DominateDocsData.Models.PaymentSchedule fixedPaymentSchedule;

    private Guid userId;



    private UserSession session;

    private readonly IMongoDatabaseRepo dbApp;
    private readonly ILogger<LoanAgreementViewModel> logger;
    private readonly UserSession userSession;
    private IApplicationStateManager appState;
    private DashboardViewModel dvm;
    private IDocumentManagerState docState;
    private IJobQueue<LoanJob> loanQueue;
    private IJobQueue<EmailJob> emailQueue;
    private ILoanScheduler loanScheduler;
    private IBalloonPaymentCalculater balloonPaymentCalculater;
    private IFetchCurrentIndexRatesAndSchedulesService indexRates;

    private int nextLoanNumber = 0;

    public LoanAgreementViewModel(IMongoDatabaseRepo dbApp, ILogger<LoanAgreementViewModel> logger, UserSession userSession, IApplicationStateManager appState, DashboardViewModel dvm, IDocumentManagerState docState, ILoanScheduler loanScheduler, IBalloonPaymentCalculater balloonPaymentCalculater, IFetchCurrentIndexRatesAndSchedulesService indexRates, IJobQueue<LoanJob> loanQueue,
        IJobQueue<EmailJob> emailQueue)
    {
        this.dbApp = dbApp;
        this.logger = logger;
        this.userSession = userSession;
        this.appState = appState;
        this.dvm = dvm;
        this.docState = docState;
        this.loanScheduler = loanScheduler;
        this.balloonPaymentCalculater = balloonPaymentCalculater;
        this.indexRates = indexRates;
        this.loanQueue = loanQueue;
        this.emailQueue = emailQueue;

        userId = userSession.UserId;

        LoanTypes = new ObservableCollection<DominateDocsData.Models.DTOs.LoanTypeListDTO>(dbApp.GetRecords<DominateDocsData.Models.LoanType>().Select(x => new DominateDocsData.Models.DTOs.LoanTypeListDTO(x.Id, x.Name, x.Description, x.IconKey)));

    }

    [RelayCommand]
    private async Task InitializePage()
    {
        AgreementList = new ObservableCollection<DominateDocsData.Models.LoanAgreement>(dbApp.GetRecords<DominateDocsData.Models.LoanAgreement>().Where(x => x.UserId == userSession.UserId));

        if (AgreementList.Count > 0)
        {
            nextLoanNumber = AgreementList.Max(x => Convert.ToInt32(x.LoanNumber.Substring(8))); //"LN-2024-0";
        }

        if (dvm.SelectedAgreement is not null)
        {
            SelectedAgreement = dvm.SelectedAgreement;
            EditingAgreement = SelectedAgreement;
        }
        else
        {
            EditingAgreement = GetNewRecord();
        }

        SyncFromEditingAgreement();
    }

    [RelayCommand]
    private async Task InitializeRecord()
    {
        if (EditingAgreement is null)
        {
            EditingAgreement = GetNewRecord();
        }

        if (EditingAgreement.DownPaymentPercentage > 0)
        {
            EditingAgreement.DownPaymentAmmount = EstimatedDownPayment;

            EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);

            EditingAgreement.DownPaymentAmmount = EstimatedDwnPaymentAmount;
        }

        GetLoanMaturityDate(EditingAgreement.TermInMonths);

    }

    [RelayCommand]
    private void UpsertAgreement()
    {
        try
        {
            int index = AgreementList.FindIndex(x => x.Id == EditingAgreement.Id);

            if (index > -1)
            {
                AgreementList[index] = EditingAgreement;
            }
            else
            {
                AgreementList.Add(EditingAgreement);
            }

            dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
        }
        catch (Exception ex)
        {
            string Error = ex.Message;
        }
    }

    [RelayCommand]
    private async Task EditAgreement()
    {
        int index = AgreementList.FindIndex(x => x.Id == EditingAgreement.Id);

        if (index > -1)
        {
            AgreementList[index] = EditingAgreement;
        }

        await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(EditingAgreement);

        AgreementList.Clear();

        SelectedAgreement = EditingAgreement;
    }

    [RelayCommand]
    private void DeleteAgreement(DominateDocsData.Models.LoanAgreement r)
    {
        if (r != null)
        {
            int index = AgreementList.FindIndex(x => x.Id == r.Id);

            if (index > -1)
            {
                AgreementList.RemoveAt(index);
            }

            dbApp.DeleteRecord<DominateDocsData.Models.LoanAgreement>(SelectedAgreement);

            SelectedAgreement = null;

            EditingAgreement = GetNewRecord();
        }
    }

    [RelayCommand]
    private void SelectAgreement(DominateDocsData.Models.LoanAgreement r)
    {
        SelectedAgreement = EditingAgreement;

        GetLoanMaturityDate(EditingAgreement.TermInMonths);
    }

    [RelayCommand]
    private async Task ProcessAgreement()
    {
        docState.LoanProcessQueue.Enqueue(EditingAgreement);
    }

    [RelayCommand]
    private void ClearSelection()
    {
        if (SelectedAgreement != null)
        {
            SelectedAgreement = null;
            EditingAgreement = new DominateDocsData.Models.LoanAgreement();
        }
    }

    [RelayCommand]
    private async Task AddAgreement()
    {
        AgreementList.Add(EditingAgreement);

        dbApp.UpSertRecord<DominateDocsData.Models.LoanAgreement>(EditingAgreement);
    }

    [RelayCommand]
    private async Task DeleteBorrower(DominateDocsData.Models.Borrower r)
    {
        int index = EditingAgreement.Borrowers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Borrowers.RemoveAt(index);
        }

        SelectedBorrower = null;

        UpsertAgreement();
    }

    [RelayCommand]
    private async Task UpsertBorrower(DominateDocsData.Models.Borrower r)
    {
        int index = EditingAgreement.Borrowers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Borrowers[index] = r;
        }
        else
        {
            EditingAgreement.Borrowers.Add(r);
        }

        UpsertAgreement();
    }

    [RelayCommand]
    private void DeleteProperty(DominateDocsData.Models.PropertyRecord r)
    {
        int index = EditingAgreement.Properties.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Properties.RemoveAt(index);
        }

        SelectedProperty = null;

        UpsertAgreement();
    }

    [RelayCommand]
    private async Task UpsertProperty(DominateDocsData.Models.PropertyRecord r)
    {
        int index = EditingAgreement.Properties.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Properties[index] = r;
        }
        else
        {
            EditingAgreement.Properties.Add(r);
        }

        UpsertAgreement();
    }



    [RelayCommand]
    private async Task DeleteBroker(DominateDocsData.Models.Broker r)
    {
        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Brokers.RemoveAt(index);
        }

        SelectedBroker = null;

        UpsertAgreement();
    }

    [RelayCommand]
    private async Task UpsertBroker(DominateDocsData.Models.Broker r)
    {
        int index = EditingAgreement.Brokers.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Brokers[index] = r;
        }
        else
        {
            EditingAgreement.Brokers.Add(r);
        }

        UpsertAgreement();
    }

    [RelayCommand]
    private async Task DeleteGuarantor(DominateDocsData.Models.Guarantor r)
    {
        int index = EditingAgreement.Guarantors.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Guarantors.RemoveAt(index);
        }

        SelectedGuarantor = null;

        UpsertAgreement();
    }

    [RelayCommand]
    private async Task UpsertGuarantor(DominateDocsData.Models.Guarantor r)
    {
        int index = EditingAgreement.Guarantors.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Guarantors[index] = r;
        }
        else
        {
            EditingAgreement.Guarantors.Add(r);
        }

        UpsertAgreement();
    }

    // ============================================================
    // ✅ NEW: One-call pipeline from LoanSummary button
    // ============================================================
    public async Task ProcessDocsMergeEmailAsync()
    {
        if (EditingAgreement is null || EditingAgreement.Id == Guid.Empty)
        {
            LastPipelineStatus = "No loan loaded.";
            return;
        }

        var loanId = EditingAgreement.Id;

        DominateDocsData.Models.LoanAgreement? freshLoan = null;
        string? to = null;
        string? originalEmailTo = null;

        try
        {
            // Always reload from DB so this button runs with the same truth the Admin Bench sees.
            freshLoan = dbApp.GetRecordById<DominateDocsData.Models.LoanAgreement>(loanId);
            if (freshLoan is null)
            {
                LastPipelineStatus = "Loan not found in DB.";
                return;
            }

            // Avoid AdminBench-mode suppression in LoanWorker.
            if (freshLoan.AdminBench != null)
            {
                var enabledProp = freshLoan.AdminBench.GetType().GetProperty("Enabled");
                if (enabledProp?.CanWrite == true && enabledProp.PropertyType == typeof(bool))
                    enabledProp.SetValue(freshLoan.AdminBench, false);

                // Strongest option: remove AdminBench block entirely so LoanWorker cannot suppress merges.
                freshLoan.AdminBench = null;
            }

            // Preserve any LoanType selection already stored on the in-memory agreement (if you changed it in the UI).
            if (EditingAgreement.LoanTypeId != Guid.Empty)
            {
                freshLoan.LoanTypeId = EditingAgreement.LoanTypeId;
                freshLoan.LoanTypeName = EditingAgreement.LoanTypeName;
            }

            // Resolve destination email (we will send exactly ONE explicit EmailJob).
            to = ResolveEmailTo(freshLoan);
            if (string.IsNullOrWhiteSpace(to))
            {
                LastPipelineStatus = "No email address found on the loan (EmailTo).";
                return;
            }

            // Prevent any background auto-email path from firing (per-doc/per-merge) during this run.
            // IMPORTANT: LoanWorker/MergeWorker may re-load from DB by Id. So we must persist the temporary suppression.
            originalEmailTo = freshLoan.EmailTo;
            freshLoan.EmailTo = null;

            await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(freshLoan).ConfigureAwait(false);

            LastPipelineStatus = "Queued evaluation + merge pipeline…";
            await loanQueue.EnqueueAsync(new DocumentManager.Jobs.LoanJob(freshLoan), CancellationToken.None).ConfigureAwait(false);

            // Wait for deliveries so we know how many documents are expected.
            var expectedCount = await WaitForDeliveriesCountAsync(loanId, timeoutSeconds: 25).ConfigureAwait(false);

            if (expectedCount > 0)
                LastPipelineStatus = $"Waiting for merges to finish… (expected {expectedCount})";
            else
                LastPipelineStatus = "Waiting for merges to finish… (delivery count not yet visible)";

            // Wait for ALL merges to complete (not just the first one), but do NOT hard-fail the whole pipeline if timing is off.
            var mergesOk = expectedCount > 0
                ? await WaitForMergesCompleteAsync(loanId, expectedCount, timeoutSeconds: 150).ConfigureAwait(false)
                : await WaitForMergesCompleteAsync(loanId, expectedCount: 1, timeoutSeconds: 150).ConfigureAwait(false);

            if (!mergesOk)
                logger.LogWarning("ProcessDocsMergeEmailAsync: merge wait timed out. LoanId={LoanId} ExpectedDeliveries={Expected}", loanId, expectedCount);

            // Force ZIP output for this button (single email, single zip, all docs).
            var mode = DocumentManager.Email.EmailEnums.AttachmentOutput.ZipFile;
            var subject = $"Loan Documents: {freshLoan.ReferenceName ?? "Loan"} | Deliveries={expectedCount}";

            await emailQueue.EnqueueAsync(
                    new DocumentManager.Jobs.EmailJob(loanId, to, subject, mode, ZipMaxWaitSeconds: 45),
                    CancellationToken.None)
                .ConfigureAwait(false);

            LastPipelineStatus = mergesOk
                ? $"Queued ZIP email to {to} with {expectedCount} document(s)."
                : $"Queued ZIP email to {to}. (Merge wait timed out; ZIP may be incomplete. Check logs.)";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ProcessDocsMergeEmailAsync failed for LoanId={LoanId}", loanId);
            LastPipelineStatus = "Pipeline failed. Check logs.";
        }
        finally
        {
            // Always restore EmailTo so normal app flows are not impacted.
            if (freshLoan is not null)
            {
                try
                {
                    freshLoan.EmailTo = originalEmailTo;
                    await dbApp.UpSertRecordAsync<DominateDocsData.Models.LoanAgreement>(freshLoan).ConfigureAwait(false);
                }
                catch (Exception restoreEx)
                {
                    logger.LogWarning(restoreEx, "Failed to restore EmailTo on LoanId={LoanId}", loanId);
                }
            }
        }
    }

    private async Task<bool> WaitForMergesCompleteAsync(Guid loanId, int expectedCount, int timeoutSeconds)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < stopAt)
        {
            var merges = docState.DocumentList.Values
                .Where(m => m?.LoanAgreement?.Id == loanId)
                .ToList();

            if (merges.Count == 0)
            {
                await Task.Delay(300).ConfigureAwait(false);
                continue;
            }

            var anyRunning = merges.Any(m =>
                m is not null &&
                (m.Status == DocumentMergeState.Status.Queued ||
                 m.Status == DocumentMergeState.Status.Submittied));

            var completedWithBytes = merges.Count(m =>
                m is not null &&
                m.Status == DocumentMergeState.Status.Complete &&
                m.MergedDocumentBytes is not null &&
                m.MergedDocumentBytes.Length > 0);

            // ✅ wait for ALL expected docs
            if (!anyRunning && completedWithBytes >= expectedCount)
                return true;

            await Task.Delay(400).ConfigureAwait(false);
        }

        return false;
    }

    private async Task<int> WaitForDeliveriesCountAsync(Guid loanId, int timeoutSeconds)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < stopAt)
        {
            try
            {
                var fresh = dbApp.GetRecordById<DominateDocsData.Models.LoanAgreement>(loanId);
                var count = fresh?.DocumentDeliverys?.Count ?? 0;
                if (count > 0)
                    return count;
            }
            catch
            {
                // ignore + retry
            }

            await Task.Delay(250).ConfigureAwait(false);
        }

        return 0;
    }

    private string ResolveEmailTo(DominateDocsData.Models.LoanAgreement loan)
    {
        if (!string.IsNullOrWhiteSpace(loan.EmailTo))
            return loan.EmailTo.Trim();

        // fallback: if your session holds a user email
        var sessionEmail = userSession?.Email;
        return sessionEmail?.Trim() ?? "";
    }

    private static EmailEnums.AttachmentOutput? TryGetEmailAttachmentMode(DominateDocsData.Models.LoanAgreement loan)
    {
        try
        {
            // If you later add loan.EmailAttachmentOutput, this will pick it up without breaking older loans.
            var prop = loan.GetType().GetProperty("EmailAttachmentOutput");
            if (prop?.CanRead == true && prop.PropertyType == typeof(EmailEnums.AttachmentOutput))
            {
                return (EmailEnums.AttachmentOutput?)prop.GetValue(loan);
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    [RelayCommand]
    private async Task DeleteLender(DominateDocsData.Models.Lender r)
    {
        int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Lenders.RemoveAt(index);
        }

        SelectedLender = null;

        UpsertAgreement();
    }

    [RelayCommand]
    private async Task UpsertLender(DominateDocsData.Models.Lender r)
    {
        int index = EditingAgreement.Lenders.FindIndex(x => x.Id == r.Id);

        if (index > -1)
        {
            EditingAgreement.Lenders[index] = r;
        }
        else
        {
            EditingAgreement.Lenders.Add(r);
        }

        UpsertAgreement();
    }

    public async Task<string> GenerateNewLoanNumberAsync()
    {
        nextLoanNumber++;
        string loanNumberPrefix = "LN-";
        string uniqueIdentifier = $"{DateTime.UtcNow.ToString("yyyy", CultureInfo.InvariantCulture)}-{nextLoanNumber}";

        EditingAgreement.LoanNumber = $"{loanNumberPrefix}{uniqueIdentifier}";

        return $"{loanNumberPrefix}{uniqueIdentifier}";
    }

    private DominateDocsData.Models.LoanAgreement GetNewRecord()
    {
        EditingAgreement = new DominateDocsData.Models.LoanAgreement()
        {
            UserId = userId
        };

        return EditingAgreement;
    }

    public decimal EstimatedDownPayment => Math.Round(EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m), 2);

    public DateOnly GetLoanMaturityDate(int termsInMonths)
    {
        DateTime date = DateTime.Now;

        if (termsInMonths != 0)
        {
            if (EditingAgreement.SignedDate is null)
            {
                date = DateTime.Now;
            }
            else
            {
                date = EditingAgreement.SignedDate.Value;
            }

            MaturityDate = date.AddMonths(termsInMonths);

            date = date.AddMonths(termsInMonths);
        }

        return DateOnly.FromDateTime(date);
    }

    public DateOnly GetBalloonDate(int termsInMonths)
    {
        DateTime date = DateTime.Now;

        if (termsInMonths != 0)
        {
            if (EditingAgreement.SignedDate is null)
            {
                date = DateTime.Now;
            }
            else
            {
                date = EditingAgreement.SignedDate.Value;
            }

            EditingAgreement.BalloonPayments.DueDate = DateOnly.FromDateTime(date.AddMonths(termsInMonths));
        }

        return EditingAgreement.BalloonPayments.DueDate;
    }

    partial void OnEditingAgreementChanged(DominateDocsData.Models.LoanAgreement value) => SyncFromEditingAgreement();

    //******************************

    private void SyncFromEditingAgreement()
    {
        if (EditingAgreement is null) return;

        PrincipalAmount = EditingAgreement.PrincipalAmount;
        InterestRate = EditingAgreement.InterestRate;
        TermInMonths = EditingAgreement.TermInMonths;
        AmorizationType = EditingAgreement.AmorizationType;
        RepaymentSchedule = EditingAgreement.RepaymentSchedule;
        MaturityDate = EditingAgreement.MaturityDate;
        BalloonTermMonths = EditingAgreement.BalloonPayments.BalloonTermMonths;
        BalloonAmount = EditingAgreement.BalloonPayments.BalloonAmount;
        PayBalloonSchedule = EditingAgreement.BalloonPayments;
        InitialMargin = EditingAgreement.InitialMargin;
        AdjustmentInterval = EditingAgreement.VariableInterestProperties.AdjustmentInterval;
        AssumedIndexPath = EditingAgreement.VariableInterestProperties.AssumedIndexPath;
        RateIndex = EditingAgreement.VariableInterestProperties.RateIndex;
        DownPaymentPercentage = EditingAgreement.DownPaymentPercentage;
        EstimatedDwnPaymentAmount = EditingAgreement.PrincipalAmount * (EditingAgreement.DownPaymentPercentage / 100m);
        RateType = EditingAgreement.RateType;
        PaySchedule = EditingAgreement.VariableInterestProperties.PaymentSchedule;
        FixedPaymentSchedule = EditingAgreement.FixedPaymentSchedule;
    }

    //public PaymentSchedule PaymentSchedule()
    //{
    //    PaymentSchedule result = null;

    //    DateTime startDate = DateTime.Now;
    //    DateTime endDate = DateTime.Now;

    //    if (EditingAgreement.SignedDate is null) startDate = DateTime.Now;

    //    if (EditingAgreement.MaturityDate is null) endDate = DateTime.Now.AddMonths(12);

    //    if (EditingAgreement.RateType == Payment.RateTypes.Fixed)
    //    {
    //        if (EditingAgreement.FixedInterestProperties.InterestRate != 0 && EditingAgreement.PrincipalAmount != 0)
    //        {
    //            result = loanScheduler.GenerateFixed(EditingAgreement.DownPaymentAmmount, EditingAgreement.FixedInterestProperties.InterestRate, EditingAgreement.DownPaymentPercentage, startDate, endDate, EditingAgreement.FixedInterestProperties.AmorizationType);
    //        }
    //    }
    //    else
    //    {
    //        if (EditingAgreement.VariableInterestProperties.InterestRate != 0 && EditingAgreement.PrincipalAmount != 0)
    //        {
    //            result = loanScheduler.GenerateVariable(EditingAgreement.DownPaymentAmmount, EditingAgreement.VariableInterestProperties.InterestRate, startDate, endDate, EditingAgreement.VariableInterestProperties.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
    //        }

    //    }

    //    return result;
    //}

    //*********************************************************************
    // Push mirrors back into the model and recompute on each change
    partial void OnPrincipalAmountChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.PrincipalAmount = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnPayBalloonScheduleChanged(DominateDocsData.Models.BalloonPayments value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments = value;

        // RecomputeSchedule(EditingAgreement.VariableInterestProperties.TermInMonths, EditingAgreement.VariableInterestProperties.InterestRate, EditingAgreement.VariableInterestProperties.RepaymentSchedule, EditingAgreement.VariableInterestProperties.AmorizationType);
    }

    partial void OnPayScheduleChanged(DominateDocsData.Models.PaymentSchedule value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.PaymentSchedule = value;
    }

    partial void OnFixedPaymentScheduleChanged(DominateDocsData.Models.PaymentSchedule value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.FixedPaymentSchedule = value;
    }

    partial void OnBalloonAmountChanged(decimal value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.BalloonPayments.BalloonAmount = value;

        RecomputeBalloonSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    partial void OnInterestRateChanged(decimal value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.InterestRate = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnInitialMarginChanged(decimal value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.InitialMargin = value;
    }

    partial void OnTermInMonthsChanged(int value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.TermInMonths = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnBalloonTermMonthsChanged(int value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.BalloonPayments.BalloonTermMonths = value;

        RecomputeBalloonSchedule(EditingAgreement.BalloonPayments.BalloonTermMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
    }

    partial void OnAmorizationTypeChanged(DominateDocsData.Enums.Payment.AmortizationTypes value)
    {
        if (EditingAgreement is null) return;

        if (AmorizationType == Payment.AmortizationTypes.PartiallyAmortized || AmorizationType == Payment.AmortizationTypes.Other)
        {
            EditingAgreement.IsBalloonPayment = true;
        }

        EditingAgreement.AmorizationType = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            //var latestSofr = indexRates.GetLatestSofrAsync();

            //var curve = indexRates.GetLatestSofrAsync(latestSofr, resetsNeeded: 12);

            var schedule = indexRates.GenerateProjectedSchedule(terms: new FetchCurrentIndexRatesAndSchedulesService.LoanTerms());

            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnRepaymentScheduleChanged(DominateDocsData.Enums.Payment.Schedules value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.RepaymentSchedule = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnRateTypeChanged(DominateDocsData.Enums.Payment.RateTypes value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.RateType = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
        else
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType);
        }
    }

    partial void OnDownPaymentPercentageChanged(decimal value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.DownPaymentPercentage = value;
    }

    partial void OnRateIndexChanged(DominateDocsData.Enums.Payment.RateIndexes value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.RateIndex = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
    }

    partial void OnMaturityDateChanged(DateTime? value)
    {
        if (EditingAgreement is null) return;
        EditingAgreement.MaturityDate = value;
    }

    partial void OnAdjustmentIntervalChanged(DominateDocsData.Enums.Payment.Schedules value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.AdjustmentInterval = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
    }

    partial void OnAssumedIndexPathChanged(DominateDocsData.Enums.Payment.IndexPaths value)
    {
        if (EditingAgreement is null) return;

        EditingAgreement.VariableInterestProperties.AssumedIndexPath = value;

        if (EditingAgreement.RateType == Payment.RateTypes.Variable)
        {
            RecomputeSchedule(EditingAgreement.TermInMonths, EditingAgreement.InterestRate, EditingAgreement.RepaymentSchedule, EditingAgreement.AmorizationType, EditingAgreement.VariableInterestProperties.RateChangeList);
        }
    }

    // Single place that decides schedule creation with full null-safety
    private void RecomputeSchedule(int termsInMoths, decimal interestRate, Payment.Schedules paymentSchedule, Payment.AmortizationTypes amortizationType, List<DominateDocsData.Models.RateChange>? rateChangeList = null)
    {
        try
        {
            if (EditingAgreement is null)
            {
                CurrentSchedule = new();
                return;
            }

            var start = EditingAgreement.SignedDate ?? DateTime.Today;

            var end = EditingAgreement.MaturityDate
                        ?? (EditingAgreement.OriginationDate ?? DateTime.Today).AddMonths(
                            termsInMoths > 0 ? termsInMoths : 12);

            if (EditingAgreement.PrincipalAmount > 0 && EditingAgreement.DownPaymentPercentage > -1 && termsInMoths > 0 && start < end)
            {
                if (EditingAgreement.PrincipalAmount <= 0 || interestRate <= 0 || end <= start)
                {
                    CurrentSchedule = new();

                    if (EditingAgreement.RateType == Payment.RateTypes.Variable)
                    {
                        EditingAgreement.VariableInterestProperties.PaymentSchedule = CurrentSchedule;
                    }
                    else
                    {
                        EditingAgreement.FixedPaymentSchedule = CurrentSchedule;
                    }

                    return;
                }

                DominateDocsData.Models.PaymentSchedule schedule = null;

                if (EditingAgreement.RateType == DominateDocsData.Enums.Payment.RateTypes.Fixed)
                {
                    schedule = loanScheduler.GenerateFixed(
                        principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                        annualRatePercent: interestRate,
                        downPaymentPercent: EditingAgreement.DownPaymentPercentage,
                        startDate: start,
                        endDate: end,
                        amortizationType: amortizationType,
                        amortizationTermMonths: termsInMoths);

                    EditingAgreement.FixedPaymentSchedule = schedule ?? new();
                }
                else
                {
                    schedule = loanScheduler.GenerateVariable(
                        principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                        downPaymentPercent: EditingAgreement.DownPaymentPercentage,
                        startDate: start,
                        endDate: end,
                        amortizationType: amortizationType,
                        rateSchedule: rateChangeList,
                        amortizationTermMonths: termsInMoths);

                    EditingAgreement.VariableInterestProperties.PaymentSchedule = schedule ?? new();
                }

                //CurrentSchedule = schedule ?? new();
            }
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }

    private void RecomputeBalloonSchedule(int termsInMoths, decimal interestRate, Payment.Schedules paymentSchedule, Payment.AmortizationTypes amortizationType)
    {
        try
        {
            if (EditingAgreement is null)
            {
                CurrentBalloonSchedule = new DominateDocsData.Models.BalloonPayments();
                return;
            }

            var start = EditingAgreement.SignedDate ?? DateTime.Today;

            var end = EditingAgreement.MaturityDate
                        ?? (EditingAgreement.OriginationDate ?? DateTime.Today).AddMonths(
                            termsInMoths > 0 ? termsInMoths : 12);

            if (EditingAgreement.PrincipalAmount > 0 && EditingAgreement.DownPaymentPercentage > -1 && termsInMoths > 0 && start < end)
            {
                if (EditingAgreement.PrincipalAmount <= 0 || interestRate <= 0 || end <= start)
                {
                    return;
                }

                DominateDocsData.Models.BalloonPayments schedule = null;

                DateTime firstPayment = EditingAgreement.SignedDate ?? DateTime.Today;

                schedule = balloonPaymentCalculater.Generate(
                    principal: EditingAgreement.PrincipalAmount - EditingAgreement.DownPaymentAmmount,
                    annualRatePercent: InterestRate,
                    amortizationTermMonths: TermInMonths,
                    balloonTermMonths: BalloonTermMonths,
                    firstPaymentDate: firstPayment.AddMonths(1),
                    paymentsPerYear: 12);

                PayBalloonSchedule = schedule ?? new();

                PayBalloonSchedule.DueDate = GetBalloonDate(BalloonTermMonths);
            }
        }
        catch (SystemException ex)
        {
            logger.LogError(ex.Message);
        }
    }
}
