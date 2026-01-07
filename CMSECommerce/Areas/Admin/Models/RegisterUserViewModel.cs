using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Areas.Admin.Models
{
    public class RegisterUserViewModel
    {
        // --- Context Property ---
        // This holds the Identity UserId. It's null during Registration 
        // and populated during Edit.
        public string? Id { get; set; }
        // --- Account ---
        [Required]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [Remote(action: "CheckEmail", controller: "Account", areaName: "Admin")]
        public string Email { get; set; }
        [Required][DataType(DataType.Password)] public string Password { get; set; }
        [DataType(DataType.Password)][Compare("Password")] public string ConfirmPassword { get; set; }

        // --- Profile: Personal ---
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required]
        [RegularExpression(@"^\d{8}$", ErrorMessage = "ITS Number must be exactly 8 digits.")]
        [Remote(action: "CheckITSNumber", controller: "Account", areaName: "Admin")]
        public string ITSNumber { get; set; }
        [Required]
        [Display(Name = "WhatsApp Number")]
        // Validates Indian numbers: Optional +91 or 91, followed by a 6-9 digit, then 9 more digits
        [RegularExpression(@"^(?:\+91|91)?[6-9]\d{9}$", ErrorMessage = "Invalid Indian WhatsApp number.")]
        [Remote(action: "CheckWhatsApp", controller: "Account", areaName: "Admin")]
        public string WhatsAppNumber { get; set; }
        public string Profession { get; set; }
        public string ServicesProvided { get; set; }
        public string About { get; set; }

        // --- Profile: Social & Payment ---
        public string FacebookUrl { get; set; }
        public string LinkedInUrl { get; set; }
        public string InstagramUrl { get; set; }

        // --- Profile: Addresses ---
        [Required] public string HomeAddress { get; set; }
        [Display(Name = "Home Phone Number")]
        // Validates Indian numbers: Optional +91 or 91, followed by a 6-9 digit, then 9 more digits
        [RegularExpression(@"^(?:\+91|91)?[6-9]\d{9}$", ErrorMessage = "Invalid Indian WhatsApp number.")]
        public string HomePhoneNumber { get; set; }
        [Required] public string BusinessAddress { get; set; }

        [Display(Name = "Business Phone Number")]
        // Validates Indian numbers: Optional +91 or 91, followed by a 6-9 digit, then 9 more digits
        [RegularExpression(@"^(?:\+91|91)?[6-9]\d{9}$", ErrorMessage = "Invalid Indian WhatsApp number.")]
        public string BusinessPhoneNumber { get; set; }

        // --- Store Details ---
        [Required]
        [Remote(action: "CheckStoreName", controller: "Account", areaName: "Admin")]
        public string StoreName { get; set; }
        public string GSTIN { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string Country { get; set; }
    }
}
