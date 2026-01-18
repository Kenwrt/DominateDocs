using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models;

public sealed class LoanScheduleResult
{
    public decimal Principal { get; init; }
    public decimal AnnualInterestRatePercent { get; init; }
    public int AmortizationTermMonths { get; init; }
    public int BalloonTermMonths { get; init; }
    public int PaymentsPerYear { get; init; }
    public decimal ScheduledPayment { get; init; }
    public decimal BalloonAmount { get; init; }
    public IReadOnlyList<PaymentRow> Rows { get; init; } = Array.Empty<PaymentRow>();
    public LoanTotals Totals { get; init; } = new();
}

public sealed class LoanTotals
{
    public decimal TotalRegularPayments { get; init; }
    public decimal TotalInterestBeforeBalloon { get; init; }
    public decimal TotalPaidIncludingBalloon { get; init; }
}

