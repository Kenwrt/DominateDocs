using DominateDocsData.Models;

namespace DocumentManager.Jobs;

public record LoanJob(LoanAgreement Loan);
public record MergeJob(DocumentMerge Merge);
