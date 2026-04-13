using Microsoft.AspNetCore.Identity;

namespace CMSECommerce.Models
{
    public class BroadcastRecipient
    {
        public int Id { get; set; }

        public int BroadcastMessageId { get; set; }
        public BroadcastMessage BroadcastMessage { get; set; }

        public string UserId { get; set; }
        public IdentityUser User { get; set; }

        public string Email { get; set; }

        public string Status { get; set; } // Pending, Sent, Failed

        public string ErrorMessage { get; set; }

        public DateTime? SentAt { get; set; }
    }
}
