using DominateDocsData.Enums;
using DominateDocsData.Models.Stripe;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("UserId")]
    public Guid UserId { get; set; }

    [BsonElement("UserName")]
    public string? UserName { get; set; }

    public string Name { get; set; }

    public string Email { get; set; }

    public string Password { get; set; }

    public string ConfirmedPassword { get; set; }
       
    public string? ProfilePictureUrl { get; set; }

    [JsonConverter(typeof(StringEnumConverter))]
    [BsonRepresentation(BsonType.String)]
    public UserEnums.Roles? UserRole { get; set; }

    public LoanDefaults LoanDefaults { get; set; } = new();

    public List<Guid> LoanAgreementGuids { get; set; } = new();

    public List<LoanDocumentSetGeneratedEvent> LoanDocsGenerated { get; set; } = new();

    
    public List<ChargingAuditTrail>? CharingAuditTrails { get; set; } = new();

    
    public BillingAccount Billing { get; set; } = new();

    public List<BillingChargeRecord> BillingCharges { get; set; } = new();

    public List<BillingEventRecord> BillingEvents { get; set; } = new();

    
    [BsonIgnoreExtraElements]
    public sealed class BillingAccount
    {
        // ---- Access control ----
        public bool IsAccountDisabled { get; set; } = false;
        public string? DisabledReason { get; set; }
        public DateTime? DisabledAtUtc { get; set; }
        public Guid? DisabledByUserId { get; set; }

        // ---- Admin-only bypass flags ----
        public bool BypassSubscriptionCharges { get; set; } = false;
        public bool BypassDocumentProcessingCharges { get; set; } = false;
        public Guid? LastBypassChangedByUserId { get; set; }
        public DateTime? LastBypassChangedAtUtc { get; set; }

        
        public DateTime? SubscriptionValidUntilUtc { get; set; }

        
        public string? StripeCustomerId { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public DateTime? StripeLastSyncedAtUtc { get; set; }

        
        public List<LoanBillingState> LoanStates { get; set; } = new();
    }

    [BsonIgnoreExtraElements]
    public sealed class LoanBillingState
    {
        public Guid LoanId { get; set; }

        
        public bool ProcessingFeeSatisfied { get; set; } = false;

        public DateTime? FirstSatisfiedAtUtc { get; set; }

        
        public int GenerationCount { get; set; } = 0;

        public DateTime? LastGeneratedAtUtc { get; set; }

        
        public string? ExternalChargeRef { get; set; }
    }

  
    public enum BillingChargeTypes
    {
        SubscriptionAnnual,
        DocumentProcessingPerLoan
    }

  
    public enum BillingChargeStatus
    {
        Pending,
        Succeeded,
        Failed,
        Voided,
        Bypassed
    }

    [BsonIgnoreExtraElements]
    public sealed class BillingChargeRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public BillingChargeTypes ChargeType { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public BillingChargeStatus Status { get; set; } = BillingChargeStatus.Pending;

        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";

        public Guid UserId { get; set; }
        public Guid? LoanId { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAtUtc { get; set; }

        /// <summary>Stripe Invoice/PaymentIntent/etc ID if applicable.</summary>
        public string? ExternalRef { get; set; }

        public string? Notes { get; set; }
    }

    
    public enum BillingEventTypes
    {
        AccountDisabled,
        AccountEnabled,

        BypassFlagsChanged,

        SubscriptionStatusChecked,
        SubscriptionExpiredBlocked,
        SubscriptionBypassedAllowed,

        DocumentGenerationAttempted,
        DocumentGenerationBlockedAccountDisabled,
        DocumentGenerationBlockedSubscriptionExpired,

        DocumentProcessingChargedFirstTime,
        DocumentProcessingBypassedFirstTime,
        DocumentGenerationFreeRepeat,

        StripeSyncUpdated
    }

    [BsonIgnoreExtraElements]
    public sealed class BillingEventRecord
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public BillingEventTypes EventType { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        public Guid UserId { get; set; }
        public Guid? LoanId { get; set; }

        /// <summary>
        /// Admin actor (if this was initiated by an admin action).
        /// </summary>
        public Guid? ActorUserId { get; set; }

        public string? Message { get; set; }

        /// <summary>
        /// Extra data bag for later analytics. Keep it small; Mongo is not your emotional support bucket.
        /// </summary>
        public Dictionary<string, string>? Data { get; set; }
    }
}
