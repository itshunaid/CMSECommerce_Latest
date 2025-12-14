using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        public bool IsImageApproved { get; set; } = false;

        // --- UserProfile Fields ---

        // NEW FIELD FOR PROFILE VISIBILITY
        [Display(Name = "Keep Profile Visible")]
        [Description("If checked, this profile will be publicly visible.")]
        public bool IsProfileVisible { get; set; } = true; // Default to true (visible)

        // ID & Bio
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }
        [DataType(DataType.MultilineText)]
        public string About { get; set; }
        public string Profession { get; set; }
        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }

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

        // --- File Uploads (for the form) and Existing Paths (for display) ---

        // Profile Image
        public string ExistingProfileImagePath { get; set; }
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
        public string FirstName { get;  set; }
        public string LastName { get;  set; }
        public string CurrentRole { get;  set; }
    }
}