using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LiquidDocsData.Models;

[BsonIgnoreExtraElements]
public class LoanAgreement
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    public string? ReferenceName { get; set; }

    public string LoanNumber { get; set; }

    public DocumentSet DocumentSet { get; set; }

    public decimal PrincipalAmount { get; set; } = 0;

    public decimal InterestRate { get; set; } = 0;
      
    public int TermInMonths { get; set; } = 0;

    public decimal InitialMargin { get; set; } = 0;

    public VariableInterestProperties VariableInterestProperties { get; set; } = new();

    public BalloonPayments BalloonPayments { get; set; } = new();

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Payment.AmortizationTypes AmorizationType { get; set; } = Payment.AmortizationTypes.InterestOnly;



    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.Schedules RepaymentSchedule { get; set; } = LiquidDocsData.Enums.Payment.Schedules.Monthly;


    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Loan.Types LoanType { get; set; } = LiquidDocsData.Enums.Loan.Types.ConstructionOrRehab;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.RateTypes RateType { get; set; } = LiquidDocsData.Enums.Payment.RateTypes.Fixed;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.PerDiemInterestOptions PerDiemOption { get; set; }
    
    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.PrepaymentPremiums PrepaymentPremium { get; set; } = LiquidDocsData.Enums.Payment.PrepaymentPremiums.PenaltyInMonths;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.ReserveTypes ReserveType { get; set; } = LiquidDocsData.Enums.Payment.ReserveTypes.CalculateMonthlyAmount;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.FeesPaidToOptions FeesPaidToOption { get; set; } = LiquidDocsData.Enums.Payment.FeesPaidToOptions.PaymentDefferedUntilAfterClosing;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.ExtensionFeeTypes ExtenstionFeeType { get; set; } = LiquidDocsData.Enums.Payment.ExtensionFeeTypes.DollarAmount;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public LiquidDocsData.Enums.Payment.RateIndexes RateIndex { get; set; } = LiquidDocsData.Enums.Payment.RateIndexes.PRIME;

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    [DataType(DataType.Text)]
    public Loan.Status Status { get; set; } = Loan.Status.Pending;

    public decimal DownPaymentPercentage { get; set; } = 0.00M;

    public decimal DownPaymentAmmount { get; set; }

    public bool IsPrepaymentPenalty { get; set; } = false;

    public decimal PrepaymentFee { get; set; }

    public int ReserveInMonthsToCalculate { get; set; }

    public decimal ReserveSpecificAmount { get; set; }

    public DateTime? OriginationDate { get; set; }

    public DateTime? MaturityDate { get; set; }

    public bool IsTaxInsuranceOtherImpounds { get; set; } = false;

    public bool IsBorrowerResponsibleForServicingFees { get; set; } = false;

    public decimal ServicingFeeAmount { get; set; }

    public bool IsExitFeeIncluded { get; set; } = false;

    public bool IsACHDelivery { get; set; } = false;

    public bool IsRemoveACHDFormFromDocSet { get; set; } = false;

    public bool IsEscrowInvolved { get; set; } = false;

    public LoanServicer LoanServicer { get; set; } = new();

    public bool IsBalloonPayment { get; set; } = false;
   
    public decimal ExitFeeAmount { get; set; }

    public bool IsConditionalRightToExtend { get; set; } = false;

    public int NumberOfExtensions { get; set; }

    public int NumberOfMonthsForEachExtension { get; set; }

    public string LoanPreparerName { get; set; }

    public string LoanPreparerStreetAddress { get; set; }

    public string LoanPreparerCity { get; set; }

    public string LoanPreparerState { get; set; }

    public string LoanPreparerZipCode { get; set; }

    public string LoanPreparerCounty { get; set; }

    public string LoanPreparerEmailAddress { get; set; }

    public bool IsW9TObeIncludedInDocSet { get; set; } = false;

    public bool IsLoanIntendedForSale { get; set; } = false;

    public string LoanSalesInformation { get; set; }

    public string LoanPreparerPhoneNumber { get; set; }

    public string LoanPurchaserName { get; set; }

    public string LoanPurchaserStreetAddress { get; set; }

    public string LoanPurchaserCity { get; set; }

    public string LoanPurchaserState { get; set; }

    public string LoanPurchaserZipCode { get; set; }

    public string LoanPurchaserCounty { get; set; }

    public string LoanPurchaserEmailAddress { get; set; }

    public string LoanPurchaserPhoneNumber { get; set; }

    public string LoanPurchaserAssignees { get; set; }

    public bool IsMERSLanuageToBeInserted { get; set; } = false;

    public bool IsSignAffidavitAkaRequired { get; set; } = false;

    public string ClosingContactName { get; set; }

    public string ClosingContactEmail { get; set; }

    public DateTime? SignedDate { get; set; }

    [NotMapped]
    [BsonIgnore]
    public string DocumentTitle { get; set; }

    public List<Lender> Lenders { get; set; } = new();

    public List<ConstructionContract> ConstructionContractors { get; set; } = new();

    public string LenderNames { get; set; }

    public List<Borrower> Borrowers { get; set; } = new();

    public string BorrowerNames { get; set; }

    public List<Broker> Brokers { get; set; } = new();

    public string BrokerNames { get; set; }

    public List<Guarantor> Guarantors { get; set; } = new();

    public string GuarantorNames { get; set; }

    public List<PropertyRecord> Properties { get; set; } = new();

    public string PropertyAddresses { get; set; }

    public List<FeeToBePaid> FeesToBePaid { get; set; } = new();

    public PaymentSchedule FixedPaymentSchedule { get; set; } = new();
}