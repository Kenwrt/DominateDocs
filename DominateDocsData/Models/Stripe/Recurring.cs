using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;
public class Recurring
{
    public Guid Id { get; set; }

    public string Interval { get; set; } = default!;
}

