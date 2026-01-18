using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models;

public class PaymentRow
{
    public int MonthNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Payment { get; set; }
    public decimal Interest { get; set; }
    public decimal Principal { get; set; }
    public decimal EndingBalance { get; set; }
    public bool IsBalloon { get; set; }

    public decimal? IndexPercent { get; init; }

    public decimal BeginningBalance { get; init; }

    public decimal InterestComponent { get; init; }

    public decimal PrincipalComponent { get; init; }
}
   