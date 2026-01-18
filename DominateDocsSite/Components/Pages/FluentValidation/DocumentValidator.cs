using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;

public class DocumentValidator : AbstractValidator<Document>
{
    public DocumentValidator()
    {
        RuleFor(x => x.Id).NotEqual(Guid.Empty);
        RuleFor(x => x.UserId).NotEqual(Guid.Empty);
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.State).IsInEnum();
    }
}
