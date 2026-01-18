namespace LiquidDocsData.Enums;

public class DocumentMergeState
{
    public enum Status
    {
        [System.ComponentModel.Description("Pending")]
        Pending,

        [System.ComponentModel.Description("Queued")]
        Queued,

        [System.ComponentModel.Description("Error")]
        Error,

        [System.ComponentModel.Description("Complete")]
        Complete,



        [System.ComponentModel.Description("Submittied")]
        Submittied
    }

    
}