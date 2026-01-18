using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models;

public class RateChange
{
    public DateTime EffectiveDate { get; set; }      // Date the new rate starts (aligned to a payment period)
    public decimal AnnualRatePercent { get; set; }   // e.g., 8.5m for 8.5%
}

