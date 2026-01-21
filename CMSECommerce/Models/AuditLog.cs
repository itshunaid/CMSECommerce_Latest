using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual Microsoft.AspNetCore.Identity.IdentityUser User { get; set; }

        [Required]
        [StringLength(100)]
        public string Action { get; set; } // e.g., "Created", "Updated", "Deleted", "Approved", "Rejected"

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } // e.g., "User", "Product", "Order", "SubscriptionRequest"

        public string EntityId { get; set; } // ID of the entity being audited

        public string OldValues { get; set; } // JSON serialized old values (for updates)

        public string NewValues { get; set; } // JSON serialized new values

        [Required]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [StringLength(45)]
        public string IpAddress { get; set; }

        public string UserAgent { get; set; }

        // Additional fields for better tracking
        public string SessionId { get; set; }

        [StringLength(100)]
        public string Controller { get; set; }

        [StringLength(100)]
        public string ActionMethod { get; set; }
    }
}
