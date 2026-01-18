using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace DocumentManager.Services;

/// <summary>
/// One-stop shop for:
/// 1) Pulling index rates (SOFR via NY Fed; Prime via FRED if configured)
/// 2) Generating projected payment schedules for fixed/variable loans
/// </summary>
public class FetchCurrentIndexRatesAndSchedulesService : IFetchCurrentIndexRatesAndSchedulesService
{
    private readonly HttpClient http;
    private ILogger<FetchCurrentIndexRatesAndSchedulesService> logger;

    private LoanTerms sampleTerms;

    public FetchCurrentIndexRatesAndSchedulesService(HttpClient httpClient, ILogger<FetchCurrentIndexRatesAndSchedulesService> logger)
    {
        http = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.logger = logger;

        sampleTerms = new LoanTerms()
        {
            Principal = 5_000_000m,
            TermMonths = 24,
            FirstPaymentDate = DateTime.Today.AddMonths(1),
            Structure = PaymentStructure.InterestOnly,
            RateMode = RateMode.Variable,
            IndexName = "SOFR",
            MarginPercent = 4.50m,
            AdjustmentIntervalMonths = 3,
            Projection = new IndexProjection
            {
                StartIndexPercent = 5.30m, // fallback if offline
                BasisPointsPerReset = 25m               // +25 bps each reset
            },
            Caps = new RateCaps
            {
                PeriodicCapPct = 1.00m,
                LifetimeCapAbovePct = 5.00m,
                LifetimeFloorBelowPct = 5.00m
            }
        };
    }

    /// <summary>
    /// Gets the latest published SOFR (percent per annum) from the NY Fed public API.
    /// Returns null if unreachable or unexpected payload.
    /// </summary>
    public async Task<decimal?> GetLatestSofrAsync(CancellationToken ct = default)
    {
        // Example: https://markets.newyorkfed.org/api/rates/secured/sofr/last/1.json
        using var req = new HttpRequestMessage(HttpMethod.Get,
            "https://markets.newyorkfed.org/api/rates/secured/sofr/last/1.json");

        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        try
        {
            var doc = JsonDocument.Parse(json);
            // Typical structure: { "refRates":[{"effectiveDate":"YYYY-MM-DD","percentRate":"5.30"}] }
            if (doc.RootElement.TryGetProperty("refRates", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
            {
                var first = arr[0];
                if (first.TryGetProperty("percentRate", out var rateNode))
                {
                    if (decimal.TryParse(rateNode.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var pct))
                        return pct; // percent, e.g., 5.30 means 5.30% p.a.
                }
            }
        }
        catch
        {
            // swallow; return null
        }
        return null;
    }

    /// <summary>
    /// Gets the last observation for a given FRED series (e.g., "MPRIME" for WSJ Prime Rate, monthly).
    /// Returns null if unreachable. Requires a valid FRED API key.
    /// </summary>
    public async Task<decimal?> GetLatestFromFredAsync(string fredSeriesId, string fredApiKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fredSeriesId) || string.IsNullOrWhiteSpace(fredApiKey))
            return null;

        // Example:
        // https://api.stlouisfed.org/fred/series/observations?series_id=MPRIME&api_key=KEY&file_type=json&sort_order=desc&limit=1
        var url = $"https://api.stlouisfed.org/fred/series/observations?series_id={Uri.EscapeDataString(fredSeriesId)}&api_key={Uri.EscapeDataString(fredApiKey)}&file_type=json&sort_order=desc&limit=1";

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        using var resp = await http.SendAsync(req, ct).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode) return null;

        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        try
        {
            var fred = JsonSerializer.Deserialize<FredObsResponse>(json);
            var last = fred?.observations?.FirstOrDefault();
            if (last != null && decimal.TryParse(last.value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                return v; // percent per annum
        }
        catch
        {
        }
        return null;
    }

    private class FredObsResponse
    {
        public List<FredObservation>? observations { get; set; }
    }

    private class FredObservation
    {
        public string? date { get; set; }
        public string? value { get; set; }
    }

    public enum PaymentStructure
    {
        InterestOnly,
        FullyAmortizing,
        PartiallyAmortizingBalloon
    }

    public enum RateMode
    {
        Fixed,
        Variable
    }

    /// <summary>
    /// Defines how to project the underlying index path across resets.
    /// </summary>
    public class IndexProjection
    {
        /// <summary>
        /// Starting index (percent p.a.), e.g., 5.30 = 5.30% p.a.
        /// </summary>
        public decimal StartIndexPercent { get; init; }

        /// <summary>
        /// Growth in basis points applied at EACH reset. Can be negative. Example: +25 bps per reset = 0.25%.
        /// </summary>
        public decimal BasisPointsPerReset { get; init; } = 0m;

        /// <summary>
        /// Optional: explicit override curve per reset (percent p.a.). If provided and length > 0,
        /// this wins over BasisPointsPerReset. Index at reset k = Curve[k] if available.
        /// </summary>
        public List<decimal>? ExplicitResetCurvePercents { get; init; }
    }

    public class RateCaps
    {
        /// <summary>
        /// Max change per reset in percentage points. Null means unlimited. Example: 1.0m caps a +/-1% change per reset.
        /// </summary>
        public decimal? PeriodicCapPct { get; init; }

        /// <summary>
        /// Lifetime cap: maximum total rate above initial fully-indexed rate. Null = unlimited.
        /// </summary>
        public decimal? LifetimeCapAbovePct { get; init; }

        /// <summary>
        /// Lifetime floor: minimum total rate relative to initial fully-indexed rate. Null = unlimited.
        /// Typically a non-negative value meaning "can't fall more than X% below start."
        /// </summary>
        public decimal? LifetimeFloorBelowPct { get; init; }
    }

    public class LoanTerms
    {
        public decimal Principal { get; set; }
        public int TermMonths { get; set; }
        public DateTime FirstPaymentDate { get; set; }

        public PaymentStructure Structure { get; set; } = PaymentStructure.InterestOnly;

        public RateMode RateMode { get; set; } = RateMode.Variable;

        /// <summary> If RateMode == Fixed, set FixedRatePercent. </summary>
        public decimal? FixedRatePercent { get; set; }

        /// <summary> If RateMode == Variable, set IndexName, MarginPercent, AdjustmentIntervalMonths, Projection, Caps (optional). </summary>
        public string? IndexName { get; set; }

        public decimal MarginPercent { get; set; } = 0m;
        public int AdjustmentIntervalMonths { get; set; } = 12;
        public IndexProjection? Projection { get; set; }
        public RateCaps? Caps { get; set; }

        /// <summary>
        /// For fully amortizing structures, you can specify an amortization length that differs from TermMonths.
        /// If null, uses remaining term at each reset.
        /// </summary>
        public int? AmortizationMonths { get; set; }

        /// <summary>
        /// For Partial/ Balloon: month at which balloon occurs. If null, balloon at Term end.
        /// </summary>
        public int? BalloonMonth { get; set; }
    }

    public class PaymentRow
    {
        public int PeriodNumber { get; init; }
        public DateTime PaymentDate { get; init; }

        /// <summary> Index value for the period, percent p.a. Null for fixed-rate. </summary>
        public decimal? IndexPercent { get; init; }

        /// <summary> Fully-indexed rate (index + margin for variable, or fixed), percent p.a. </summary>
        public decimal RatePercent { get; init; }

        public decimal BeginningBalance { get; init; }
        public decimal ScheduledPayment { get; init; }
        public decimal InterestComponent { get; init; }
        public decimal PrincipalComponent { get; init; }
        public decimal EndingBalance { get; init; }
        public bool IsBalloon { get; init; }
    }

    public class ScheduleResult
    {
        public string RateLabel { get; init; } = "";
        public string Disclaimer { get; init; } = "Projected schedule. Actual payments vary with the applicable index, margin, and loan terms.";
        public IReadOnlyList<PaymentRow> Rows { get; init; } = Array.Empty<PaymentRow>();
        public decimal TotalInterest => Rows.Sum(r => r.InterestComponent);
        public decimal TotalPrincipal => Rows.Sum(r => r.PrincipalComponent);
        public decimal FinalBalance => Rows.LastOrDefault()?.EndingBalance ?? 0m;
    }

    /// <summary>
    /// Generate a projected schedule based on the provided terms.
    /// </summary>
    public ScheduleResult GenerateProjectedSchedule(LoanTerms terms)
    {
        //if (terms is null) throw new ArgumentNullException(nameof(terms));
        //if (terms.TermMonths <= 0) throw new ArgumentOutOfRangeException(nameof(terms.TermMonths));
        //if (terms.Principal <= 0) throw new ArgumentOutOfRangeException(nameof(terms.Principal));

        var rows = new List<PaymentRow>(capacity: terms.TermMonths);
        var bal = terms.Principal;
        var date = terms.FirstPaymentDate;

        var startIndexPercent = GetLatestSofrAsync().Result;

        terms = sampleTerms;

        terms.Projection = new IndexProjection
        {
            StartIndexPercent = startIndexPercent ?? terms.Projection?.StartIndexPercent ?? 5.30m,
            BasisPointsPerReset = terms.Projection?.BasisPointsPerReset ?? 25m,
            ExplicitResetCurvePercents = terms.Projection?.ExplicitResetCurvePercents
        };

        // Determine starting fully-indexed rate
        decimal startRatePct = terms.RateMode == RateMode.Fixed
            ? (terms.FixedRatePercent ?? throw new ArgumentException("FixedRatePercent is required for fixed loans."))
            : ((terms.Projection?.StartIndexPercent ?? 0m) + terms.MarginPercent);

        decimal initialFullyIndexedRate = startRatePct;

        int balloonAt = Math.Clamp(terms.BalloonMonth ?? terms.TermMonths, 1, terms.TermMonths);

        for (int p = 1; p <= terms.TermMonths; p++)
        {
            // Determine the rate for this period
            decimal periodRatePct;
            decimal? indexPctThisPeriod = null;

            if (terms.RateMode == RateMode.Fixed)
            {
                periodRatePct = terms.FixedRatePercent!.Value;
            }
            else
            {
                // If at reset boundary (or first period), compute index at this reset
                // Resets occur at months 1, 1+Adj, 1+2*Adj, ...
                int resetsSoFar = (p - 1) / Math.Max(1, terms.AdjustmentIntervalMonths);

                var indexPct = GetProjectedIndexAtReset(terms.Projection!, resetsSoFar);
                // Apply periodic caps relative to prior fully-indexed rate
                periodRatePct = ApplyCaps(
                    previousRatePct: rows.Count == 0 ? initialFullyIndexedRate : rows.Last().RatePercent,
                    proposedRatePct: indexPct + terms.MarginPercent,
                    initialRatePct: initialFullyIndexedRate,
                    caps: terms.Caps
                );
                indexPctThisPeriod = periodRatePct - terms.MarginPercent;
            }

            // Convert annual percent to monthly decimal rate
            var r = (double)(periodRatePct / 100m / 12m);

            decimal payment;
            decimal interest;
            decimal principal;

            bool isBalloonThisPeriod = terms.Structure == PaymentStructure.PartiallyAmortizingBalloon && p == balloonAt;

            if (terms.Structure == PaymentStructure.InterestOnly && !isBalloonThisPeriod)
            {
                interest = RoundMoney(bal * (decimal)r);
                principal = 0m;
                payment = interest;
            }
            else if (isBalloonThisPeriod)
            {
                // Last regular payment + balloon of remaining balance
                interest = RoundMoney(bal * (decimal)r);
                principal = bal; // wipe it
                payment = interest + principal;
            }
            else
            {
                // Amortizing payment
                var remaining = terms.TermMonths - p + 1;
                var amortMonths = terms.AmortizationMonths.HasValue
                    ? Math.Max(1, terms.AmortizationMonths.Value - (terms.TermMonths - remaining))
                    : remaining;

                payment = ComputeAmortizingPayment(bal, (decimal)r, amortMonths);

                interest = RoundMoney(bal * (decimal)r);
                principal = payment - interest;

                // Ensure we don’t overshoot last tiny balances due to rounding
                if (principal > bal) { principal = bal; payment = interest + principal; }
            }

            var endBal = bal - principal;

            rows.Add(new PaymentRow
            {
                PeriodNumber = p,
                PaymentDate = date,
                IndexPercent = indexPctThisPeriod,
                RatePercent = periodRatePct,
                BeginningBalance = RoundMoney(bal),
                ScheduledPayment = RoundMoney(payment),
                InterestComponent = RoundMoney(interest),
                PrincipalComponent = RoundMoney(principal),
                EndingBalance = RoundMoney(endBal),
                IsBalloon = isBalloonThisPeriod
            });

            bal = endBal;
            date = date.AddMonths(1);
        }

        var rateLabel = terms.RateMode == RateMode.Fixed
            ? $"Fixed {terms.FixedRatePercent!.Value:F3}%"
            : $"{terms.IndexName ?? "Index"} + {terms.MarginPercent:F3}% (initial FI rate {initialFullyIndexedRate:F3}%)";

        return new ScheduleResult
        {
            RateLabel = rateLabel,
            Rows = rows
        };
    }

    private static decimal ComputeAmortizingPayment(decimal principal, decimal monthlyRate, int nMonths)
    {
        if (principal <= 0m) return 0m;
        if (nMonths <= 0) return principal;
        if (monthlyRate <= 0m) return RoundMoney(principal / nMonths);

        var r = (double)monthlyRate;
        var p = (double)principal;
        var n = nMonths;

        var pay = p * r / (1 - Math.Pow(1 + r, -n));
        return RoundMoney((decimal)pay);
    }

    private static decimal RoundMoney(decimal x) => Math.Round(x, 2, MidpointRounding.ToEven);

    private static decimal GetProjectedIndexAtReset(IndexProjection projection, int resetNumber)
    {
        if (resetNumber <= 0) return projection.StartIndexPercent;

        if (projection.ExplicitResetCurvePercents != null && projection.ExplicitResetCurvePercents.Count > 0)
        {
            // If curve shorter than needed, hold last value
            if (resetNumber < projection.ExplicitResetCurvePercents.Count)
                return projection.ExplicitResetCurvePercents[resetNumber];

            return projection.ExplicitResetCurvePercents.Last();
        }

        var bumpPct = projection.BasisPointsPerReset / 100m; // bps to percent
        return projection.StartIndexPercent + bumpPct * resetNumber;
    }

    private static decimal ApplyCaps(decimal previousRatePct, decimal proposedRatePct, decimal initialRatePct, RateCaps? caps)
    {
        if (caps == null) return proposedRatePct;

        var result = proposedRatePct;

        // Periodic cap
        if (caps.PeriodicCapPct.HasValue)
        {
            var maxUp = previousRatePct + caps.PeriodicCapPct.Value;
            var maxDown = previousRatePct - caps.PeriodicCapPct.Value;
            result = Clamp(result, maxDown, maxUp);
        }

        // Lifetime caps relative to initial fully indexed
        if (caps.LifetimeCapAbovePct.HasValue)
        {
            var max = initialRatePct + caps.LifetimeCapAbovePct.Value;
            result = Math.Min(result, max);
        }
        if (caps.LifetimeFloorBelowPct.HasValue)
        {
            var min = initialRatePct - caps.LifetimeFloorBelowPct.Value;
            result = Math.Max(result, min);
        }

        return result;
    }

    private static decimal Clamp(decimal x, decimal min, decimal max) => x < min ? min : (x > max ? max : x);
}