using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models;

public class PaymentSchedule
{
    public IReadOnlyList<PaymentPeriod> Periods { get; set; } = Array.Empty<PaymentPeriod>();
    public decimal TotalPayments { get; set; }
    public decimal TotalInterest { get; set; }
    public decimal FinancedPrincipal { get; set; }
    public int PeriodCount { get; set; }
    public int MyProperty { get; set; }

    public List<RateChange> RateChangeList { get; set; }

   
            
            
            
            
}






