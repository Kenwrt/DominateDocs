using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiquidDocsData.Models;


    // ... all the index stuff, loan terms, payment rows ...

    public class SorRPaymentSchedule
    {
        public string RateLabel { get; init; } = "";
        public string Disclaimer { get; init; } = "Projected schedule. Actual payments vary with the applicable index, margin, and loan terms.";
        public IReadOnlyList<PaymentRow> Rows { get; init; } = Array.Empty<PaymentRow>();
        public decimal TotalInterest => Rows.Sum(r => r.InterestComponent);
        public decimal TotalPrincipal => Rows.Sum(r => r.PrincipalComponent);
        public decimal FinalBalance => Rows.LastOrDefault()?.EndingBalance ?? 0m;
    }
