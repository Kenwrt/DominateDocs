using FluentValidation;
using LiquidDocsData.Models;

namespace LiquidDocsSite.Components.Pages.FluentValidation;


public class PaymentOptionsValidator : AbstractValidator<CreditCardSubscription>
{
    public PaymentOptionsValidator()
    {
        RuleFor(x => x.MonthlyCost)
           .GreaterThanOrEqualTo(0).WithMessage("Monthly Cost is required");

        RuleFor(x => x.CreditCard.CardholderName)
             .NotEmpty().WithMessage("Card Holder Name is required")
            .MaximumLength(120);

        RuleFor(x => x.CreditCard.CardNumber)
            .NotEmpty().WithMessage("Card Number is required")
            .MaximumLength(100);

        RuleFor(x => x.CreditCard.ExpDate)
           .NotEmpty().WithMessage("Card Expiration Date is required")
           .MaximumLength(100);

        RuleFor(x => x.CreditCard.CCV)
            .NotEmpty().WithMessage("CCV is required")
          .MaximumLength(60);

        RuleFor(x => x.PlanName)
            .NotEmpty().WithMessage("Plan Name is required")
            .MaximumLength(120);

        RuleFor(x => x.CreditCard.BillingAddress)
            .NotEmpty().WithMessage("Billing Address is required")
            .MaximumLength(120);

        RuleFor(x => x.CreditCard.BillingCity)
            .NotEmpty().WithMessage("Billing City is required")
            .MaximumLength(120);

        //RuleFor(x => x.CreditCard.BillingState)
        //    .NotEmpty().WithMessage("Billing state is required")
        //    .MaximumLength(120);

        RuleFor(x => x.CreditCard.BillingZipCode)
            .NotEmpty().WithMessage("Billing Zip Code is required")
            .MaximumLength(120);
    }
}