namespace DominateDocsData.Models;

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