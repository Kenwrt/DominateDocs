using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static DominateDocsData.Enums.Stripe;

namespace DominateDocsData.Models.Stripe;

public sealed class BillingStatementLineItem
{
    public Guid Id { get; set; }

    public Guid BillingStatementId { get; set; }

    public string Description { get; set; } = string.Empty;

    public BillableUnit Unit { get; set; } = BillableUnit.LoanDocumentSet;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Amount => Quantity * UnitPrice;

    // Optional snapshot metadata for audit/debugging.
    public string? PricingTierApplied { get; set; } // e.g. "51–100 @ 250"

    public string? Source { get; set; } // e.g., "Usage", "ManualAdjustment"
}
