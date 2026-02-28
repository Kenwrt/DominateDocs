using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class LoanAgreement
{
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.Schedules AdjustmentInterval { get; set; }

    public int AdjustmentIntervalMonths { get; set; } = 0;

    /// <summary>
    /// When true, this loan is being run from the Admin Bench and worker(s) should apply overrides + produce trace.
    /// </summary>
    public AdminBenchOverrides AdminBench { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.AmortizationTypes AmorizationType { get; set; } = Payment.AmortizationTypes.InterestOnly;

    public List<Assignee> Assignees { get; set; } = [];

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.IndexPaths AssumedIndexPath { get; set; }

    public BalloonPayments BalloonPayments { get; set; } = new();

    public decimal BasisPointsPerReset { get; set; } = 25m;

    public string? BorrowerCode { get; set; }

    public string BorrowerNames { get; set; }

    public List<Borrower> Borrowers { get; set; } = new();

    public string? BrokerCode { get; set; }

    public List<FeeToBePaid> BrokerFees { get; set; } = [];

    public string BrokerNames { get; set; }

    public List<Broker> Brokers { get; set; } = new();

    public string ClosingContact { get; set; } = string.Empty;

    public string ClosingContactEmail { get; set; } = string.Empty;

    public string ClosingContactName { get; set; }

    public List<Contractor> Contractors { get; set; } = new();

    public string DefaultLender { get; set; }

    public decimal DefaultRate { get; set; } = 0.00M;

    public decimal DSCRRatio { get; set; } = 0.00M;

    public bool IsDSCRReserveAccount { get; set; } = false;

    /// <summary>
    /// Optional Doc Library override used by Admin Bench.
    /// </summary>
    public Guid DocLibId { get; set; } = Guid.Empty;

    public List<DocumentDelivery> DocumentDeliverys { get; set; } = new();

    [NotMapped]
    [BsonIgnore]
    public string DocumentTitle { get; set; }

    public decimal DownPaymentAmmount { get; set; } = 0.00M;

    public decimal DownPaymentPercentage { get; set; } = 0.00M;

    /// <summary>
    /// Default email target for delivery (bench can override per-run via AdminBench.EmailToOverride).
    /// </summary>
    public string? EmailTo { get; set; }

    public decimal ExitFeeAmount { get; set; } = 0.00M;

    public decimal ExitFeePercent { get; set; } = 0.00M;

    public List<decimal> ExplicitResetCurvePercents { get; set; } = new() { 5.30m, 5.55m, 5.80m, 5.75m, 5.60m };

    public int ExtensionCount { get; set; } = 2;

    public decimal ExtensionFeePercent { get; set; } = 0.50m;

    public int ExtensionTermMonths { get; set; } = 6;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.ExtensionFeeTypes ExtenstionFeeType { get; set; } = DominateDocsData.Enums.Payment.ExtensionFeeTypes.DollarAmount;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.FeeTypes FeesType { get; set; } = DominateDocsData.Enums.Payment.FeeTypes.Origination;

    public DateOnly? FirstPaymentDate { get; set; }

    public PaymentSchedule FixedPaymentSchedule { get; set; } = new();

    public string GuaranteedMonths { get; set; } = "6";

    public string GuarantorNames { get; set; }

    public List<Guarantor> Guarantors { get; set; } = new();

    public bool HasBorrowers => Borrowers?.Any() == true;

    public bool HasBrokers => Brokers?.Any() == true;

    public bool HasContractors => Contractors?.Any() == true;

    public bool HasGurantors => Guarantors?.Any() == true;

    public bool HasLenders => Lenders?.Any() == true;

    public bool HasProperties => Properties?.Any() == true;

    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public decimal? IndexSpotPercentAtClosing { get; set; } = 0.00M;


    public decimal InitialMargin { get; set; } = 0.00M;
    public decimal InterestRate { get; set; } = 0.00M;
    public bool IsACHDelivery { get; set; } = false;
    public bool IsACHPaymentsRequired { get; set; } = true;
    public bool IsBalloonPayment { get; set; } = false;
    public bool IsBorrowerResponsibleForServicingFees { get; set; } = false;
    // ── Simple Toggles ──
    public bool IsBorrowerServiceFees { get; set; }

    // ── Conditional Extension ──
    public bool IsConditionalRightToExtend { get; set; } = true;
   
    public bool IsConstructionAssignments { get; set; }
    public bool IsEscrowInvolved { get; set; } = false;
    // ── Exit Fee ──
    public bool IsExitFee { get; set; }

    public bool IsExitFeeIncluded { get; set; } = false;
    // ── Loan Intended for Sale ──
    public bool IsLoanIntendedForSale { get; set; }

    public bool IsLoanTypeConstruction { get; set; } = false;
    public bool IsLoanTypeDCSR { get; set; } = false;
    public bool IsMERSLanguage { get; set; } = false;
    public bool IsMERSLanuageToBeInserted { get; set; } = false;
    // ── Partial Release ──
    public bool IsPartialRelease { get; set; }

    // ── Prepayment Penalty ──
    public bool IsPrepaymentPenalty { get; set; } = true;

    public bool IsRemoveACHDFormFromDocSet { get; set; } = false;
    public bool IsSignAffidavitAkaRequired { get; set; } = false;
    public bool IsSigningAffidavit { get; set; } = false;
    public bool IsShowConstruction { get; set; } = false;
    public bool IsTaxInsuranceOtherImpounds { get; set; } = false;
    // ── Termination Fee ──
    public bool IsTerminationFee { get; set; }

    // ── Document Inclusions ──
    public bool IsW9Included { get; set; } = true;

    public bool IsW9TObeIncludedInDocSet { get; set; } = false;
    public decimal LateChargeAmount { get; set; } = 0.00M;
    public int LateChargeDays { get; set; } = 0;
    public decimal LateChargePercent { get; set; } = 0.00M;
    // =================================
    // Common rule keys (explicit, not reflection)
    // =================================
    public string? LenderCode { get; set; }

    public List<FeeToBePaid> LenderFees { get; set; } = [];
    public string LenderNames { get; set; }
    //public LoanFeatures LoanFeatures { get; set; }
    public List<Lender> Lenders { get; set; } = new();

    public string LoanNumber { get; set; }
    public string LoanPreparerCity { get; set; }
    public string LoanPreparerCounty { get; set; }
    public string LoanPreparerEmailAddress { get; set; }
    public string LoanPreparerName { get; set; }
    public string LoanPreparerPhoneNumber { get; set; }
    public string LoanPreparerState { get; set; }
    public string LoanPreparerStreetAddress { get; set; }
    public string LoanPreparerZipCode { get; set; }
    public string LoanPurchaserAssignees { get; set; }
    public string LoanPurchaserCity { get; set; }
    public string LoanPurchaserCounty { get; set; }
    public string LoanPurchaserEmailAddress { get; set; }
    public string LoanPurchaserName { get; set; }
    public string LoanPurchaserPhoneNumber { get; set; }
    public string LoanPurchaserState { get; set; }
    public string LoanPurchaserStreetAddress { get; set; }
    public string LoanPurchaserZipCode { get; set; }
    public string LoanSalesInformation { get; set; }
    public LoanServicer LoanServicer { get; set; } = new();
    public Guid LoanTypeId { get; set; } = Guid.Empty;
    public string LoanTypeName { get; set; }
    public LoanType LoanType { get; set; } = new();
    public DateOnly? MaturityDate { get; set; }
    public decimal MaxInterestAllowed { get; set; } = 0.00M;
    public DateOnly? OriginationDate { get; set; }
    public List<FeeToBePaid> OtherFees { get; set; } = [];
    // ============================
    // Output/Delivery overrides
    // ============================
    /// <summary>
    /// Default output type for this run (bench can override per-run via AdminBench.OutputTypeOverride).
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DocumentTypes.OutputTypes OutputType { get; set; } = DocumentTypes.OutputTypes.PDF;

    public PaymentSchedule PaymentSchedule { get; set; } = new();
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.PerDiemInterestOptions PerDiemOption { get; set; }

    public decimal PrepaymentFee { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.PrepaymentPremiums PrepaymentPremium { get; set; } = DominateDocsData.Enums.Payment.PrepaymentPremiums.PenaltyInMonths;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.PrepayPenaltyTypes PrepayType { get; set; } = Payment.PrepayPenaltyTypes.Stepdown;

    public decimal PrincipalAmount { get; set; } = 0.00M;
    // Store the projected curve you used (JSON)
    public string? ProjectedIndexCurveJson { get; set; }

    public List<PropertyRecord> Properties { get; set; } = new();
    public string PropertyAddresses { get; set; }
    public string? PropertyState { get; set; }
    // assume +0.25% each reset
    public List<RateChange> RateChangeList { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.RateIndexes RateIndex { get; set; } = DominateDocsData.Enums.Payment.RateIndexes.PRIME;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.RateTypes RateType { get; set; } = DominateDocsData.Enums.Payment.RateTypes.Fixed;

    public string Ratio { get; set; } = string.Empty;
    public string? ReferenceName { get; set; }
    //public VariableInterestProperties VariableInterestProperties { get; set; } = new();
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.Schedules RepaymentSchedule { get; set; } = DominateDocsData.Enums.Payment.Schedules.Monthly;

    public bool ReserveAccount { get; set; }
    public int ReserveInMonthsToCalculate { get; set; }
    public decimal ReserveSpecificAmount { get; set; }
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public DominateDocsData.Enums.Payment.ReserveTypes ReserveType { get; set; } = DominateDocsData.Enums.Payment.ReserveTypes.CalculateMonthlyAmount;

    //  public List<ConstructionContract> ConstructionContractors { get; set; } = new();
    public List<Servicer> Servicers { get; set; } = new();

    public decimal ServicingFeeAmount { get; set; }
    public DateOnly? SignedDate { get; set; }
    public decimal StartIndexPercent { get; set; } = 5.30m;
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Loan.Status Status { get; set; } = Loan.Status.Pending;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.StepdownStructures StepdownStructure { get; set; } = Payment.StepdownStructures.FiveYear;

    public string TerminationDollar { get; set; } = string.Empty;
    public decimal TerminationFee { get; set; } = 0.00M;
    public decimal TerminationPercent { get; set; } = 0.00M;
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.TerminationFeeTypes TerminationType { get; set; } = Payment.TerminationFeeTypes.DollarAmount;

    public int TermInMonths { get; set; } = 0;
    public Guid UserId { get; set; }

    public UserProfile UserProfile { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public UserEnums.UserTypes? UserType { get; set; }
   
    // today's SOFR
    //public string ClosingContactEmail { get; set; }
    //public List<FeeToBePaid> FeesToBePaid { get; set; } = new();
    
    // ============================
    // Admin Bench Overrides (optional)
    // ============================
    public sealed class AdminBenchOverrides
    {
        /// <summary>
        /// Optional: override email recipient for bench runs.
        /// </summary>
        public string? EmailToOverride { get; set; }

        /// <summary>Enable Admin Bench mode (overrides + trace).</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Optional: arbitrary key/value overrides injected into rule evaluation.
        /// </summary>
        public Dictionary<string, string> KeyOverrides { get; set; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Optional: override output type (PDF/DOCX) for bench runs.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public DocumentTypes.OutputTypes? OutputTypeOverride { get; set; }

        /// <summary>
        /// If true, evaluate ThenGenerate only and DO NOT enqueue MergeJobs.
        /// </summary>
        public bool SuppressMerge { get; set; } = false;
        /// <summary>
        /// Trace lines captured during evaluation (rules matched, docs added, missing tokens, etc.).
        /// </summary>
        public List<string> Trace { get; set; } = new();
    }
}
