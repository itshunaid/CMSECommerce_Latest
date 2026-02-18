// Models/ChatMessage.cs
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{

    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        public string SenderId { get; set; }
        public string SenderName { get; set; }

        // For private messages, RecipientId is set. Null for broadcasts/groups
        public string RecipientId { get; set; }

        // For group messages, GroupName is set
        public string GroupName { get; set; }

        [Required]
        public string MessageContent { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Whether the recipient has read this message (only meaningful for private messages)
        public bool IsRead { get; set; } = false;
    }
}
