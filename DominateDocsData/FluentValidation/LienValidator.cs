using FluentValidation;
using DominateDocsData.Models;

namespace DominateDocsData.FluentValidation;

public class LienValidator : AbstractValidator<Lien>
{
    public LienValidator()
    {
        RuleFor(x => x.Description)
            .NotEmpty();

        RuleFor(x => x.LienPosition)
            .IsInEnum();
    }
}