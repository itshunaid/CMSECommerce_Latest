namespace CMSECommerce.Models
{
    using Microsoft.AspNetCore.Identity;
    // CMSECommerce/Models/Entities/UserAgreement.cs
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace CMSECommerce.Models.Entities
    {
        public class UserAgreement
        {
            [Key]
            public int Id { get; set; }

            [Required]
            public string UserId { get; set; } // Foreign Key to AspNetUsers

            [Required]
            public string AgreementType { get; set; } // e.g., "TermsAndConditions"

            [Required]
            public string Version { get; set; } // e.g., "v1.0-2026-01"

            [Required]
            public DateTime AcceptedAt { get; set; }

            public string IpAddress { get; set; } // Good for legal proof

            [ForeignKey("UserId")]
            public virtual IdentityUser User { get; set; }
        }
    }
}
