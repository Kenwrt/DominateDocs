using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class AliasValidator : AbstractValidator<AkaName>
{
    public AliasValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(60);

        RuleFor(x => x.AlsoKnownAs)
            .NotEmpty().WithMessage("Alias Name is required")
            .MaximumLength(60);
    }
}