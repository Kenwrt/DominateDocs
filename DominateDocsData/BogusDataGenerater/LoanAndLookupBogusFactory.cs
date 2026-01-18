using Bogus;
using DominateDocsData.Enums;
using DominateDocsData.Models;

namespace DominateDocsData.BogusDataGenerater;

public static class LoanAndLookupBogusFactory
{
    private static readonly Faker<Borrower> BorrowerFaker =
        new Faker<Borrower>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.EntityStructure, f => f.PickRandom(Entity.Structures.LLC, Entity.Structures.INC, Entity.Structures.LLC))
            .RuleFor(x => x.EntityType, f => f.PickRandom(Entity.Types.Individual, Entity.Types.Entity))
            .RuleFor(x => x.ContactsRole, f => f.PickRandom<Entity.ContactRoles>())
            .RuleFor(x => x.StateOfIncorporation, f => f.PickRandom<UsStates.UsState>())
            .RuleFor(x => x.EntityName, f => f.Company.CompanyName())
            .RuleFor(x => x.ContactName, f => f.Name.FullName())
            .RuleFor(x => x.ContactEmail, f => f.Internet.Email())
            .RuleFor(x => x.ContactPhoneNumber, f => f.Phone.PhoneNumber("(###) ###-####"))
            .RuleFor(x => x.FullAddress, f => f.Address.FullAddress())
            .RuleFor(x => x.StreetAddress, f => f.Address.StreetAddress())
            .RuleFor(x => x.City, f => f.Address.City())
            .RuleFor(x => x.State, f => f.Address.StateAbbr())
            .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
            .RuleFor(x => x.Country, _ => "US")
            .RuleFor(x => x.SSN, f => f.Random.Replace("###-##-####"))
            .RuleFor(x => x.EIN, f => f.Random.Replace("##-#######"))
            .RuleFor(x => x.IsActive, _ => true);

    private static readonly Faker<Lender> LenderFaker =
        new Faker<Lender>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.EntityType, f => f.PickRandom<Entity.Types>())
            .RuleFor(x => x.StateOfIncorporation, f => f.PickRandom<UsStates.UsState>())
            .RuleFor(x => x.EntityStructure, f => f.PickRandom<Entity.Structures>())
            .RuleFor(x => x.ContactsRole, f => f.PickRandom<Entity.ContactRoles>())
            .RuleFor(x => x.EntityName, f => f.Company.CompanyName())
            .RuleFor(x => x.ContactName, f => f.Name.FullName())
            .RuleFor(x => x.ContactEmail, f => f.Internet.Email())
            .RuleFor(x => x.ContactPhoneNumber, f => f.Phone.PhoneNumber("(###) ###-####"))
            .RuleFor(x => x.FullAddress, f => f.Address.FullAddress())
            .RuleFor(x => x.StreetAddress, f => f.Address.StreetAddress())
            .RuleFor(x => x.City, f => f.Address.City())
            .RuleFor(x => x.State, f => f.Address.StateAbbr())
            .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
            .RuleFor(x => x.Country, _ => "US")
            .RuleFor(x => x.EIN, f => f.Random.Replace("##-#######"))
            .RuleFor(x => x.IsActive, _ => true);

    private static readonly Faker<Guarantor> GuarantorFaker =
        new Faker<Guarantor>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.GuarantorType, f => f.PickRandom<GuarantorPosition.Types>())
            .RuleFor(x => x.EntityType, f => f.PickRandom<Entity.Types>())
            .RuleFor(x => x.EntityStructure, f => f.PickRandom<Entity.Structures>())
            .RuleFor(x => x.ContactsRole, f => f.PickRandom<Entity.ContactRoles>())
            .RuleFor(x => x.StateOfIncorporation, f => f.PickRandom<UsStates.UsState>())
            .RuleFor(x => x.EntityName, f => f.Company.CompanyName())
            .RuleFor(x => x.ContactName, f => f.Name.FullName())
            .RuleFor(x => x.ContactEmail, f => f.Internet.Email())
            .RuleFor(x => x.ContactPhoneNumber, f => f.Phone.PhoneNumber("(###) ###-####"))
            .RuleFor(x => x.FullAddress, f => f.Address.FullAddress())
            .RuleFor(x => x.StreetAddress, f => f.Address.StreetAddress())
            .RuleFor(x => x.City, f => f.Address.City())
            .RuleFor(x => x.State, f => f.Address.StateAbbr())
            .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
            .RuleFor(x => x.Country, _ => "US")
            .RuleFor(x => x.SSN, f => f.Random.Replace("###-##-####"))
            .RuleFor(x => x.EIN, f => f.Random.Replace("##-#######"))
            .RuleFor(x => x.IsActive, _ => true);

    private static readonly Faker<Broker> BrokerFaker =
        new Faker<Broker>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.EntityType, f => f.PickRandom<Entity.Types>())
            .RuleFor(x => x.EntityStructure, f => f.PickRandom<Entity.Structures>())
            .RuleFor(x => x.ContactsRole, f => f.PickRandom<Entity.ContactRoles>())
            .RuleFor(x => x.StateOfIncorporation, f => f.PickRandom<UsStates.UsState>())
            .RuleFor(x => x.EntityName, f => f.Company.CompanyName())
            .RuleFor(x => x.ContactName, f => f.Name.FullName())
            .RuleFor(x => x.ContactEmail, f => f.Internet.Email())
            .RuleFor(x => x.ContactPhoneNumber, f => f.Phone.PhoneNumber("(###) ###-####"))
            .RuleFor(x => x.FullAddress, f => f.Address.FullAddress())
            .RuleFor(x => x.StreetAddress, f => f.Address.StreetAddress())
            .RuleFor(x => x.City, f => f.Address.City())
            .RuleFor(x => x.State, f => f.Address.StateAbbr())
            .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
            .RuleFor(x => x.Country, _ => "US")
            .RuleFor(x => x.BrokerCommissionPercentage, f => f.Random.Decimal(0.5m, 4m))
            .RuleFor(x => x.IsActive, _ => true);

    private static readonly Faker<PropertyRecord> PropertyFaker =
        new Faker<PropertyRecord>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.FullAddress, f => f.Address.FullAddress())
            .RuleFor(x => x.StreetAddress, f => f.Address.StreetAddress())
            .RuleFor(x => x.City, f => f.Address.City())
            .RuleFor(x => x.State, f => f.Address.StateAbbr())
            .RuleFor(x => x.ZipCode, f => f.Address.ZipCode())
            .RuleFor(x => x.Country, _ => "US")
            .RuleFor(x => x.ParcelNumber, f => f.Random.AlphaNumeric(10))
            .RuleFor(x => x.EstimatedValue, f => f.Finance.Amount(250_000, 2_000_000))
            .RuleFor(x => x.IsActive, _ => true);

    private static readonly Faker<Lien> LienFaker =
        new Faker<Lien>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.Description, f => $"Deed of Trust #{f.Random.Int(1, 9999)}")
            .RuleFor(x => x.LienPosition, f => f.PickRandom<Liens.Positions>());

    private static readonly Faker<LoanAgreement> LoanAgreementFaker =
        new Faker<LoanAgreement>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            .RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.LoanNumber, f => $"LN-{DateTime.UtcNow:yyyy}-{f.Random.Int(1000, 9999)}")
            .RuleFor(x => x.PrincipalAmount, f => f.Finance.Amount(100_000, 3_000_000))
            .RuleFor(x => x.InterestRate, f => f.Random.Decimal(8m, 14m))
            .RuleFor(x => x.TermInMonths, f => f.Random.Int(6, 36))
            .RuleFor(x => x.InitialMargin, f => f.Random.Decimal(0m, 5m))
            .RuleFor(x => x.AmorizationType, f => f.PickRandom<Payment.AmortizationTypes>())
            .RuleFor(x => x.RepaymentSchedule, f => f.PickRandom<Payment.Schedules>())
            // .RuleFor(x => x.LoanType, f => f.PickRandom<Loan.Types>())
            .RuleFor(x => x.RateType, f => f.PickRandom<Payment.RateTypes>())
            .RuleFor(x => x.PerDiemOption, f => f.PickRandom<Payment.PerDiemInterestOptions>())
            .RuleFor(x => x.IsBalloonPayment, f => f.Random.Bool());

    private static readonly Faker<Document> DocumentFaker =
        new Faker<Document>()
            .RuleFor(x => x.Id, _ => Guid.NewGuid())
            //.RuleFor(x => x.UserId, f => f.Random.Guid())
            .RuleFor(x => x.Name, f => f.Lorem.Sentence(3))

            .RuleFor(x => x.HiddenTagName, _ => "LoanNumber")
            .RuleFor(x => x.HiddenTagValue, f => $"LN-{f.Random.Int(1000, 9999)}");

    public static LoanAgreementDocument GenerateLoanApplication(Guid userId)
    {
        var agreement = LoanAgreementFaker.Clone()
            .RuleFor(x => x.UserId, _ => userId)
            .Generate();

        var app = new LoanAgreementDocument
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LoanNumber = agreement.LoanNumber,
            CreatedAtUtc = DateTime.UtcNow,
            Status = Loan.Status.Pending,
            LoanAgreement = agreement,
            Borrowers = BorrowerFaker.Generate(1),
            Guarantors = GuarantorFaker.GenerateBetween(0, 2),
            Lenders = LenderFaker.Generate(1),
            Brokers = BrokerFaker.GenerateBetween(0, 1),
            Properties = PropertyFaker.GenerateBetween(1, 3),
            Liens = LienFaker.GenerateBetween(0, 3)
        };

        return app;
    }

    public static Borrower GenerateBorrower(Guid userId) =>
        BorrowerFaker.Clone().RuleFor(x => x.UserId, _ => userId).Generate();

    public static Guarantor GenerateGuarantor(Guid userId) =>
        GuarantorFaker.Clone().RuleFor(x => x.UserId, _ => userId).Generate();

    public static Lender GenerateLender(Guid userId) =>
        LenderFaker.Clone().RuleFor(x => x.UserId, _ => userId).Generate();

    public static Broker GenerateBroker(Guid userId) =>
        BrokerFaker.Clone().RuleFor(x => x.UserId, _ => userId).Generate();

    public static PropertyRecord GenerateProperty(Guid userId) =>
        PropertyFaker.Clone().RuleFor(x => x.UserId, _ => userId).Generate();
}