using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Models
{
    public class UserProfile
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
       
        public string Profession { get; set; }
        public string ServicesProvided { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        [Required(ErrorMessage = "ITS Number is required")]
        public string ITSNumber { get; set; }
        
        [Required(ErrorMessage = "WhatsApp Number is required")]
        [Phone(ErrorMessage = "Invalid Phone Number")]
        public string WhatsAppNumber { get; set; }

        // Missing Fields Fix
        public string About { get; set; }
        public bool IsProfileVisible { get; set; } = true;
        public bool IsImageApproved { get; set; } = false;
        public bool IsImagePending { get; set; } = false;
        public string ProfileImagePath { get; set; }
        public string PendingProfileImagePath { get; set; }

        // Social & Payment QR Paths
        public string FacebookUrl { get; set; }
        public string LinkedInUrl { get; set; }
        public string InstagramUrl { get; set; }
        public string GpayQRCodePath { get; set; }
        public string PhonePeQRCodePath { get; set; }

        // Address Fields
        [Required(ErrorMessage = "Home Address is required")]
        public string HomeAddress { get; set; }
        public string HomePhoneNumber { get; set; }
        [Required(ErrorMessage = "Business Address is required")]
        public string BusinessAddress { get; set; }
        public string BusinessPhoneNumber { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }
        public int? StoreId { get; set; } // The '?' is essential

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        public DateTime? SubscriptionStartDate { get; set; }
        public DateTime? SubscriptionEndDate { get; set; }

        public int CurrentProductLimit { get; set; }
    }

    public class Store
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; }
        public string StoreName { get; set; }
        public string GSTIN { get; set; }
        public string Email { get; set; }
        public string Contact { get; set; }

        // Missing Fields Fix
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string Country { get; set; }

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}