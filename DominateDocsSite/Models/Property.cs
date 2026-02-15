using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Models;

/// <summary>Collateral property securing the loan.</summary>
public class Property
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PropertyType Type { get; set; } = PropertyType.SingleFamily;
    public string Address { get; set; } = string.Empty;
    public string ParcelNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;  // legal description

    // ── Ownership ──
    public OwnerType OwnerType { get; set; } = OwnerType.Borrower;
    public List<int> SelectedBorrowerIndices { get; set; } = [];
    public List<int> SelectedGuarantorIndices { get; set; } = [];
    public List<ThirdPartyOwner> ThirdPartyOwners { get; set; } = [];

    // ── Title Insurance ──
    public string PolicyPercent { get; set; } = string.Empty;
    public bool CoverFullLoan { get; set; } = true;
    public string SecurityAmount { get; set; } = string.Empty;

    // ── Partial Release (populated if feature enabled) ──
    public string ReleaseAmount { get; set; } = string.Empty;
}
