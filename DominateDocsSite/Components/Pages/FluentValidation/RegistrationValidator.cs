using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class RegistrationValidator : AbstractValidator<UserProfile>
{
    public RegistrationValidator()
    {
        RuleFor(x => x.UserName)
               .NotEmpty().WithMessage("User Name is required")
            .EmailAddress().WithMessage("Invalid User Name address")
            .MaximumLength(60);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one number.")
            .Matches(@"[\W_]").WithMessage("Password must contain at least one special character.");

        RuleFor(x => x.ConfirmedPassword)
            .Equal(x => x.Password).WithMessage("Passwords do not match.");

        RuleFor(x => x.Name)
           .NotEmpty().WithMessage("Name is required")
           .MaximumLength(120);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(60);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone Number is required")
            .MaximumLength(12);

        RuleFor(x => x.StreetAddress)
            .NotEmpty().WithMessage("Street Address is required")
            .MaximumLength(120);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required")
            .MaximumLength(120);

        RuleFor(x => x.ZipCode)
            .NotEmpty().WithMessage("Zip Code is required")
            .MaximumLength(60);
    }
}