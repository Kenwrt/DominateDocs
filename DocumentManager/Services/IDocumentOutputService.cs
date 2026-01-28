using DominateDocsData.Enums;
using DominateDocsData.Models;

namespace DocumentManager.Services;
public interface IDocumentOutputService
{
    Dictionary<string, object?> BuildEvalData(LoanAgreement loan);
    List<Document> EvaluateDocuments(LoanType loanType, LoanAgreement loanAgreement, IReadOnlyList<Document> docPool);
    List<Guid> GetDocLibIds();
    List<Document> GetDocuments(Guid docLibId);
    List<LoanAgreement> GetLoanAgreements();
    string GetLoanLabel(LoanAgreement loan);
    List<LoanType> GetLoanTypes(Guid docLibId);
    Task MergeAndEmailAsync(IReadOnlyList<Document> docs, LoanAgreement loanAgreement, DocumentTypes.OutputTypes outputType, string emailTo, string subject);
}