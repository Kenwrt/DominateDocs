using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public sealed class BillingAccount
{
    public Guid Id { get; set; }

    public Guid CustomerProfileId { get; set; }

    public StripeCustomerProfile? CustomerProfile { get; set; }

    public string Currency { get; set; } = "usd";

    // Customer-specific pricing (NOT system-wide)
    public Guid VolumePricingScheduleId { get; set; }

    public VolumePricingSchedule? VolumePricingSchedule { get; set; }

    public string? StripeSubscriptionId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

