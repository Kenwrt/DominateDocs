using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public class Price
{
    public Guid Id { get; set; } = default!;

    public string Object { get; set; } = default!;

    public string Product { get; set; } = default!;

    public long Unit_amount { get; set; }
    
    public string Currency { get; set; } = default!;

    public Recurring? Recurring { get; set; }
}

