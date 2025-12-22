using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CMSECommerce.Models.ViewModels
{
    public class ProfileUpdateViewModel
    {
        // Unique identifier of the IdentityUser
        [Required]
        public string UserId { get; set; }

        // --- BASIC INFO (IdentityUser) ---
        [Display(Name = "Username")]
        [ReadOnly(true)] // Usually not changeable during profile update
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [Display(Name = "Primary Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [Phone(ErrorMessage = "Please enter a valid phone number")]
        [Display(Name = "Mobile Number")]
        public string PhoneNumber { get; set; }

        // --- IMAGE APPROVAL STATUS ---
        public bool IsImageApproved { get; set; } = false;
        public bool IsImagePending { get; set; } = false;

        // --- USERPROFILE FIELDS ---
        [Display(Name = "Keep Profile Visible")]
        public bool IsProfileVisible { get; set; } = true;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "ITS Number is required")]
        [RegularExpression(@"^[0-9]{8,12}$", ErrorMessage = "ITS Number must be between 8 and 12 digits")]
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }

        [DataType(DataType.MultilineText)]
        [StringLength(500, ErrorMessage = "About section cannot exceed 500 characters")]
        public string About { get; set; }

        public string Profession { get; set; }

        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }

        public string CurrentRole { get; set; }

        // --- SOCIAL MEDIA & CONTACTS ---
        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "LinkedIn URL")]
        public string LinkedInUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Facebook URL")]
        public string FacebookUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Instagram URL")]
        public string InstagramUrl { get; set; }

        [Required(ErrorMessage = "WhatsApp number is required")]
        [Phone(ErrorMessage = "Invalid WhatsApp number")]
        [Display(Name = "WhatsApp Number")]
        public string WhatsappNumber { get; set; }

        // --- ADDRESSES ---
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; }

        [Display(Name = "Home Phone")]
        public string HomePhoneNumber { get; set; }

        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }

        [Display(Name = "Business Phone")]
        public string BusinessPhoneNumber { get; set; }

        // --- STORE FIELDS ---
        [Display(Name = "Store ID")]
        public int? StoreId { get; set; }

        [Required(ErrorMessage = "Store Name is required")]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; }

        [Required(ErrorMessage = "Street Address is required")]
        [Display(Name = "Store Street Address")]
        public string StoreStreetAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "Store City")]
        public string StoreCity { get; set; }

        [Required(ErrorMessage = "Post Code is required")]
        [Display(Name = "Store Post Code")]
        public string StorePostCode { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [Display(Name = "Store Country")]
        public string StoreCountry { get; set; }

        [Required(ErrorMessage = "GSTIN is required")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
            ErrorMessage = "Invalid GSTIN format (e.g., 27AAAAA0000A1Z5)")]
        [Display(Name = "GSTIN (Tax ID)")]
        public string GSTIN { get; set; }

        [Required(ErrorMessage = "Store Contact Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [Display(Name = "Store Contact Email")]
        public string StoreEmail { get; set; }

        [Required(ErrorMessage = "Store Contact Number is required")]
        [Phone(ErrorMessage = "Invalid phone format")]
        [Display(Name = "Store Contact Number")]
        public string StoreContact { get; set; }

        // --- FILE UPLOADS ---
        public string ExistingProfileImagePath { get; set; }
        public string PendingProfileImagePath { get; set; }

        [Display(Name = "Change Profile Image")]
        public IFormFile ProfileImageUpload { get; set; }

        public string ExistingGpayQRCodePath { get; set; }
        [Display(Name = "Upload GPay QR")]
        public IFormFile GpayQRCodeUpload { get; set; }

        public string ExistingPhonePeQRCodePath { get; set; }
        [Display(Name = "Upload PhonePe QR")]
        public IFormFile PhonePeQRCodeUpload { get; set; }
    }
}
