using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.Stripe;

public sealed class VolumePricingTier
{
    public Guid Id { get; set; }

    public Guid VolumePricingScheduleId { get; set; }

    public int MinQuantityInclusive { get; set; }

    public int? MaxQuantityInclusive { get; set; }

    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;
}

