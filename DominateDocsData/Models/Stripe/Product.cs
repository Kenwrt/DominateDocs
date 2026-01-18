using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public class Product
{
    public Guid Id { get; set; } = default!;

    public string Object { get; set; } = default!;

    public string Name { get; set; } = default!;

    public string? Description { get; set; }
}

