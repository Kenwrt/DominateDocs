using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class PropertyValidator : AbstractValidator<PropertyRecord>
{
    public PropertyValidator()
    {
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

        RuleFor(x => x.ParcelNumber)
            .NotEmpty().WithMessage("Parcel Number is required")
            .MaximumLength(64);

        RuleFor(x => x.TitleDocumentNumber)
          .NotEmpty().WithMessage("Title Document Number is required")
          .MaximumLength(64);

        RuleFor(x => x.LegalDescription)
            .NotEmpty().WithMessage("Description is required")
            .MaximumLength(120);

        RuleFor(x => x.MinimumReleasePrice)
           .GreaterThanOrEqualTo(0).WithMessage("Minimum Release Price is required");

        RuleFor(x => x.SquareFootage)
           .GreaterThanOrEqualTo(0).WithMessage("Square Footage is required");

        RuleFor(x => x.LastAppraisedValue)
           .GreaterThanOrEqualTo(0).WithMessage("Last Appraised Value is required");

        RuleFor(x => x.PurchasePrice)
          .GreaterThanOrEqualTo(0).WithMessage("PurchasePrice is required");

        RuleFor(x => x.PropertyTax)
          .GreaterThanOrEqualTo(0).WithMessage("PropertyTax is required");

        RuleFor(x => x.EstimatedValue)
            .GreaterThanOrEqualTo(0).WithMessage("Estimated Value is required");
    }
}