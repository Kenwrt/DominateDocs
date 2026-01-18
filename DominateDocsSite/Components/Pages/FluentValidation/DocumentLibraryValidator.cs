using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;

public class DocumentLibraryValidator : AbstractValidator<DocumentLibrary>
{
    public DocumentLibraryValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.LoanApplicationId).NotEqual(Guid.Empty);
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty();

        RuleForEach(x => x.DocumentSets)
            .SetValidator(new DocumentSetValidator());
    }
}
