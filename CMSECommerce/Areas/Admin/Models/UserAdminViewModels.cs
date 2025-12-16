using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Areas.Admin.Models
{

    public class UserViewModel
    {
        // --- IdentityUser Fields ---
        public string Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public IList<string> Roles { get; set; }
        public bool IsLockedOut { get; set; }


        // ** --- UserProfile Fields Added Below --- **

        // Basic Info
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [DisplayName("Last Name")]
        public string LastName { get; set; }

        // Identification and Visibility
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }

        [Display(Name = "Profile Image Path")]
        public string ProfileImagePath { get; set; }

        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; }

        [Display(Name = "Profile Visible")]
        public bool IsProfileVisible { get; set; }

        // Bio and Profession
        public string About { get; set; }

        public string Profession { get; set; }

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

        // Payment QR Codes (Paths)
        [Display(Name = "GPay QR Code Path")]
        public string GpayQRCodePath { get; set; }

        [Display(Name = "PhonePe QR Code Path")]
        public string PhonePeQRCodePath { get; set; }
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


        // ** --- 2. UserProfile Fields --- **

        // Basic Info (Required)
        [Required(ErrorMessage = "First Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for First Name is 3.")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for Last Name is 3.")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }

        // Identification and Visibility
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }

        [Display(Name = "Profile Image Path")]
        // Note: For actual creation, you'd usually use an IFormFile here,
        // but this field is included to mirror the model structure.
        public string ProfileImagePath { get; set; }

        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; } = false;

        [Display(Name = "Keep Profile Visible")]
        [Description("If checked, this profile will be publicly visible.")]
        public bool IsProfileVisible { get; set; } = true;

        // Bio and Profession
        [DataType(DataType.MultilineText)]
        public string About { get; set; }

        public string Profession { get; set; }

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

        // Payment QR Codes (Paths)
        [Display(Name = "GPay QR Code Path")]
        public string GpayQRCodePath { get; set; }

        [Display(Name = "PhonePe QR Code Path")]
        public string PhonePeQRCodePath { get; set; }
    }



    public class EditUserModel
    {
        // --- 1. IdentityUser Fields ---

        // The user ID is required to identify the user being edited
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

        // Field for Role assignment
        [Display(Name = "Assign Role")]
        public string Role { get; set; }

        // Optional field for changing the password
        [DataType(DataType.Password)]
        [Display(Name = "New Password (Leave blank to keep existing)")]
        public string NewPassword { get; set; }


        // ** --- 2. UserProfile Fields --- **

        // Basic Info (Required)
        [Required(ErrorMessage = "First Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for First Name is 3.")]
        [DisplayName("First Name")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [MinLength(3, ErrorMessage = "Minimum length for Last Name is 3.")]
        [DisplayName("Last Name")]
        public string LastName { get; set; }

        // Identification and Visibility
        [Display(Name = "ITS Number")]
        public string ITSNumber { get; set; }

        [Display(Name = "Profile Image Path")]
        public string ProfileImagePath { get; set; }

        [Display(Name = "Image Approved")]
        public bool IsImageApproved { get; set; }

        [Display(Name = "Keep Profile Visible")]
        [Description("If checked, this profile will be publicly visible.")]
        public bool IsProfileVisible { get; set; }

        // Bio and Profession
        [DataType(DataType.MultilineText)]
        public string About { get; set; }

        public string Profession { get; set; }

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

        // Payment QR Codes (Paths)
        [Display(Name = "GPay QR Code Path")]
        public string GpayQRCodePath { get; set; }

        [Display(Name = "PhonePe QR Code Path")]
        public string PhonePeQRCodePath { get; set; }
    }
    
}
