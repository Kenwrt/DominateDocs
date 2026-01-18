using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Enums;

public class Stripe
{
    public enum BillableUnit
    {
        LoanDocumentSet = 1
    }

    public enum TieringMode
    {
        // "Volume" pricing: once a threshold is reached, ALL units are billed at the tier unit price.
        Volume = 1,

        // "Graduated" pricing: units are priced per band.
        Graduated = 2
    }


    public enum BillingStatementStatus
    {
        Draft = 1,
        Finalized = 2,
        Sent = 3,
        Paid = 4,
        Void = 5
    }

    public enum AdjustmentType
    {
        Charge = 1,
        Credit = 2
    }



}
