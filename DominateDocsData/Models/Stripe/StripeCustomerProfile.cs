using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public sealed class StripeCustomerProfile
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid StripeCustomerId { get; set; }

    // Subscription
    public decimal AnnualSubscriptionFeeUsd { get; set; } = 450m;

    // If true, the $450 annual subscription fee is waived.
    public bool IsAnnualSubscriptionFeeWaived { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public string? BillingEmail { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

  
    // Non-sensitive payment method summary for display
    public string? DefaultPaymentMethodId { get; set; } // pm_...
    public string? DefaultCardBrand { get; set; }
    public string? DefaultCardLast4 { get; set; }
    public int? DefaultCardExpMonth { get; set; }
    public int? DefaultCardExpYear { get; set; }

    // Billing details
   
    public string? BillingPhone { get; set; }

    public string? BillingAddressLine1 { get; set; }
    public string? BillingAddressLine2 { get; set; }
    public string? BillingCity { get; set; }
    public string? BillingState { get; set; }
    public string? BillingPostalCode { get; set; }
    public string? BillingCountry { get; set; }

   
    public string? StripeSubscriptionId { get; set; } // sub_...
    public string? SubscriptionStatus { get; set; }

   

}

