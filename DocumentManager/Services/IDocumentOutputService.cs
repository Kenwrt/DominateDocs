using DominateDocsData.Models;

namespace DocumentManager.Services;
public interface IDocumentOutputService
{
    List<Document> EvaluateDocuments(LoanType loanType, LoanAgreement loanAgreement, IReadOnlyList<Document> docPool);
    List<Guid> GetDocLibIds();
    List<Document> GetDocuments(Guid docLibId);
    List<LoanAgreement> GetLoanAgreements();
    string GetLoanLabel(LoanAgreement loan);
    List<LoanType> GetLoanTypes(Guid docLibId);
    byte[]? TryGetTemplateBytes(Document doc);
    Document? TryResolveDocumentById(Guid docId);
}