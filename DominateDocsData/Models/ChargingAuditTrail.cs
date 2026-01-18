namespace DominateDocsData.Models;

public class ChargingAuditTrail
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? CardId { get; set; }

    public DateTime Timestamp { get; set; }
    public string ActionType { get; set; } // e.g. "CHARGE", "REFUND", "FAILED"
    public string Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    public UserProfile UserProfile { get; set; }
    public CreditCard CreditCard { get; set; }
}