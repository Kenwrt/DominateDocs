using DominateDocsData.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DominateDocsData.Enums.Stripe;

namespace DominateDocsData.Models.Stripe;

public sealed class VolumePricingSchedule
{
    public Guid Id { get; set; }

    // This schedule is intended to be customer-specific.
    public Guid BillingAccountId { get; set; }

    public BillableUnit Unit { get; set; } = BillableUnit.LoanDocumentSet;

    public TieringMode TieringMode { get; set; } = TieringMode.Volume;

    public string Currency { get; set; } = "usd";

    public DateTimeOffset EffectiveFrom { get; set; } = DateTimeOffset.UtcNow;
    
    public DateTimeOffset? EffectiveTo { get; set; }

    public bool IsActive { get; set; } = true;

    public List<VolumePricingTier> Tiers { get; set; } = new();
}

