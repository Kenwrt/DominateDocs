using DocumentManager.Infrastructure;
using DocumentManager.Jobs;
using DominateDocsData.Models;

namespace DocumentManager.Services;

public sealed class JobDispatcher : IJobDispatcher
{
    private readonly IJobQueue<LoanJob> _loanQueue;

    public JobDispatcher(IJobQueue<LoanJob> loanQueue)
        => _loanQueue = loanQueue;

    public ValueTask EnqueueLoanAsync(LoanAgreement loan, CancellationToken ct = default)
        => _loanQueue.EnqueueAsync(new LoanJob(loan), ct);
}
