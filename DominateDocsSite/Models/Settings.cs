using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Models;

/// <summary>User default profile — settings wizard data.</summary>
public class UserProfile
{
    // ── Step 1: Your Details ──
    public UserRole Role { get; set; } = UserRole.Lender;
    public EntityType EntityOrIndividual { get; set; } = EntityType.Entity;
    public EntityStructure EntityStructure { get; set; } = EntityStructure.LimitedLiabilityCompany;
    public string EntityName { get; set; } = "Westridge Lending REIT II, LLC";
    public string StateOfIncorporation { get; set; } = "Delaware";
    public string EIN { get; set; } = string.Empty;
    public string EntityAddress { get; set; } = string.Empty;

    public string ContactName { get; set; } = "Matt Horwitz";
    public string ContactRole { get; set; } = "Member";
    public string ContactEmail { get; set; } = "anthony@geracillp.com";
    public string ContactPhone { get; set; } = "(155) 519-3547";

    public List<License> Licenses { get; set; } = [new() { State = "CA", Number = "60DBO-12916" }];

    // ── Step 2: Loan Basics ──
    public LoanType DefaultLoanType { get; set; } = LoanType.Bridge;

    // ── Step 3: User Defaults (features) ──
    public LoanFeatures DefaultFeatures { get; set; } = new();

    // Document delivery
    public string DeliveryMethod { get; set; } = "both";  // "both", "download", "email"
    public string DeliveryEmail { get; set; } = "anthony@geracillp.com";

    // Document preferences
    public bool W9Included { get; set; } = true;
    public bool OwnershipPledgeIncluded { get; set; } = true;

    // ── Step 4: Billing ──
    public string Plan { get; set; } = "Professional";
    public string BillingEmail { get; set; } = "anthony@geracillp.com";
    public string PaymentMethodLast4 { get; set; } = "4242";
    public bool AccountActive { get; set; } = true;
}
