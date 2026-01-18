using FluentValidation;
using DominateDocsData.Models;

namespace DominateDocsData.FluentValidation;

public class QuickPropertyValidator : AbstractValidator<PropertyRecord>
{
    public QuickPropertyValidator()
    {
        RuleFor(x => x.FullAddress)
            .NotEmpty().WithMessage("Street Address is required")
            .MaximumLength(120);
    }
}