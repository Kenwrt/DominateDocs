namespace LiquidDocsData.Models;

public class Session
{
    public string? SessionId { get; set; }

    public Guid? DocLibId { get; set; }

    public Guid? DocSetId { get; set; }

    public Guid? DocId { get; set; }
}