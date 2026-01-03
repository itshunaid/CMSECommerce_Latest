
using Microsoft.AspNetCore.Identity;
// CMSECommerce/Models/Entities/UserAgreement.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class UserAgreement
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string AgreementType { get; set; }

        [Required]
        public string Version { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(max)")] //Ensures storage for long HTML content
        public string FullContent { get; set; } // <--- NEW: Stores the 7 points text

        [Required]
        public DateTime AcceptedAt { get; set; }

        public string IpAddress { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
    }
}

