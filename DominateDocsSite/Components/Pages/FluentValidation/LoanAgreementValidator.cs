using FluentValidation;
using LiquidDocsData.Enums;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class LoanAgreementValidator : AbstractValidator<LoanAgreement>
{
    public LoanAgreementValidator()
    {
        RuleFor(x => x.PrincipalAmount)
            .GreaterThan(0);

        RuleFor(x => x.InterestRate)
            .InclusiveBetween(0.01m, 100m);

        RuleFor(x => x.TermInMonths)
            .GreaterThan(0);

        RuleFor(x => x.LoanType)
            .IsInEnum();

        RuleFor(x => x.RateType)
            .IsInEnum();

        RuleFor(x => x.AmorizationType)
            .IsInEnum();

        RuleFor(x => x.RepaymentSchedule)
            .IsInEnum();

        RuleFor(x => x.PerDiemOption)
            .IsInEnum();

        RuleFor(x => x.LoanNumber)
            .NotEmpty()
            .MaximumLength(100);

        When(x => x.IsBalloonPayment, () =>
        {
            RuleFor(x => x.BalloonPayments)
                .NotNull();
        });

        When(x => x.IsPrepaymentPenalty, () =>
        {
            RuleFor(x => x.PrepaymentFee)
                .GreaterThanOrEqualTo(0);
        });
    }
}
