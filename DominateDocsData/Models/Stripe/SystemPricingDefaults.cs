using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DominateDocsData.Enums.Stripe;

namespace DominateDocsData.Models.Stripe;

public sealed class SystemPricingDefaults
{
    // One row (or versioned rows). Used as a template on signup.
    public Guid Id { get; set; }

    public decimal DefaultAnnualSubscriptionFeeUsd { get; set; } = 450m;

    // Default tier template (can be serialized JSON or normalized tables)
    public TieringMode DefaultTieringMode { get; set; } = TieringMode.Volume;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public Guid UpdatedByAdminUserId { get; set; }
}
