using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DominateDocsData.Enums.Stripe;

namespace DominateDocsData.Models.Stripe;

public sealed class LoanDocumentSetGeneratedEvent
{
    public Guid Id { get; set; }

    public Guid BillingAccountId { get; set; }

    public Guid CustomerProfileId { get; set; }

    public Guid LoanApplicationId { get; set; }
    
    // Always 1 per generated set; kept generic.
    public int Quantity { get; set; } = 1;

    public BillableUnit Unit { get; set; } = BillableUnit.LoanDocumentSet;

    // Prevent double-billing if generation retries.
    public string IdempotencyKey { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsBilled { get; set; }
    
    public Guid? BillingStatementId { get; set; }
}

