namespace DocumentManager.Jobs;

public record EmailJob(Guid LoanId, string ToEmail, string Subject);
