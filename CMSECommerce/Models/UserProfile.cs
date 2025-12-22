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
        [StringLength(450)] // Matches default IdentityUser key length
        public string UserId { get; set; }

        [Required(ErrorMessage = "First Name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "First Name must be between 3 and 50 characters")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = "Sample First Name";

        [Required(ErrorMessage = "Last Name is required")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Last Name must be between 3 and 50 characters")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = "Sample Last Name";

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; }

        // --- Identification ---

        [Display(Name = "ITS Number")]
        [RegularExpression(@"^\d+$", ErrorMessage = "ITS Number must contain only digits")]
        [StringLength(20)]
        public string ITSNumber { get; set; }

        // --- Image Approval Workflow ---

        [Display(Name = "Active Profile Image")]
        [StringLength(500)]
        public string ProfileImagePath { get; set; }

        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; } = false;

        [Display(Name = "Pending Image Path")]
        [StringLength(500)]
        public string PendingProfileImagePath { get; set; }

        [Display(Name = "Image Pending Review")]
        public bool IsImagePending { get; set; } = false;

        // --- Visibility & Bio ---

        [Display(Name = "Publicly Visible")]
        public bool IsProfileVisible { get; set; } = true;

        [DataType(DataType.MultilineText)]
        [StringLength(2000, ErrorMessage = "Bio cannot exceed 2000 characters")]
        [Display(Name = "About Me")]
        public string About { get; set; }

        [StringLength(100)]
        public string Profession { get; set; }

        [Display(Name = "Services Provided")]
        [StringLength(500)]
        public string ServicesProvided { get; set; }

        // --- Social Media ---

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500)]
        public string LinkedInUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500)]
        public string FacebookUrl { get; set; }

        [Url(ErrorMessage = "Please enter a valid URL")]
        [StringLength(500)]
        public string InstagramUrl { get; set; }

        [Display(Name = "WhatsApp Number")]
        [Phone(ErrorMessage = "Invalid WhatsApp Number")]
        [StringLength(15, MinimumLength = 10, ErrorMessage = "Phone number must be between 10 and 15 digits")]
        public string WhatsAppNumber { get; set; }

        // --- Contact & Payment ---

        [StringLength(255)]
        public string HomeAddress { get; set; }

        [Phone]
        [StringLength(20)]
        public string HomePhoneNumber { get; set; }

        [StringLength(255)]
        public string BusinessAddress { get; set; }

        [Phone]
        [StringLength(20)]
        public string BusinessPhoneNumber { get; set; }

        [StringLength(500)]
        public string GpayQRCodePath { get; set; }

        [StringLength(500)]
        public string PhonePeQRCodePath { get; set; }

        // --- Relationship with Store ---

        [Display(Name = "Assigned Store")]
        public int? StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }
    }

    public class Store
    {
        public Store()
        {
            UserProfiles = new HashSet<UserProfile>();
        }

        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Store Name is required")]
        [StringLength(100)]
        [Display(Name = "Store Name")]
        public string StoreName { get; set; }

        [Required]
        [StringLength(255)]
        public string StreetAddress { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; }

        [Required]
        [StringLength(20)]
        [DataType(DataType.PostalCode)]
        public string PostCode { get; set; }

        [Required]
        [StringLength(100)]
        public string Country { get; set; }

        [Display(Name = "GSTIN Number")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$", ErrorMessage = "Invalid GSTIN format")]
        [StringLength(15)]
        public string GSTIN { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

       

        [Required]
        [Phone]
        [StringLength(20)]
        public string Contact { get; set; }

        // Navigation property
        public virtual ICollection<UserProfile> UserProfiles { get; set; }
    }
}