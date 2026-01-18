namespace DominateDocsData.Models;

public class PaymentPeriod
{
    public int PeriodNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Payment { get; set; }
    public decimal Interest { get; set; }
    public decimal Principal { get; set; }
    public decimal EndingBalance { get; set; }
    public decimal AnnualRatePercent { get; set; }
}