using FluentValidation;
using DominateDocsData.Models;

namespace DominateDocsData.FluentValidation;

public class TrusteeValidator : AbstractValidator<Trustee>
{
    public TrusteeValidator()
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

       
    }
}