using DnsClient.Internal;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;

namespace DocumentManager.CalculatorsSchedulers;

public class BalloonPaymentCalculater : IBalloonPaymentCalculater
{
    private ILogger<BalloonPaymentCalculater> logger;

    public BalloonPaymentCalculater(ILogger<BalloonPaymentCalculater> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Generates a fixed-rate amortization schedule with a balloon payoff.
    /// - Scheduled payment is computed from AmortizationTermMonths.
    /// - Schedule runs to BalloonTermMonths, then adds a balloon payoff row.
    /// </summary>
    ///

    public BalloonPayments Generate(decimal principal, decimal annualRatePercent, int amortizationTermMonths, int balloonTermMonths, DateTime firstPaymentDate, int paymentsPerYear = 12)
    {
        BalloonPayments result;
        decimal scheduledPayment = 0m;
        decimal balloonAmount = 0m;
        List<PaymentRow> rows = new List<PaymentRow>();
        List<PaymentRow> regulars = new List<PaymentRow>();

        try
        {
            if (principal <= 0) throw new ArgumentOutOfRangeException(nameof(principal));
            if (annualRatePercent < 0) throw new ArgumentOutOfRangeException(nameof(annualRatePercent));
            if (amortizationTermMonths <= 0) throw new ArgumentOutOfRangeException(nameof(amortizationTermMonths));
            if (balloonTermMonths <= 0) throw new ArgumentOutOfRangeException(nameof(balloonTermMonths));
            if (paymentsPerYear <= 0) throw new ArgumentOutOfRangeException(nameof(paymentsPerYear));

            var r = (annualRatePercent / 100m) / paymentsPerYear;
            var n = amortizationTermMonths;

            // Scheduled level payment based on full amortization term.
            scheduledPayment = r == 0m
                ? Round(principal / n)
                : Round(principal * r / (1 - (decimal)Math.Pow(1 + (double)r, -n)));

            rows = new List<PaymentRow>(balloonTermMonths + 1);
            var balance = principal;
            var monthsToPay = Math.Min(balloonTermMonths, amortizationTermMonths);

            for (int k = 1; k <= monthsToPay; k++)
            {
                var interest = Round(balance * r);
                var principalPaid = (r == 0m) ? Round(scheduledPayment) : Round(scheduledPayment - interest);

                if (principalPaid > balance)
                {
                    principalPaid = balance;
                    // adjust payment on the final amortizing month if it exactly pays off
                    if (r != 0m) scheduledPayment = Round(interest + principalPaid);
                }

                var endBalance = Round(balance - principalPaid);

                rows.Add(new PaymentRow
                {
                    MonthNumber = k,
                    DueDate = AddMonthsSafe(firstPaymentDate, k - 1),
                    Payment = scheduledPayment,
                    Interest = interest,
                    Principal = principalPaid,
                    EndingBalance = endBalance,
                    IsBalloon = false
                });

                balance = endBalance;
            }

            // If not fully amortized by the balloon month, add a balloon payoff row.
            balloonAmount = 0m;
            if (balance > 0)
            {
                balloonAmount = Round(balance);
                rows.Add(new PaymentRow
                {
                    MonthNumber = monthsToPay + 1,
                    DueDate = AddMonthsSafe(firstPaymentDate, monthsToPay),
                    Payment = balloonAmount,
                    Interest = 0m,
                    Principal = balloonAmount,
                    EndingBalance = 0m,
                    IsBalloon = true
                });
                balance = 0m;
            }

            regulars = rows.Where(x => !x.IsBalloon).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return new BalloonPayments
        {
            TotalInterestBeforeBalloon = regulars.Sum(x => x.Interest),
            TotalRegularPayments = regulars.Sum(x => x.Payment),
            TotalPaidIncludingBalloon = regulars.Sum(x => x.Payment) + balloonAmount,
            BalloonTermMonths = balloonTermMonths,
            PaymentsPerYear = paymentsPerYear,
            ScheduledPayment = scheduledPayment,
            BalloonAmount = balloonAmount,
            PaymentPeriods = rows,
        };
    }

    private decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    // Keeps day-of-month stable; clamps to month-end if needed (e.g., from Jan 31 to Feb 28/29).
    private DateTime AddMonthsSafe(DateTime date, int months)
    {
        DateTime target = date;

        try
        {
            target = date.AddMonths(months);

            if (date.Day != target.Day)
            {
                var dim = DateTime.DaysInMonth(target.Year, target.Month);
                return new DateTime(target.Year, target.Month, Math.Min(date.Day, dim));
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return target;
    }
}