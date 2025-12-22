using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[a-zA-Z0-9._]+$", ErrorMessage = "Username can only contain letters, numbers, dots, and underscores.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; }

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50)]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50)]
        public string LastName { get; set; }

        // --- STRICT MOBILE VALIDATION ---
        [Required(ErrorMessage = "Mobile number is required")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Please enter a valid 10-digit mobile number starting with 6-9.")]
        [StringLength(10, MinimumLength = 10, ErrorMessage = "Mobile number must be exactly 10 digits.")]
        public string PhoneNumber { get; set; }

        [StringLength(15)]
        [Display(Name = "GST Number")]
        [RegularExpression(@"^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$",
    ErrorMessage = "Invalid GSTIN format. (Ex: 22AAAAA0000A1Z5)")]
        public string? GSTIN { get; set; }

        [Required(ErrorMessage = "ITS Number is required")]
        [StringLength(20)]
        public string ITSNumber { get; set; }

        [Display(Name = "WhatsApp Number")]
        [RegularExpression(@"^[6-9]\d{9}$", ErrorMessage = "Invalid WhatsApp number format.")]
        public string? WhatsAppNumber { get; set; }

        [Required(ErrorMessage = "Store name is required")]
        [StringLength(100)]
        public string StoreName { get; set; }

        // --- ADDRESS VALIDATION ---
        [Required(ErrorMessage = "Street address is required")]
        [StringLength(200)]
        public string StreetAddress { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(50)]
        public string City { get; set; }

        [Required(ErrorMessage = "Postal code is required")]
        [RegularExpression(@"^\d{5,10}$", ErrorMessage = "Postal code must be between 5 and 10 digits.")]
        public string PostalCode { get; set; }

        [Required(ErrorMessage = "Country is required")]
        [StringLength(50)]
        public string Country { get; set; }
    }
}