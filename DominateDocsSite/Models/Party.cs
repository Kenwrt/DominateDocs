using DominateDocsSite.Models.Enums;

namespace DominateDocsSite.Models;

/// <summary>AKA / Also Known As name for any party or signatory.</summary>
public class Alias
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
}

/// <summary>Authorized signatory for an entity, trust, or assignee.</summary>
public class Signatory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public List<Alias> Aliases { get; set; } = [];
}

/// <summary>Owner/member of a borrower or guarantor entity with ownership percentage.</summary>
public class EntityOwner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;       // e.g., "Managing Member"
    public string OwnershipPercent { get; set; } = string.Empty;
    public List<Alias> Aliases { get; set; } = [];
}

/// <summary>State lending license.</summary>
public class License
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string State { get; set; } = string.Empty;
    public string Number { get; set; } = string.Empty;
}

/// <summary>
/// A party to a loan: Borrower, Lender, or Guarantor.
/// Fields used depend on EntityType (Individual / Entity / Trust).
/// </summary>
public class Party
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EntityType EntityType { get; set; } = EntityType.Entity;

    // ── Common ──
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<Alias> Aliases { get; set; } = [];

    // ── Entity-specific ──
    public EntityStructure? Structure { get; set; }
    public string FormationState { get; set; } = string.Empty;
    public string EIN { get; set; } = string.Empty;
    public List<EntityOwner> Owners { get; set; } = [];
    public List<Signatory> Signatories { get; set; } = [];

    // ── Trust-specific ──
    public string StateOrganized { get; set; } = string.Empty;
    // Trustee info captured via Signatories

    // ── Lender-specific ──
    public string ClosingContact { get; set; } = string.Empty;
    public string ClosingContactEmail { get; set; } = string.Empty;
    public string OwnershipPercent { get; set; } = string.Empty;  // for multi-lender
    public bool IsAutoFilled { get; set; }
    public bool HasLicenses { get; set; }
    public List<License> Licenses { get; set; } = [];
}

/// <summary>
/// Broker entity — simplified (no EIN, no aliases, no owners).
/// </summary>
public class Broker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public string FormationState { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsLicensed { get; set; }
    public string LicenseState { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
}

/// <summary>Loan servicer — self-serviced or external entity.</summary>
public class Servicer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public bool SelfServiced { get; set; } = true;
    public string SelectedId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Structure { get; set; } = string.Empty;
    public string FormationState { get; set; } = string.Empty;
    public string EIN { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

/// <summary>Assignee for loans intended for sale — supports Individual/Entity/Trust.</summary>
public class Assignee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EntityType EntityType { get; set; } = EntityType.Entity;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<Alias> Aliases { get; set; } = [];

    // Entity
    public EntityStructure? Structure { get; set; }
    public string FormationState { get; set; } = string.Empty;

    // Trust
    public string StateOrganized { get; set; } = string.Empty;

    // Signatories (Entity + Trust)
    public List<Signatory> Signatories { get; set; } = [];
}

/// <summary>Third-party property owner — supports Individual/Entity/Trust.</summary>
public class ThirdPartyOwner
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public EntityType EntityType { get; set; } = EntityType.Individual;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<Alias> Aliases { get; set; } = [];

    // Entity
    public EntityStructure? Structure { get; set; }
    public string FormationState { get; set; } = string.Empty;

    // Trust
    public string StateOrganized { get; set; } = string.Empty;

    // Signatories for Deed of Trust (Entity + Trust)
    public List<Signatory> Signatories { get; set; } = [];
}
