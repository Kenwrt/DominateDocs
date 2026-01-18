using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public class SubscriptionItem
{
    public Guid Id { get; set; } = default!;

    public Price Price { get; set; } = default!;

    public long Quantity { get; set; }
}


