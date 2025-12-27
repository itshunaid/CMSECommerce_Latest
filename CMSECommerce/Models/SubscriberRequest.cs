using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace CMSECommerce.Models
{
    // Example: CMSECommerce.Models/SubscriberRequest.cs
    public class SubscriberRequest
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string RequestedRole { get; set; } = "Subscriber"; // Role requested
        public DateTime? ApprovalDate;
        public bool Approved { get; set; }
        public DateTime RequestDate { get; set; }
        public string? AdminNotes { get; set; } // Notes from the Admin

        // Navigation property
        [ForeignKey("UserId")]
        public virtual IdentityUser? User { get; set; }
    }
}
