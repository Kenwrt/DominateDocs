using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class SigningAuthorityValidator : AbstractValidator<SigningAuthority>
{
    public SigningAuthorityValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(60);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(60);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone Number is required")
            .MaximumLength(12);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(120);

        RuleFor(x => x.StreetAddress)
            .NotEmpty().WithMessage("Street Address is required")
            .MaximumLength(120);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(120);

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip Code is required")
            .MaximumLength(60);

        RuleFor(x => x.County)
            .NotEmpty().WithMessage("County is required")
            .MaximumLength(60);

        RuleFor(x => x.Country)
            .MaximumLength(60);

        RuleFor(x => x.SSN)
            .NotEmpty().WithMessage("Social Security Number is required")
            .MaximumLength(64);
    }
}