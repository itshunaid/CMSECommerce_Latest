using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{
    // In Models/UserStatusTracker.cs

    public class UserStatusTracker
    {
        // Make UserId the Primary Key
        [Key]
        public string UserId { get; set; }

        // The actual status flag
        public bool IsOnline { get; set; }

        // When the status was last confirmed (helpful for cleanup/timeout logic)
        public DateTime LastActivity { get; set; }
    }
}
