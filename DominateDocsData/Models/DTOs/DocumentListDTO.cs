using DominateDocsData.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.DTOs;

public class DocumentListDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DocLibId { get; set; }

    public Guid DocStoreId { get; set; }

    public string Name { get; set; }

    public DateTime? UpdatedAt { get; set; }
     
    public DocumentListDTO(Guid Id, Guid DocLibId, string Name, DateTime? UpdatedAt, Guid DocStoreId)
    {
        this.Id = Id;
        this.DocLibId = DocLibId;
        this.DocStoreId = DocStoreId;
        this.Name = Name;
        this.UpdatedAt = UpdatedAt;
       

    }
}
