using DominateDocsData.Models;

namespace DocumentManager.Services;

public interface IJobDispatcher
{
    ValueTask EnqueueLoanAsync(LoanAgreement loan, CancellationToken ct = default);
}
