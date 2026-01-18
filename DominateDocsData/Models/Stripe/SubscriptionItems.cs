using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;
public class SubscriptionItems
{
    public Guid Id { get; set; } = default!;
    public List<SubscriptionItem> Data { get; set; } = new();
}

