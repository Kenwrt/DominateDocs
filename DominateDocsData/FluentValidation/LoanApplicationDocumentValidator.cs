using FluentValidation;
using DominateDocsData.Models;

namespace DominateDocsData.FluentValidation;

public class LoanApplicationDocumentValidator : AbstractValidator<LoanAgreementDocument>
{
    public LoanApplicationDocumentValidator()
    {
        RuleFor(x => x.Id)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.UserId)
            .NotEqual(Guid.Empty);

        RuleFor(x => x.LoanNumber)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Status)
            .IsInEnum();

        RuleFor(x => x.LoanAgreement)
            .NotNull()
            .SetValidator(new LoanAgreementValidator());

        RuleFor(x => x.Borrowers)
            .NotEmpty()
            .WithMessage("At least one borrower is required.");

        RuleForEach(x => x.Borrowers)
            .SetValidator(new BorrowerValidator());

        RuleForEach(x => x.Guarantors)
            .SetValidator(new GuarantorValidator());

        RuleForEach(x => x.Lenders)
            .SetValidator(new LenderValidator());

        RuleForEach(x => x.Brokers)
            .SetValidator(new BrokerValidator());

        RuleForEach(x => x.Properties)
            .SetValidator(new PropertyRecordValidator());

        RuleForEach(x => x.Liens)
            .SetValidator(new LienValidator());
    }
}