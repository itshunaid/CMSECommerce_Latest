using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class RegisterViewModel
    {
        // --- CORE IDENTITY FIELDS (Keep Required for Amazon-style login) ---
       
        [StringLength(50, MinimumLength = 3)]
        [Required(ErrorMessage = "Please enter your Email, Mobile, or ITS number")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Please re-enter your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Re-enter password")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit mobile number starting with 6-9.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Mobile number must be exactly 10 digits.")]
        public string PhoneNumber { get; set; }

        // --- OPTIONAL FIELDS (Removed [Required] to prevent breaking functionality) ---

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [StringLength(20)]
        public string? ITSNumber { get; set; }

        [Display(Name = "WhatsApp Number")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid WhatsApp number format.")]
        public string? WhatsAppNumber { get; set; }

        [StringLength(100)]
        public string? StoreName { get; set; }

        [StringLength(15)]
        public string? GSTIN { get; set; }

        [StringLength(200)]
        public string? StreetAddress { get; set; }

        [StringLength(50)]
        public string? City { get; set; }

        [RegularExpression(@"^\d{5,10}$", ErrorMessage = "Postal code must be between 5 and 10 digits.")]
        public string? PostalCode { get; set; }

        [StringLength(50)]
        public string? Country { get; set; }
    }
}