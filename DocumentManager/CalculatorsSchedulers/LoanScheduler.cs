using DominateDocsData.Enums;
using DominateDocsData.Models;
using Microsoft.Extensions.Logging;

namespace DocumentManager.CalculatorsSchedulers;

public class LoanScheduler : ILoanScheduler
{
    private ILogger<LoanScheduler> logger;

    public LoanScheduler(ILogger<LoanScheduler> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Generate schedule for a FIXED interest loan.
    /// </summary>
    /// <param name="principal">Purchase price or gross principal before down payment.</param>
    /// <param name="annualRatePercent">Nominal APR in percent. Example: 9.25 means 9.25%.</param>
    /// <param name="downPaymentPercent">Percent of principal paid upfront. Example: 25 means 25%.</param>
    /// <param name="startDate">First payment date (or anchor date). Payments align monthly to this date using same-day or EOM rule.</param>
    /// <param name="endDate">Final scheduled payment date. Determines number of periods.</param>
    /// <param name="amortizationType">FullyAmortized, PartiallyAmortized, InterestOnly.</param>
    /// <param name="amortizationTermMonths">Only used for PartiallyAmortized; if null, defaults to term months.</param>
    /// <returns>PaymentScheduleResult with rows, totals, and balloon.</returns>
    public PaymentSchedule GenerateFixed(decimal principal, decimal annualRatePercent, decimal downPaymentPercent, DateTime startDate, DateTime endDate, Payment.AmortizationTypes amortizationType, int? amortizationTermMonths = null)
    {
        List<PaymentPeriod> periods = new();

        decimal financed = 0;

        try
        {
            ValidateDates(startDate, endDate);

            int n = CountMonthlyPeriods(startDate, endDate);

            if (n <= 0) return EmptyResult(principal, downPaymentPercent);

            financed = principal * (1 - downPaymentPercent / 100m);

            financed = RoundMoney(financed);

            periods = new List<PaymentPeriod>(n);

            decimal balance = financed;
            decimal iPer = annualRatePercent / 100m / 12m;

            // Determine payment amount strategy
            decimal pmt = amortizationType switch
            {
                Payment.AmortizationTypes.InterestOnly => 0m, // computed each period as interest-only
                Payment.AmortizationTypes.FullyAmortized => ComputePayment(balance, iPer, n),
                Payment.AmortizationTypes.PartiallyAmortized => ComputePayment(balance, iPer, amortizationTermMonths is > 0 ? amortizationTermMonths.Value : n),
                _ => throw new ArgumentOutOfRangeException(nameof(amortizationType))
            };

            DateTime due = startDate;
            for (int k = 1; k <= n; k++)
            {
                due = AlignMonthly(due, k == 1 ? 0 : 1);

                decimal interest = RoundMoney(balance * iPer);
                decimal principalPay;

                decimal periodPayment = amortizationType switch
                {
                    Payment.AmortizationTypes.InterestOnly => interest,        // interest-only periodic payment
                    _ => pmt
                };

                // Prevent overpay on the last period
                if (periodPayment > balance + interest) periodPayment = RoundMoney(balance + interest);

                principalPay = RoundMoney(periodPayment - interest);
                balance = RoundMoney(balance - principalPay);

                periods.Add(new PaymentPeriod
                {
                    PeriodNumber = k,
                    DueDate = due,
                    Payment = periodPayment,
                    Interest = interest,
                    Principal = principalPay,
                    EndingBalance = balance,
                    AnnualRatePercent = annualRatePercent
                });
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return new PaymentSchedule
        {
            TotalInterest = periods.Sum(x => x.Interest),
            TotalPayments = periods.Sum(x => x.Payment),
            PeriodCount = periods.Count,
            Periods = periods,
        };
    }

    /// <summary>
    /// Generate schedule for a VARIABLE rate loan. Payments are recast on each rate change to amortize the remaining balance over remaining periods,
    /// except InterestOnly which always remains interest-only.
    /// </summary>
    /// <param name="principal">Gross principal before down payment.</param>
    /// <param name="downPaymentPercent">Percent down.</param>
    /// <param name="startDate">Start date for periodization.</param>
    /// <param name="endDate">End date for final scheduled payment.</param>
    /// <param name="amortizationType">FullyAmortized, PartiallyAmortized, InterestOnly.</param>
    /// <param name="rateSchedule">Ordered list of RateChange. The first item must be on or before startDate. If empty, throws.</param>
    /// <param name="amortizationTermMonths">Only used for PartiallyAmortized; amortization horizon used to compute PMT at each reset.</param>
    public PaymentSchedule GenerateVariable(decimal principal, decimal downPaymentPercent, DateTime startDate, DateTime endDate, Payment.AmortizationTypes amortizationType, List<RateChange> rateSchedule, int? amortizationTermMonths = null)
    {
        List<PaymentPeriod> periods = new();

        decimal financed = 0;

        try
        {
            ValidateDates(startDate, endDate);

            if (rateSchedule == null || rateSchedule.Count == 0)
                throw new ArgumentException("rateSchedule must contain at least one RateChange covering startDate.");

            int n = CountMonthlyPeriods(startDate, endDate);

            if (n <= 0) return EmptyResult(principal, downPaymentPercent);

            var ordered = rateSchedule.OrderBy(r => r.EffectiveDate).ToList();

            if (ordered[0].EffectiveDate > startDate)
                throw new ArgumentException("First rate change must be on or before the startDate.");

            financed = RoundMoney(principal * (1 - downPaymentPercent / 100m));

            periods = new List<PaymentPeriod>(n);

            decimal balance = financed;
            DateTime due = startDate;

            // Build a per-period rate map
            var perPeriodRates = new decimal[n];
            for (int k = 0; k < n; k++)
            {
                DateTime periodDate = AlignMonthly(startDate, k);
                perPeriodRates[k] = GetAnnualRateForDate(ordered, periodDate);
            }

            // Payment amount may change at resets (for non-IO types)
            int remaining = n;
            decimal currentAnnual = perPeriodRates[0];
            decimal iPer = currentAnnual / 100m / 12m;
            int currentIndex = 0;

            // initial PMT
            decimal pmt = amortizationType switch
            {
                Payment.AmortizationTypes.InterestOnly => 0m,
                Payment.AmortizationTypes.FullyAmortized => ComputePayment(balance, iPer, remaining),
                Payment.AmortizationTypes.PartiallyAmortized => ComputePayment(balance, iPer, amortizationTermMonths is > 0 ? amortizationTermMonths.Value : remaining),
                _ => throw new ArgumentOutOfRangeException(nameof(amortizationType))
            };

            for (int k = 1; k <= n; k++)
            {
                due = AlignMonthly(due, k == 1 ? 0 : 1);

                // Check for reset at this period
                currentIndex = k - 1;
                decimal thisAnnual = perPeriodRates[currentIndex];
                if (amortizationType != Payment.AmortizationTypes.InterestOnly && thisAnnual != currentAnnual)
                {
                    currentAnnual = thisAnnual;
                    iPer = currentAnnual / 100m / 12m;
                    int rem = n - (k - 1);
                    pmt = amortizationType == Payment.AmortizationTypes.FullyAmortized
                        ? ComputePayment(balance, iPer, rem)
                        : ComputePayment(balance, iPer, amortizationTermMonths is > 0 ? Math.Max(1, amortizationTermMonths.Value - (n - rem)) : rem);
                }
                else
                {
                    iPer = thisAnnual / 100m / 12m;
                }

                decimal interest = RoundMoney(balance * iPer);
                decimal periodPayment = amortizationType == Payment.AmortizationTypes.InterestOnly ? interest : pmt;

                if (periodPayment > balance + interest) periodPayment = RoundMoney(balance + interest);

                decimal principalPay = RoundMoney(periodPayment - interest);
                balance = RoundMoney(balance - principalPay);

                periods.Add(new PaymentPeriod
                {
                    PeriodNumber = k,
                    DueDate = due,
                    Payment = periodPayment,
                    Interest = interest,
                    Principal = principalPay,
                    EndingBalance = balance,
                    AnnualRatePercent = thisAnnual
                });

                remaining--;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return new PaymentSchedule
        {
            Periods = periods,
            FinancedPrincipal = financed
        };
    }

    // --------------------
    // Helpers
    // --------------------

    private decimal ComputePayment(decimal principal, decimal ratePerPeriod, int periods)
    {
        decimal pmt = 0;

        try
        {
            if (periods <= 0) return 0m;
            if (principal <= 0) return 0m;

            if (ratePerPeriod == 0m)
            {
                return RoundMoney(principal / periods);
            }

            // PMT = P * r / (1 - (1+r)^-n)
            decimal r = ratePerPeriod;
            decimal pow = (decimal)Math.Pow((double)(1m + r), -periods);
            pmt = principal * r / (1m - pow);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return RoundMoney(pmt);
    }

    private int CountMonthlyPeriods(DateTime start, DateTime end)
    {
        int count = 0;

        try
        {
            if (end <= start) return 0;

            // Count monthly boundaries using same-day-or-EOM convention
            count = 0;
            DateTime cursor = start;
            while (true)
            {
                DateTime next = AlignMonthly(cursor, 1);
                if (next > end) break;
                count++;
                cursor = next;
            }

            // If end aligns exactly with one more step, include it
            if (cursor < end && SameDayOrEOM(AlignMonthly(cursor, 1), end))
            {
                count++;
            }

            // If no exact alignment, approximate by calendar month difference
            if (count == 0)
            {
                count = ((end.Year - start.Year) * 12 + end.Month - start.Month);
                if (count <= 0) count = 1;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return count;
    }

    private bool SameDayOrEOM(DateTime a, DateTime b)
    {
        bool bothEom = a == EndOfMonth(a) && b == EndOfMonth(b);
        return bothEom || a.Day == b.Day;
    }

    private DateTime AlignMonthly(DateTime anchor, int monthsToAdd)
    {
        var target = anchor.AddMonths(monthsToAdd);
        int day = Math.Min(anchor.Day, DateTime.DaysInMonth(target.Year, target.Month));
        // Same-day if possible; otherwise EOM
        return new DateTime(target.Year, target.Month, day);
    }

    private DateTime EndOfMonth(DateTime d) => new DateTime(d.Year, d.Month, DateTime.DaysInMonth(d.Year, d.Month));

    private decimal RoundMoney(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private void ValidateDates(DateTime start, DateTime end)
    {
        if (end <= start)
            throw new ArgumentException("endDate must be after startDate.");
    }

    private decimal GetAnnualRateForDate(IReadOnlyList<RateChange> schedule, DateTime date)
    {
        RateChange? current = null;

        try
        {
            // last rate whose EffectiveDate <= date

            foreach (var rc in schedule)
            {
                if (rc.EffectiveDate <= date) current = rc; else break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
        }

        return current?.AnnualRatePercent
               ?? throw new InvalidOperationException($"No rate covers date {date:d}.");
    }

    private PaymentSchedule EmptyResult(decimal principal, decimal downPaymentPercent) =>
        new PaymentSchedule
        {
            Periods = Array.Empty<PaymentPeriod>(),
            FinancedPrincipal = RoundMoney(principal * (1 - downPaymentPercent / 100m))
        };
}