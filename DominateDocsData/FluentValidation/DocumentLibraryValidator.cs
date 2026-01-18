using FluentValidation;
using DominateDocsData.Models;

namespace DominateDocsData.FluentValidation;

public class DocumentLibraryValidator : AbstractValidator<DocumentLibrary>
{
    public DocumentLibraryValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        //RuleFor(x => x.LoanApplicationId)
        //    .NotEqual(Guid.Empty);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}

public class DocumentValidator : AbstractValidator<Document>
{
    public DocumentValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty();
    }
}