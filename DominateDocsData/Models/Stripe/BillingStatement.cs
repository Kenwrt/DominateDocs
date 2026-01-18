using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DominateDocsData.Enums.Stripe;

namespace DominateDocsData.Models.Stripe;

public sealed class BillingStatement
{
    public Guid Id { get; set; }

    public Guid BillingAccountId { get; set; }

    public DateTimeOffset PeriodStart { get; set; }

    public DateTimeOffset PeriodEnd { get; set; }

    public string Currency { get; set; } = "usd";

    public int TotalLoanDocumentSets { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public BillingStatementStatus Status { get; set; } = BillingStatementStatus.Draft;

    public string? StripeInvoiceId { get; set; }

    public DateTimeOffset? FinalizedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public List<BillingStatementLineItem> LineItems { get; set; } = new();
}

