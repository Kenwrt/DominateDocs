using FluentValidation;
using DominateDocsData.Models;

namespace DominateDocsData.FluentValidation;

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