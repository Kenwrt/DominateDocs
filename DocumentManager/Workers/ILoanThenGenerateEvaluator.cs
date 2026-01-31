using DominateDocsData.Models;

namespace DocumentManager.Workers;

public interface ILoanThenGenerateEvaluator
{
    Task<IReadOnlyList<Document>> EvaluateAsync(LoanAgreement loan, CancellationToken ct);
}

