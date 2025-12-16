using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Areas.Admin.Models
{

    public class UserViewModel
    {
        // --- IdentityUser Fields ---
        public string Id { get; set; }
        // This property is used only for file uploads from the form.
        // It should be decorated with [NotMapped] if this ViewModel maps directly to an Entity Framework entity
        // that does not have a column for the file object itself.
        [Display(Name = "Upload New Profile Image")]
        public IFormFile ProfileImageFile { get; set; }
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
    
   // ... (UserViewModel and EditUserModel remain unchanged)

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

            // FILE UPLOADS: Replaced string paths with IFormFile for uploading
            [Display(Name = "Profile Image")] // Updated Display Name for upload
            public IFormFile ProfileImageFile { get; set; } // Renamed for clarity

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

            // Payment QR Codes (IFormFile for uploading)
            [Display(Name = "GPay QR Code")]
            public IFormFile GpayQRCodeFile { get; set; } // Renamed for clarity

            [Display(Name = "PhonePe QR Code")]
            public IFormFile PhonePeQRCodeFile { get; set; } // Renamed for clarity

        // The path strings will be handled by the controller after file upload
        // and stored in the database.
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

        // --- Profile Image (Existing Path & New File Upload) ---
        [Display(Name = "Current Profile Image")]
        public string ProfileImagePath { get; set; } // Keeps existing path for display

        [Display(Name = "Upload New Profile Image")]
        [DataType(DataType.Upload)]
        public IFormFile ProfileImageFile { get; set; } // Handles new file upload
                                                        // --------------------------------------------------------

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

        // --- Payment QR Codes (Existing Paths & New File Uploads) ---
        [Display(Name = "Current GPay QR Code")]
        public string GpayQRCodePath { get; set; } // Keeps existing path for display

        [Display(Name = "Upload New GPay QR Code")]
        [DataType(DataType.Upload)]
        public IFormFile GpayQRCodeFile { get; set; } // Handles new file upload

        [Display(Name = "Current PhonePe QR Code")]
        public string PhonePeQRCodePath { get; set; } // Keeps existing path for display

        [Display(Name = "Upload New PhonePe QR Code")]
        [DataType(DataType.Upload)]
        public IFormFile PhonePeQRCodeFile { get; set; } // Handles new file upload
                                                         // --------------------------------------------------------------
    }

}
