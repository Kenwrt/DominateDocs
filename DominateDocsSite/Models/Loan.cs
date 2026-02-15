using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Models;

/// <summary>Title company information.</summary>
public class TitleCompany
{
    public string CompanyName { get; set; } = string.Empty;
    public string OfficerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TitlePolicyType PolicyType { get; set; } = TitlePolicyType.Single;
    public string SinglePolicyPercent { get; set; } = string.Empty;
}

/// <summary>Escrow company information.</summary>
public class EscrowCompany
{
    public string CompanyName { get; set; } = string.Empty;
    public string OfficerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Root aggregate: a complete loan with all related data.
/// </summary>
public class Loan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ── Core ──
    public LoanType? LoanType { get; set; }
    public LoanTerms Terms { get; set; } = new();
    public bool ShowConstruction { get; set; }
    public ConstructionDetails Construction { get; set; } = new();
    public DSCRDetails DSCR { get; set; } = new();

    // ── Parties ──
    public List<Party> Borrowers { get; set; } = [new()];
    public List<Party> Lenders { get; set; } = [];
    public bool HasGuarantor { get; set; } = true;
    public List<Party> Guarantors { get; set; } = [new() { EntityType = EntityType.Individual }];
    public bool HasBroker { get; set; }
    public List<Broker> Brokers { get; set; } = [new()];
    public Servicer Servicer { get; set; } = new();
    public bool HasTitle { get; set; }
    public TitleCompany TitleCompany { get; set; } = new();
    public bool HasEscrow { get; set; }
    public EscrowCompany EscrowCompany { get; set; } = new();

    // ── Properties ──
    public List<Property> Properties { get; set; } = [new()];

    // ── Fees ──
    public List<Fee> LenderFees { get; set; } = [];
    public List<Fee> BrokerFees { get; set; } = [];
    public List<Fee> OtherFees { get; set; } = [];

    // ── Features ──
    public LoanFeatures Features { get; set; } = new();

    // ── Metadata ──
    public LoanStatus Status { get; set; } = LoanStatus.Draft;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ModifiedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    // ── Computed display helpers ──
    public string DisplayName => Borrowers.FirstOrDefault()?.Name is { Length: > 0 } name
        ? $"{name} — {LoanType?.ToDisplayString() ?? "Loan"}"
        : "Untitled Loan";

    public string DisplayAddress => Properties.FirstOrDefault()?.Address is { Length: > 0 } addr
        ? addr : "No property";

    public string PackageName => $"{Borrowers.FirstOrDefault()?.Name ?? "Borrower"} — {Properties.FirstOrDefault()?.Address ?? "Property"}";
}
