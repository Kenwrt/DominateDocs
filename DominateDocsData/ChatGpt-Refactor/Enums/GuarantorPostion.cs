namespace LiquidDocsData.Enums;

public class GuarantorPosition
{
    public enum Types
    {
        [System.ComponentModel.Description("Full Recourse")]
        FullRecourse,

        [System.ComponentModel.Description("Limited Recourse")]
        LimitedRecourse,

        [System.ComponentModel.Description("Springing Recourse")]
        SpringingRecourse
    }
}