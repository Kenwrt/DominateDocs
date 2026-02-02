namespace DocumentManager.Jobs;

using DocumentManager.Email;

public record EmailJob(
    Guid LoanId,
    string ToEmail,
    string Subject,
    EmailEnums.AttachmentOutput AttachmentOutput = EmailEnums.AttachmentOutput.IndividualDocument,
    int ZipMaxWaitSeconds = 20
);
