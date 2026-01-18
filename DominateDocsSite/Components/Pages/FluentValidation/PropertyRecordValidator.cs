using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;

public class PropertyRecordValidator : AbstractValidator<PropertyRecord>
{
    public PropertyRecordValidator()
    {
        RuleFor(x => x.FullAddress)
            .NotEmpty();

        RuleFor(x => x.StreetAddress)
            .NotEmpty();

        RuleFor(x => x.City)
            .NotEmpty();

        RuleFor(x => x.State)
            .NotEmpty();

        RuleFor(x => x.ZipCode)
            .NotEmpty();

        RuleFor(x => x.EstimatedValue)
            .GreaterThan(0);
    }
}
