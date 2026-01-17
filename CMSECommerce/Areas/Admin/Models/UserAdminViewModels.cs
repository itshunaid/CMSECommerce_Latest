using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CMSECommerce.Areas.Admin.Models
{
    public class UserViewModel
    {
        // --- IdentityUser Fields ---
        public string Id { get; set; }
        [Display(Name = "Upload New Profile Image")]
        public IFormFile ProfileImageFile { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public IList<string> Roles { get; set; }
        public bool IsLockedOut { get; set; }

        // --- UserProfile Fields ---
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }
        [Display(Name = "Profile Image Path")]
        public string ProfileImagePath { get; set; }
        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; }
        [Display(Name = "Profile Visible")]
        public bool IsProfileVisible { get; set; }
        public string About { get; set; }
        public string Profession { get; set; }
        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }
        public string LinkedInUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        [Display(Name = "WhatsApp Number")]
        public string WhatsappNumber { get; set; }
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; }
        [Display(Name = "Home Phone")]
        public string HomePhoneNumber { get; set; }
        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }
        [Display(Name = "Business Phone")]
        public string BusinessPhoneNumber { get; set; }
        [Display(Name = "GPay QR Code Path")]
        public string GpayQRCodePath { get; set; }
        [Display(Name = "PhonePe QR Code Path")]
        public string PhonePeQRCodePath { get; set; }
        [Display(Name = "Deactivated")]
        public bool IsDeactivated { get; set; }

        // --- NEW: Store Fields for Index/Details ---
        public int? StoreId { get; set; }
        [Display(Name = "Store Name")]
        public string StoreName { get; set; }

        // --- NEW: Tier Fields ---
        public int? CurrentTierId { get; set; }
        [Display(Name = "Current Tier")]
        public string CurrentTierName { get; set; }
    }

    public class CreateUserModel
    {
        // --- 1. IdentityUser Fields ---
        [Required(ErrorMessage = "User Name is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email format.")]
        public string Email { get; set; }
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number (Identity)")]
        public string PhoneNumber { get; set; }
        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [Display(Name = "Assign Role")]
        public string Role { get; set; }

        // --- 2. UserProfile Fields ---
        [Required(ErrorMessage = "First Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for First Name is 3.")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for Last Name is 3.")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }
        [Display(Name = "Profile Image")]
        public IFormFile ProfileImageFile { get; set; }
        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; } = false;
        [Display(Name = "Keep Profile Visible")]
        public bool IsProfileVisible { get; set; } = true;
        [DataType(DataType.MultilineText)]
        public string About { get; set; }
        public string Profession { get; set; }
        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }
        public string LinkedInUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        [Display(Name = "WhatsApp Number")]
        public string WhatsappNumber { get; set; }
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; }
        [Display(Name = "Home Phone")]
        public string HomePhoneNumber { get; set; }
        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }
        [Display(Name = "Business Phone")]
        public string BusinessPhoneNumber { get; set; }
        [Display(Name = "GPay QR Code")]
        public IFormFile GpayQRCodeFile { get; set; }
        [Display(Name = "PhonePe QR Code")]
        public IFormFile PhonePeQRCodeFile { get; set; }

        // --- NEW: 3. Store Fields for Creation ---
        [Required(ErrorMessage = "Store Name is required.")]
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
        [Display(Name = "GSTIN")]
        public string GSTIN { get; set; }
        [EmailAddress]
        [Display(Name = "Store Email")]
        public string StoreEmail { get; set; }
        [Display(Name = "Store Contact Number")]
        public string StoreContact { get; set; }
    }

    public class EditUserModel
    {
        // --- 1. IdentityUser Fields ---
        [Required]
        public string Id { get; set; }
        [Required(ErrorMessage = "User Name is required.")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid Email format.")]
        public string Email { get; set; }
        [DataType(DataType.PhoneNumber)]
        [Display(Name = "Phone Number (Identity)")]
        public string PhoneNumber { get; set; }
        [Display(Name = "Assign Role")]
        public string Role { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "New Password (Leave blank to keep existing)")]
        public string NewPassword { get; set; }

        // --- 2. UserProfile Fields ---
        [Required(ErrorMessage = "First Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for First Name is 3.")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Last Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for Last Name is 3.")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }
        [Display(Name = "Current Profile Image")]
        public string ProfileImagePath { get; set; }
        [Display(Name = "Upload New Profile Image")]
        public IFormFile ProfileImageFile { get; set; }
        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; }
        [Display(Name = "Keep Profile Visible")]
        public bool IsProfileVisible { get; set; }
        [DataType(DataType.MultilineText)]
        public string About { get; set; }
        public string Profession { get; set; }
        [Display(Name = "Services Provided")]
        public string ServicesProvided { get; set; }
        public string LinkedInUrl { get; set; }
        public string FacebookUrl { get; set; }
        public string InstagramUrl { get; set; }
        [Display(Name = "WhatsApp Number")]
        public string WhatsAppNumber { get; set; }
        [Display(Name = "Home Address")]
        public string HomeAddress { get; set; }
        [Display(Name = "Home Phone")]
        public string HomePhoneNumber { get; set; }
        [Display(Name = "Business Address")]
        public string BusinessAddress { get; set; }
        [Display(Name = "Business Phone")]
        public string BusinessPhoneNumber { get; set; }
        [Display(Name = "Current GPay QR Code")]
        public string GpayQRCodePath { get; set; }
        [Display(Name = "Upload New GPay QR Code")]
        public IFormFile GpayQRCodeFile { get; set; }
        [Display(Name = "Current PhonePe QR Code")]
        public string PhonePeQRCodePath { get; set; }
        [Display(Name = "Upload New PhonePe QR Code")]
        public IFormFile PhonePeQRCodeFile { get; set; }

        // --- NEW: 3. Store Fields for Editing ---
        public int? StoreId { get; set; } // Required to know which store record to update
        [Display(Name = "Store Name")]
        public string StoreName { get; set; }
        [Display(Name = "Store Street")]
        public string StoreStreetAddress { get; set; }
        [Display(Name = "Store City")]
        public string StoreCity { get; set; }
        [Display(Name = "Store Post Code")]
        public string StorePostCode { get; set; }
        [Display(Name = "Store Country")]
        public string StoreCountry { get; set; }
        [Display(Name = "GSTIN")]
        public string GSTIN { get; set; }
        [Display(Name = "Store Email")]
        public string StoreEmail { get; set; }
        [Display(Name = "Store Contact")]
        public string StoreContact { get; set; }
    }
}