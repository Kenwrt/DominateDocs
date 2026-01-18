using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;

public class DocumentSetValidator : AbstractValidator<DocumentSet>
{
    public DocumentSetValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.LoanId).NotEqual(Guid.Empty);
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty();

        RuleForEach(x => x.Documents)
            .SetValidator(new DocumentValidator());
    }
}
