namespace LiquidDocsSite.Components.Pages.FluentValidation;

using global::FluentValidation;
using LiquidDocsData.Models;

public class QuickLoanAgreementValidator : AbstractValidator<LoanAgreement>
{
    public QuickLoanAgreementValidator()
    {
        RuleFor(x => x.ClosingContactEmail)
            .NotEmpty().WithMessage("Closing Contact Email is required.");

        RuleFor(x => x.ClosingContactName)
            .NotEmpty().WithMessage("Closing Contact Name is required.");

        RuleFor(x => x.LoanPreparerCity)
            .NotEmpty().WithMessage("Loan Preparer City is required.");

        RuleFor(x => x.LoanPreparerCounty)
            .NotEmpty().WithMessage("Loan Preparer County is required.");

        RuleFor(x => x.LoanPreparerEmailAddress)
            .NotEmpty().WithMessage("Loan Preparer Email Address is required.")
            .EmailAddress().WithMessage("Invalid email address")
            .MaximumLength(60);

        RuleFor(x => x.OriginationDate)
            .LessThan(x => x.MaturityDate)
            .WithMessage("Start Date must be before Maturity Date.");

        RuleFor(x => x.PrincipalAmount)
            .GreaterThan(0)
            .WithMessage("Principal Amount must be greater than zero.");

        RuleFor(x => x.InterestRate)
            .InclusiveBetween(0, 100)
            .WithMessage("Interest Rate must be between 0 and 100.");

        RuleFor(x => x.TermInMonths)
            .GreaterThanOrEqualTo(0).WithMessage("Term In Months is required");

       

        RuleFor(x => x.PrepaymentFee)
            .GreaterThanOrEqualTo(0).WithMessage("Prepayment Fee is required");

        RuleFor(x => x.ReserveSpecificAmount)
           .GreaterThanOrEqualTo(0).WithMessage("Reserve Specific Amount is required");
    }
}