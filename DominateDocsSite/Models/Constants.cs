namespace DominateDocsSite.Models;

/// <summary>Application-wide constants for dropdowns and reference data.</summary>
public static class Constants
{
    public static readonly string[] USStates =
    [
        "Alabama","Alaska","Arizona","Arkansas","California","Colorado","Connecticut",
        "Delaware","Florida","Georgia","Hawaii","Idaho","Illinois","Indiana","Iowa",
        "Kansas","Kentucky","Louisiana","Maine","Maryland","Massachusetts","Michigan",
        "Minnesota","Mississippi","Missouri","Montana","Nebraska","Nevada","New Hampshire",
        "New Jersey","New Mexico","New York","North Carolina","North Dakota","Ohio",
        "Oklahoma","Oregon","Pennsylvania","Rhode Island","South Carolina","South Dakota",
        "Tennessee","Texas","Utah","Vermont","Virginia","Washington","West Virginia",
        "Wisconsin","Wyoming","District of Columbia"
    ];

    public static readonly string[] HoldbackPaymentOptions =
    [
        "Wire Instructions To Be Provided",
        "Paid Outside Of Closing",
        "Deliver to Servicer",
        "Deliver to Construction Fund Control",
        "Deliver to Lender",
        "To Be Net Funded"
    ];

    public static readonly string[] LenderFeeDescriptions =
    [
        "Origination Fee",
        "Underwriting Fee",
        "Processing Fee",
        "Document Preparation Fee",
        "Commitment Fee",
        "Administration Fee",
        "Wire Fee",
        "Inspection Fee",
        "Appraisal Fee"
    ];

    public static readonly string[] LenderFeeNotes =
    [
        "Paid at Closing",
        "Non-Refundable",
        "Refundable if Loan Does Not Close",
        "Deducted from Loan Proceeds",
        "Paid Outside of Closing",
        "Collected at Application"
    ];

    public static readonly string[] BrokerFeeDescriptions =
    [
        "Broker Fee",
        "Origination Fee",
        "Processing Fee",
        "Referral Fee",
        "Yield Spread Premium"
    ];

    public static readonly string[] BrokerFeeNotes =
    [
        "Paid at Closing",
        "Paid by Lender",
        "Paid by Borrower",
        "Split Between Parties"
    ];

    public static readonly string[] OtherFeeDescriptions =
    [
        "Title Insurance",
        "Escrow Fee",
        "Recording Fee",
        "Notary Fee",
        "Tax Service Fee",
        "Flood Certification",
        "Credit Report Fee",
        "Environmental Report"
    ];

    public static readonly string[] OtherFeeNotes =
    [
        "Paid at Closing",
        "Paid Outside of Closing",
        "Borrower Responsibility",
        "Third Party Fee"
    ];

    public static readonly string[] OwnerRoles =
    [
        "Managing Member",
        "Member",
        "Manager",
        "General Partner",
        "Limited Partner",
        "President",
        "CEO",
        "Director",
        "Shareholder"
    ];

    public static readonly string[] SignatoryTitles =
    [
        "Member",
        "Manager",
        "Managing Member",
        "President",
        "CEO",
        "Authorized Signer",
        "General Partner",
        "Trustee",
        "Secretary",
        "Vice President"
    ];
}
