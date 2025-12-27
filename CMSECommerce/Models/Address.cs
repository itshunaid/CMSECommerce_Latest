using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class Address
    {
        public int Id { get; set; }

        // Foreign Key to link to the IdentityUser
        public string UserId { get; set; }

        // Navigation Property for the User
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        [Required]
        public string StreetAddress { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string PostalCode { get; set; }

        [Required]
        public string Country { get; set; }
    }
}