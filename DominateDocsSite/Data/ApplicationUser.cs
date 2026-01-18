using DominateDocsData.Enums;
using DominateDocsData.Models;
using DominateDocsData.Models.Stripe;
using Microsoft.AspNetCore.Identity;

namespace DominateDocsSite.Data
{
    public class ApplicationUser : IdentityUser
    {
        public string? Name { get; set; }

        public string? DateOfBirth { get; set; }

        public string? StreetAddress { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? ZipCode { get; set; }

        public string? ProfilePictureUrl { get; set; }

        public UserEnums.Roles Role { get; set; } = UserEnums.Roles.User;

        //[NotMapped]
        //public Subscription Subscription { get; set; } = new();
        
        //public List<LoanDocumentSetGeneratedEvent> LoanDocSetGenerated { get; set; } = new();
    }
}