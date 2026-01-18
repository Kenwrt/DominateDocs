using FluentValidation;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


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
