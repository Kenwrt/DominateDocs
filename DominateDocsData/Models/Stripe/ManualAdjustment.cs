using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DominateDocsData.Enums.Stripe;

namespace DominateDocsData.Models.Stripe;

public sealed class ManualAdjustment
{
    public Guid ManualAdjustmentId { get; set; }

    public Guid BillingAccountId { get; set; }

    public AdjustmentType Type { get; set; }

    // Positive amount; Type determines direction
    public decimal AmountUsd { get; set; }

    public string Reason { get; set; } = string.Empty;

    public Guid CreatedByAdminUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool PostedToStripe { get; set; }
    public string? StripeObjectId { get; set; }
}
