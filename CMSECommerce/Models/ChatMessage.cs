// Models/ChatMessage.cs
using System;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations.Schema;
namespace CMSECommerce.Models
{

    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; }
        // Assuming IdentityUser is used for Sender/Recipient
        public IdentityUser Sender { get; set; }
        public string RecipientId { get; set; } // Null for public chat
        public IdentityUser Recipient { get; set; }
        public string MessageContent { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
