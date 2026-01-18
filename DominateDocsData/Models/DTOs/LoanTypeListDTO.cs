using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DominateDocsData.Models.DTOs;

public class LoanTypeListDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IconKey { get; set; }


    public LoanTypeListDTO(Guid Id, string Name, string Description, string IconKey)
    {
        this.Id = Id;
        this.Name = Name;
        this.Description = Description;
        this.IconKey = IconKey;
    }
}
