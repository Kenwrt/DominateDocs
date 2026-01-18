using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class EntityOwnerValidator : AbstractValidator<EntityOwner>
{
    public EntityOwnerValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Contact name is required")
            .MaximumLength(60);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Contact email is required")
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(60);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Contact phone is required")
            .MaximumLength(12);

        RuleFor(x => x.FullAddress)
            .NotEmpty().WithMessage("Street Address is required")
            .MaximumLength(120);

        RuleFor(x => x.PercentOfOwnership)
            .GreaterThanOrEqualTo(0).WithMessage("% of Ownership is required");
    }
}