using Microsoft.AspNetCore.Identity;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }

        // Foreign Key linking to the IdentityUser
        public string UserId { get; set; }
        
        [Required, MinLength(3, ErrorMessage = "Minimum length is 3")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }= "Sample First Name";

        [Required, MinLength(3, ErrorMessage = "Minimum length is 3")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }= "Sample Last Name";


        // Navigation property to the Identity User (optional, but useful)
        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // --- Custom Fields ---

        // Identification
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }

        [Display(Name = "Profile Image Path")]
        public string ProfileImagePath { get; set; } // Path to the uploaded image

        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; } = false; // Default to false (unapproved)

        // Bio and Profession
        [DataType(DataType.MultilineText)]
        public string About { get; set; }
        public string Profession { get; set; }

        // List of Services (Consider storing as a comma-separated string or creating a separate table for a true one-to-many relationship if complex)
        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }

        // Social Media & Contact
        public string LinkedInUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        [Display(Name = "WhatsApp Number")]
        public string WhatsappNumber { get; set; }

        // Addresses
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; }
        [Display(Name = "Home Phone")]
        public string HomePhoneNumber { get; set; }

        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }
        [Display(Name = "Business Phone")]
        public string BusinessPhoneNumber { get; set; }

        // Payment QR Codes
        [Display(Name = "GPay QR Code Path")]
        public string GpayQRCodePath { get; set; } // Path to the uploaded QR image
        [Display(Name = "PhonePe QR Code Path")]
        public string PhonePeQRCodePath { get; set; } // Path to the uploaded QR image
    }
}