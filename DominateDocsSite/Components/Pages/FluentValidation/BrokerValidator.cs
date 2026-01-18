using FluentValidation;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class BrokerValidator : AbstractValidator<Broker>
{
    public BrokerValidator()
    {
        RuleFor(x => x.EntityType).IsInEnum();
        RuleFor(x => x.EntityStructure).IsInEnum();
        RuleFor(x => x.ContactsRole).IsInEnum();
        RuleFor(x => x.StateOfIncorporation).IsInEnum();

        RuleFor(x => x.EntityName)
            .NotEmpty();

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
                .NotEmpty();

            RuleFor(x => x.EIN)
                .Empty();
        });

        When(x => x.EntityType == Entity.Types.Entity, () =>
        {
            RuleFor(x => x.EIN)
                .NotEmpty();
        });
    }
}
