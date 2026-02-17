using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class BroadcastMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Message Subject")]
        [StringLength(255)]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Message Body")]
        [DataType(DataType.Html)]
        public string Body { get; set; }

        [Display(Name = "Attachment File Name")]
        [StringLength(255)]
        public string AttachmentFileName { get; set; }

        [Display(Name = "Attachment Path")]
        [StringLength(500)]
        public string AttachmentPath { get; set; }

        [Display(Name = "Send to All Sellers")]
        public bool SendToAllSellers { get; set; } = true;

        [Display(Name = "Selected Seller IDs")]
        public string SelectedSellerIds { get; set; } // Comma-separated list of seller user IDs

        [Required]
        [Display(Name = "Sent By")]
        public string SentByUserId { get; set; }

        [ForeignKey("SentByUserId")]
        public virtual User SentByUser { get; set; }

        [Required]
        [Display(Name = "Date Sent")]
        public DateTime DateSent { get; set; } = DateTime.UtcNow;

        [Display(Name = "Number of Recipients")]
        public int RecipientCount { get; set; }

        [Display(Name = "Status")]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Sent, Failed
    }
}
