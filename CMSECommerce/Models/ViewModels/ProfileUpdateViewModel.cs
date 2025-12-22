using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http; // Added for IFormFile

namespace CMSECommerce.Models.ViewModels
{
    public class ProfileUpdateViewModel
    {
        // Unique identifier of the IdentityUser
        public string UserId { get; set; }

        // Basic Info (pulled from IdentityUser)
        [Display(Name = "Username")]
        public string UserName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        [Display(Name = "Mobile Number")]
        public string PhoneNumber { get; set; }

        // --- IMAGE APPROVAL STATUS ---
        public bool IsImageApproved { get; set; } = false;
        public bool IsImagePending { get; set; } = false;

        // --- UserProfile Fields ---
        [Display(Name = "Keep Profile Visible")]
        [Description("If checked, this profile will be publicly visible.")]
        public bool IsProfileVisible { get; set; } = true;

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }

        [DataType(DataType.MultilineText)]
        public string About { get; set; }

        public string Profession { get; set; }

        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }
        public string CurrentRole { get; set; }

        // Social Media
        [Display(Name = "LinkedIn URL")]
        public string LinkedInUrl { get; set; }

        [Display(Name = "Facebook URL")]
        public string FacebookUrl { get; set; }

        [Display(Name = "Instagram URL")]
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

        // --- NEW: ALL STORE FIELDS ---
        [Display(Name = "Store ID")]
        public int? StoreId { get; set; } // Nullable to handle profiles without stores

        [Display(Name = "Store Name")]
        public string StoreName { get; set; }

        [Display(Name = "Store Street Address")]
        public string StoreStreetAddress { get; set; }

        [Display(Name = "Store City")]
        public string StoreCity { get; set; }

        [Display(Name = "Store Post Code")]
        public string StorePostCode { get; set; }

        [Display(Name = "Store Country")]
        public string StoreCountry { get; set; }

        [Display(Name = "GSTIN (Tax ID)")]
        public string GSTIN { get; set; }

        [Display(Name = "Store Contact Email")]
        public string StoreEmail { get; set; }

        [Display(Name = "Store Contact Number")]
        public string StoreContact { get; set; }

        // --- File Uploads & Existing Paths ---

        // Profile Image
        public string ExistingProfileImagePath { get; set; }
        public string PendingProfileImagePath { get; set; }

        [Display(Name = "Change Profile Image")]
        public IFormFile ProfileImageUpload { get; set; }

        // GPay QR
        public string ExistingGpayQRCodePath { get; set; }
        [Display(Name = "Upload GPay QR")]
        public IFormFile GpayQRCodeUpload { get; set; }

        // PhonePe QR
        public string ExistingPhonePeQRCodePath { get; set; }
        [Display(Name = "Upload PhonePe QR")]
        public IFormFile PhonePeQRCodeUpload { get; set; }
    }
}