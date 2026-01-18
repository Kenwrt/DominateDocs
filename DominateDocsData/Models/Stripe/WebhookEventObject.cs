using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public class WebhookEventObject
{
    public Guid Id { get; set; } = default!;

    public string Object { get; set; } = default!;

    public string? Status { get; set; }

    public string? Client_secret { get; set; }

    public string? Customer { get; set; }

    public string? Description { get; set; }

    public long? Amount { get; set; }

    public string? Currency { get; set; }

    public string? ProductId { get; set; }

    public string? PriceId { get; set; }
}

