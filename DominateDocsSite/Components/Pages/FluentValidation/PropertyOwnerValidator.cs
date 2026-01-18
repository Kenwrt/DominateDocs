using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class PropertyOwnerValidator : AbstractValidator<PropertyOwner>
{
    public PropertyOwnerValidator()
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
            .NotEmpty().WithMessage("EntityName is required")
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

        RuleFor(x => x.EIN)
            .NotEmpty().WithMessage("EIN is required")
            .MaximumLength(64);

        RuleFor(x => x.SSN)
            .NotEmpty().WithMessage("Social Security Number is required")
            .MaximumLength(64);

        RuleFor(x => x.PercentageOfOwnership)
            .GreaterThanOrEqualTo(0).WithMessage("Percentage Of Ownership is required");
    }
}