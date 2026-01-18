using FluentValidation;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;

public class BorrowerValidator : AbstractValidator<Borrower>
{
    public BorrowerValidator()
    {
        RuleFor(x => x.EntityStructure).IsInEnum();
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.ContactsRole).IsInEnum();
        RuleFor(x => x.StateOfIncorporation).IsInEnum();

        RuleFor(x => x.ContactName)
            .NotEmpty();

        RuleFor(x => x.ContactEmail)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.ContactEmail));

        RuleFor(x => x.ContactPhoneNumber)
            .NotEmpty();

        When(x => x.EntityType == Entity.Types.Individual, () =>
        {
            RuleFor(x => x.SSN)
                .NotEmpty()
                .WithMessage("SSN is required for individual borrowers.");

            RuleFor(x => x.EIN)
                .Empty()
                .WithMessage("Entity borrowers should use EIN, not SSN.");
        });

        When(x => x.EntityType == Entity.Types.Entity, () =>
        {
            RuleFor(x => x.EIN)
                .NotEmpty()
                .WithMessage("EIN is required for entity borrowers.");
        });
    }
}
