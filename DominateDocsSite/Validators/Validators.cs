using FluentValidation;
using DominateDocsData.Models;
using DominateDocsData.Enums;

namespace DominateDocsSite.Validators;

//public class LoanTermsValidator : AbstractValidator<LoanTerms>
//{
//    public LoanTermsValidator()
//    {
//        RuleFor(x => x.Principal).NotEmpty().WithMessage("Principal amount is required");
//        RuleFor(x => x.InterestRate).NotEmpty().WithMessage("Interest rate is required");
//        RuleFor(x => x.Term).NotEmpty().WithMessage("Loan term is required");
//        RuleFor(x => x.DefaultRate).NotEmpty().WithMessage("Default rate is required");
//    }
//}

//public class PartyValidator : AbstractValidator<Party>
//{
//    public PartyValidator()
//    {
//        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
//        When(x => x.EntityType == Entity.Types.Entity, () =>
//        {
//            RuleFor(x => x.Structure).NotNull().WithMessage("Entity structure is required");
//            RuleFor(x => x.FormationState).NotEmpty().WithMessage("State of formation is required");
//        });
//        //When(x => x.EntityType == Entity.Types..Trust, () =>
//        //{
//        //    RuleFor(x => x.StateOrganized).NotEmpty().WithMessage("State organized is required");
//        //});
//    }
//}

public class PropertyValidator : AbstractValidator<PropertyRecord>
{
    public PropertyValidator()
    {
        RuleFor(x => x.FullAddress)
            .NotEmpty().When(x => string.IsNullOrEmpty(x.LegalDescription))
            .WithMessage("Property address or legal description is required");
    }
}

public class BrokerValidator : AbstractValidator<Broker>
{
    public BrokerValidator()
    {
        //RuleFor(x => x.EntityName).NotEmpty().WithMessage("Broker name is required");
        //When(x => x.IsLicensed, () =>
        //{
        //    RuleFor(x => x.License).NotEmpty().WithMessage("License state is required");
        //    RuleFor(x => x..LicenseNumber).NotEmpty().WithMessage("License number is required");
        //});
    }
}

public class FeeValidator : AbstractValidator<Fee>
{
    public FeeValidator()
    {
        RuleFor(x => x.Amount).NotEmpty().WithMessage("Fee amount is required");
        RuleFor(x => x.Description).NotEmpty().WithMessage("Fee description is required");
    }
}

public class LoanValidator : AbstractValidator<Loan>
{
    public LoanValidator()
    {
        //RuleFor(x => x.LoanType).NotNull().WithMessage("Select a loan type");
        //RuleFor(x => x.Terms).SetValidator(new LoanTermsValidator());
        //RuleForEach(x => x.Borrowers).SetValidator(new PartyValidator());
        //RuleForEach(x => x.Properties).SetValidator(new PropertyValidator());
        //When(x => x.HasBroker, () =>
        //{
        //    RuleForEach(x => x.Brokers).SetValidator(new BrokerValidator());
        //});
    }
}

/// <summary>Helper to bridge FluentValidation with MudBlazor's Func validation.</summary>
public static class FluentValidationHelper
{
    public static Func<T, Task<string?>> CreateValidator<T>(AbstractValidator<T> validator, string propertyName)
    {
        return async (model) =>
        {
            var result = await validator.ValidateAsync(model, opt => opt.IncludeProperties(propertyName));
            return result.IsValid ? null : result.Errors.First().ErrorMessage;
        };
    }
}
