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

public class BrokerListDTO
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public int LenderCode { get; set; }

    public string EntityName { get; set; }

    public string ContactEmail { get; set; }

    public string ContactPhoneNumber { get; set; }

    public string FullAddress { get; set; }


    public BrokerListDTO(Guid Id, int LenderCode, string EntityName, string ContactEmail, string ContactPhoneNumber, string FullAddress)
    {
        this.Id = Id;
        this.LenderCode = LenderCode;
        this.EntityName = EntityName;
        this.ContactEmail = ContactEmail;
        this.ContactPhoneNumber = ContactPhoneNumber;
        this.FullAddress = FullAddress;

    }
}
