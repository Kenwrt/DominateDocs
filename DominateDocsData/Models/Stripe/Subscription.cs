using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public class Subscription
{
    public Guid Id { get; set; } = default!;

    public string Object { get; set; } = default!;

    public string Customer { get; set; } = default!;

    public SubscriptionItems Items { get; set; } = default!;

    public string Status { get; set; } = default!;
}

