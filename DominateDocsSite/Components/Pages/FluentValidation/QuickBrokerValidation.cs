using FluentValidation;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class QuickBrokerValidator : AbstractValidator<Broker>
{
    public QuickBrokerValidator()
    {
        RuleFor(x => x.ContactName)
            .NotEmpty().WithMessage("Contact name is required")
            .MaximumLength(60);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required")
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(60);

        RuleFor(x => x.ContactPhoneNumber)
            .NotEmpty().WithMessage("Contact phone is required")
            .MaximumLength(12);

        RuleFor(x => x.EntityName)
            .MaximumLength(120);

        RuleFor(x => x.SSN)
            .NotEmpty().WithMessage("SSN is required for Individuals.")
            .Matches(@"^\d{3}-\d{2}-\d{4}$").WithMessage("SSN must be in the format ###-##-####.")
            .When(x => x.EntityType == Entity.Types.Individual);

        RuleFor(x => x.EIN)
            .NotEmpty().WithMessage("EIN is required for Entities.")
            .Matches(@"^\d{2}-\d{7}$").WithMessage("EIN must be in the format ##-#######.")
            .When(x => x.EntityType == Entity.Types.Individual);

        RuleFor(x => x.FullAddress)
            .NotEmpty().WithMessage("Street Address is required")
            .MaximumLength(120);
    }
}