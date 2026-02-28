namespace DominateDocsData.Models;

/// <summary>A fee line item — lender, broker, or other.</summary>
public class Fee
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Amount { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;  // combobox (typed or selected)
    public string OwedTo { get; set; } = string.Empty;       // only for "Other Fees"
    public string Notes { get; set; } = string.Empty;        // combobox (typed or selected)
}
