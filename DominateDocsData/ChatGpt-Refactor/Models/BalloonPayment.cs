namespace LiquidDocsData.Models;

public class BalloonPayments
{
    public List<PaymentRow> PaymentPeriods { get; set; }
    public int BalloonTermMonths { get; set; }
    public DateOnly DueDate { get; set; }
    public int PaymentsPerYear { get; set; }
    public decimal ScheduledPayment { get; set; }
    public decimal BalloonAmount { get; set; }
    public decimal TotalRegularPayments { get; set; }
    public decimal TotalInterestBeforeBalloon { get; set; }
    public decimal TotalPaidIncludingBalloon { get; set; }
}



