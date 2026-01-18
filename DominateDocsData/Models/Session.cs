namespace DominateDocsData.Models;

public class Session
{
    public Guid? UserId { get; set; }

    public Guid? DocLibId { get; set; }

    public Guid? DocSetId { get; set; }

    public Guid? DocId { get; set; }
}