using DominateDocsData.Models.Storage;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace DominateDocsData.Models;

[BsonIgnoreExtraElements]
public class DocumentLibrary
{
    [Key]
    [BsonId]
    [Required]
    public Guid Id { get; set; } = Guid.NewGuid();
  
    public string Name { get; set; }

    public String Description { get; set; }

    public bool IsUsingDefaultTemplate { get; set; } = true;

    public string MasterTemplate { get; set; } = "Master Default Template";

    public BlobRef? MasterTemplateRef { get; set; }

    public byte[] MasterTemplateBytes { get; set; } // will move this to Azure at a later date
      
    public List<Document> Documents { get; set; } = new(); // will delete after migration

    public DateTime? UpdatedAt { get; set; }

    public bool IsActive { get; set; } = true;

    [BsonIgnore]
    public bool HasMasterTemplate => MasterTemplateRef is not null && !MasterTemplateRef.IsEmpty;
}


    

  

  

