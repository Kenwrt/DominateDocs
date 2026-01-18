// File: LiquidDocsData/Models/LoanServicer.cs
using LiquidDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel.DataAnnotations;

namespace LiquidDocsData.Models
{
    [BsonIgnoreExtraElements]
    public class LoanServicer
    {
        [Key]
        [BsonId]
        [Required]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid UserId { get; set; }

        // Identity
        public string EntityName { get; set; }              // Legal name

        public string? DoingBusinessAs { get; set; }        // DBA, optional
        public string? NmlsId { get; set; }                 // If applicable
        public string? LicenseNumbers { get; set; }         // Freeform, state-specific
        public string? RegulatoryAuthority { get; set; }    // e.g. "State Regulator", "NMLS"

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public Entity.Types EntityType { get; set; } = Entity.Types.Entity;

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public Entity.Structures EntityStructure { get; set; } = Entity.Structures.LLC;

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public UsStates.UsState StateOfIncorporation { get; set; }

        // Primary contact
        public string ContactName { get; set; }

        public string ContactEmail { get; set; }
        public string ContactPhoneNumber { get; set; }

        // Address
        public string FullAddress { get; set; }

        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public double? Lat { get; set; }
        public double? Lng { get; set; }

        // Remittance & banking
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public LoanService.RemittanceSchedules RemittanceSchedule { get; set; } = LoanService.RemittanceSchedules.Monthly;

        // Use "HH:mm" 24-hr format (store as string for simplicity across serializers)
        public string? RemittanceCutoffTime { get; set; }   // e.g. "17:00"

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public LoanService.PaymentMethods RemittanceMethod { get; set; } = LoanService.PaymentMethods.ACH;

        public string? AchRoutingNumber { get; set; }       // mask/redact in UI
        public string? AchAccountNumber { get; set; }       // mask/redact in UI
        public string? WireBankName { get; set; }
        public string? WireRoutingNumber { get; set; }
        public string? WireAccountNumber { get; set; }

        // Servicing fees
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public LoanService.ServicingFeeBases ServicingFeeBasis { get; set; } = LoanService.ServicingFeeBases.PercentOfUPB;

        public decimal ServicingFeePercent { get; set; }        // e.g. 0.50m = 0.50%
        public decimal ServicingFeeFixedAmount { get; set; }    // per-loan, per-month if FixedPerLoan
        public string? LateFeePolicy { get; set; }              // human-readable policy text

        // Escrow handling
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public List<LoanService.EscrowTypes> EscrowTypesHandled { get; set; } = new();

        public string? EscrowShortagePolicy { get; set; }
        public bool IsEscrowAnalysisProvided { get; set; } = true;

        // Reporting
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public LoanService.ReportingFrequencies ReportingFrequency { get; set; } = LoanService.ReportingFrequencies.Monthly;

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        [DataType(DataType.Text)]
        public LoanService.DataDeliveryFormats ReportingDeliveryFormat { get; set; } = LoanService.DataDeliveryFormats.CSV;

        public string ReportingDeliveryEmail { get; set; }
        public string? DataPortalUrl { get; set; }

        // Capabilities & footprint
        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public List<LoanService.Capabilities> Capabilities { get; set; } = new();

        [JsonConverter(typeof(StringEnumConverter))]
        [BsonRepresentation(BsonType.String)]
        public List<UsStates.UsState> PortfolioStateLicenses { get; set; } = new();

        public bool IsSubservicer { get; set; } = true;     // acts as sub-servicer vs master
        public int ApproxActiveLoanCount { get; set; }      // capacity signal, optional

        // Compliance & ops contacts
        public string? ComplianceContactName { get; set; }

        public string? ComplianceContactEmail { get; set; }
        public string? ComplianceContactPhone { get; set; }

        public string? PayoffDeskEmail { get; set; }
        public string? BoardingDeskEmail { get; set; }

        // Optional organizational lists (kept consistent with your other models)
        public List<SigningAuthority> SigningAuthorities { get; set; } = new();

        public string SigningAuthoritiesFormatted { get; set; }

        public List<AkaName> AliasNames { get; set; } = new();
        public string AliasNamesFormatted { get; set; }

        public List<EntityOwner> EntityOwners { get; set; } = new();
        public string EntityOwnersFormatted { get; set; }

        public string SignatureLinesFormatted { get; set; }

        // Flags & housekeeping
        public bool IsActive { get; set; } = true;
    }
}