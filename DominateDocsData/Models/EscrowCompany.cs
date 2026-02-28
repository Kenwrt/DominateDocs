using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]

public class EscrowCompany : EntityBase
{
   
    // Entity Base Class Plus

    public string OfficerName { get; set; } = string.Empty;
   
    
    public string TitleOrderNumber { get; set; }

    
    public string TitleReportExceptionItemsToBeDeleted { get; set; }

    
    public string AdditionalTitleEndorsmentRequested { get; set; }

    public DateTime? TitleReportEffectiveDate { get; set; }

    
    public bool IsNotificationAddress { get; set; } = false;

    
    public bool IsReduceTitleCoverAmount { get; set; } = false;

   
}