using DominateDocsSite.Models;
using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Services;

public class SeedDataService : ISeedDataService
{
    public IReadOnlyList<Party> SavedBorrowers { get; } =
    [
        new()
        {
            Id = Guid.NewGuid(), EntityType = EntityType.Entity,
            Name = "Sunrise Capital Partners, LLC",
            Structure = EntityStructure.LimitedLiabilityCompany,
            FormationState = "California", EIN = "82-1234567",
            Address = "1920 Main St, Suite 400, Irvine, CA 92614"
        },
        new()
        {
            Id = Guid.NewGuid(), EntityType = EntityType.Entity,
            Name = "Pacific Coast Holdings, LLC",
            Structure = EntityStructure.LimitedLiabilityCompany,
            FormationState = "Nevada", EIN = "45-9876543",
            Address = "3400 S Las Vegas Blvd, Las Vegas, NV 89109"
        },
        new()
        {
            Id = Guid.NewGuid(), EntityType = EntityType.Individual,
            Name = "Marcus Rivera",
            Address = "847 Elm Drive, Phoenix, AZ 85004"
        }
    ];

    public IReadOnlyList<Party> SavedGuarantors { get; } =
    [
        new()
        {
            Id = Guid.NewGuid(), EntityType = EntityType.Individual,
            Name = "James Chen",
            Address = "1920 Main St, Irvine, CA 92614"
        },
        new()
        {
            Id = Guid.NewGuid(), EntityType = EntityType.Individual,
            Name = "Sarah Mitchell",
            Address = "3400 S Las Vegas Blvd, Las Vegas, NV 89109"
        }
    ];

    public IReadOnlyList<Servicer> SavedServicers { get; } =
    [
        new() { Id = Guid.NewGuid(), Name = "FCI Lender Services", Address = "8180 E Kaiser Blvd, Anaheim, CA 92808", Phone = "(800) 931-2424" },
        new() { Id = Guid.NewGuid(), Name = "Planet Financial Group", Address = "321 N Clark St, Chicago, IL 60654", Phone = "(312) 555-0190" }
    ];

    public Party GetDefaultLender() => new()
    {
        Id = Guid.NewGuid(),
        EntityType = EntityType.Entity,
        Name = "Westridge Lending REIT II, LLC",
        Structure = EntityStructure.LimitedLiabilityCompany,
        FormationState = "California",
        EIN = "87-4521890",
        Address = "90 Discovery, Irvine, CA 92618",
        ClosingContact = "Rebecca Torres",
        ClosingContactEmail = "rtorres@westridgelending.com",
        OwnershipPercent = "100",
        IsAutoFilled = true,
        HasLicenses = false
    };

    public UserProfile GetDefaultProfile() => new();
}

public class InMemoryLoanService : ILoanService
{
    private readonly List<Loan> _loans;
    private readonly ISeedDataService _seed;

    public InMemoryLoanService(ISeedDataService seed)
    {
        _seed = seed;
        _loans =
        [
            new()
            {
                LoanType = LoanType.Bridge,
                Terms = new() { Principal = "2,000,000", InterestRate = "9.50", Term = "18" },
                Borrowers = [new() { EntityType = EntityType.Entity, Name = "Geraci Holdings LLC", Address = "123 Anywhere St, Los Angeles, CA 90012" }],
                Lenders = [seed.GetDefaultLender()],
                Properties = [new() { Address = "123 Anywhere St, Los Angeles, CA 90012" }],
                Status = LoanStatus.Pending,
                CreatedDate = new DateTime(2026, 1, 15),
                CreatedBy = "Matt Horwitz"
            },
            new()
            {
                LoanType = LoanType.Bridge,
                Terms = new() { Principal = "875,000", InterestRate = "11.25", Term = "12" },
                Borrowers = [new() { EntityType = EntityType.Entity, Name = "Westfield Capital Inc", Address = "4521 Oak Ridge Dr, Pasadena, CA 91101" }],
                Lenders = [seed.GetDefaultLender()],
                Properties = [new() { Address = "4521 Oak Ridge Dr, Pasadena, CA 91101" }],
                Status = LoanStatus.Active,
                CreatedDate = new DateTime(2025, 12, 3),
                CreatedBy = "Matt Horwitz"
            },
            new()
            {
                LoanType = LoanType.Bridge,
                ShowConstruction = true,
                Terms = new() { Principal = "4,250,000", InterestRate = "10.00", Term = "24" },
                Borrowers = [new() { EntityType = EntityType.Entity, Name = "Pacific Ridge Dev LLC", Address = "2200 Sunset Blvd, Santa Monica, CA 90401" }],
                Lenders = [seed.GetDefaultLender()],
                Properties = [new() { Address = "2200 Sunset Blvd, Santa Monica, CA 90401" }],
                Status = LoanStatus.InReview,
                CreatedDate = new DateTime(2026, 1, 28),
                CreatedBy = "Matt Horwitz"
            },
            new()
            {
                LoanType = LoanType.DSCR,
                Terms = new() { Principal = "2,000,000", InterestRate = "5.50", Term = "360" },
                Borrowers = [new() { EntityType = EntityType.Entity, Name = "KP Investments LLC", Address = "780 Pine Valley Ln, Irvine, CA 92618" }],
                Lenders = [seed.GetDefaultLender()],
                Properties = [new() { Address = "780 Pine Valley Ln, Irvine, CA 92618" }],
                Status = LoanStatus.Pending,
                CreatedDate = new DateTime(2026, 2, 1),
                CreatedBy = "Matt Horwitz"
            },
            new()
            {
                LoanType = LoanType.DSCR,
                Terms = new() { Principal = "2,000,000", InterestRate = "5.50", Term = "360" },
                Borrowers = [new() { Name = "Ken's Test Loan" }],
                Lenders = [seed.GetDefaultLender()],
                Properties = [new() { Address = "555 Market St, San Francisco, CA 94105" }],
                Status = LoanStatus.Draft,
                CreatedDate = new DateTime(2026, 2, 5),
                CreatedBy = "Matt Horwitz"
            }
        ];
    }

    public IReadOnlyList<Loan> GetAll() => _loans.AsReadOnly();

    public Loan? GetById(Guid id) => _loans.FirstOrDefault(l => l.Id == id);

    public Loan Create()
    {
        var loan = new Loan { Lenders = [_seed.GetDefaultLender()] };
        _loans.Add(loan);
        return loan;
    }

    public void Save(Loan loan)
    {
        loan.ModifiedDate = DateTime.UtcNow;
        var idx = _loans.FindIndex(l => l.Id == loan.Id);
        if (idx >= 0) _loans[idx] = loan;
        else _loans.Add(loan);
    }

    public void Delete(Guid id) => _loans.RemoveAll(l => l.Id == id);
}

public class InMemoryPartyService : IPartyService
{
    private readonly ISeedDataService _seed;
    public InMemoryPartyService(ISeedDataService seed) => _seed = seed;

    public IReadOnlyList<Party> SearchBorrowers(string query) =>
        string.IsNullOrWhiteSpace(query) ? _seed.SavedBorrowers
        : _seed.SavedBorrowers.Where(b => b.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<Party> SearchGuarantors(string query) =>
        string.IsNullOrWhiteSpace(query) ? _seed.SavedGuarantors
        : _seed.SavedGuarantors.Where(g => g.Name.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<Servicer> GetServicers() => _seed.SavedServicers;
}
