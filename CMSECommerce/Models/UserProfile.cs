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

        [Required]
        public string UserId { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [MinLength(3, ErrorMessage = "Minimum length is 3")]
        [MaxLength(50, ErrorMessage = "Maximum length is 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "Sample First Name";

        [Required(ErrorMessage = "Last Name is required")]
        [MinLength(3, ErrorMessage = "Minimum length is 3")]
        [MaxLength(50, ErrorMessage = "Maximum length is 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "Sample Last Name";

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // --- Custom Fields ---

        [Display(Name = "ITS Number")]
        [RegularExpression(@"^\d+$", ErrorMessage = "ITS Number must contain only digits")]
        public string ITSNumber { get; set; }

        [Display(Name = "Profile Image")]
        public string ProfileImagePath { get; set; }

        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; } = false;

        [Display(Name = "Pending Image")]
        public string PendingProfileImagePath { get; set; }

        [Display(Name = "Pending Review")]
        public bool IsImagePending { get; set; } = false;

        [Display(Name = "Publicly Visible")]
        [Description("If checked, this profile will be publicly visible.")]
        public bool IsProfileVisible { get; set; } = true;

        [DataType(DataType.MultilineText)]
        [MaxLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
        [Display(Name = "About Me")]
        public string About { get; set; }

        [MaxLength(100, ErrorMessage = "Profession cannot exceed 100 characters")]
        [Display(Name = "Profession")]
        public string Profession { get; set; }

        [Display(Name = "Services Provided")]
        [MaxLength(500, ErrorMessage = "Services list cannot exceed 500 characters")]
        public string ServicesProvided { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "LinkedIn Profile")]
        public string LinkedInUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Facebook Profile")]
        public string FacebookUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [Display(Name = "Instagram Profile")]
        public string InstagramUrl { get; set; }

        [Display(Name = "WhatsApp Number")]
        [Phone(ErrorMessage = "Invalid WhatsApp Number")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 digits")]
        public string WhatsAppNumber { get; set; }

        [Display(Name = "Home Address")]
        [MaxLength(255)]
        public string HomeAddress { get; set; }

        [Display(Name = "Home Phone")]
        [Phone(ErrorMessage = "Invalid Home Phone Number")]
        public string HomePhoneNumber { get; set; }

        [Display(Name = "Business Address")]
        [MaxLength(255)]
        public string BusinessAddress { get; set; }

        [Display(Name = "Business Phone")]
        [Phone(ErrorMessage = "Invalid Business Phone Number")]
        public string BusinessPhoneNumber { get; set; }

        [Display(Name = "GPay QR Code")]
        public string GpayQRCodePath { get; set; }

        [Display(Name = "PhonePe QR Code")]
        public string PhonePeQRCodePath { get; set; }

        // Made nullable to resolve migration FK conflict
        [Display(Name = "Assigned Store")]
        public int? StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }
    }

    public class Store
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Store Name is required")]
        [StringLength(100, ErrorMessage = "Store Name cannot exceed 100 characters")]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; }

        [Required(ErrorMessage = "Street Address is required")]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        [Display(Name = "City")]
        public string City { get; set; }

        [Required(ErrorMessage = "Post Code is required")]
        [Display(Name = "Post Code")]
        [DataType(DataType.PostalCode)]
        public string PostCode { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [Display(Name = "Country")]
        public string Country { get; set; }

        [Display(Name = "GSTIN Number")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GSTIN format")]
        public string GSTIN { get; set; }

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        [Display(Name = "Store Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Contact number is required")]
        [Phone(ErrorMessage = "Invalid Contact Number")]
        [Display(Name = "Store Contact")]
        public string Contact { get; set; }

        public virtual ICollection<UserProfile> UserProfiles { get; set; }
    }
}