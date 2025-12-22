using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }

        // Add these to fix "does not contain definition" errors
        public string GSTIN { get; set; }
        public string ITSNumber { get; set; }
        public string WhatsAppNumber { get; set; }
        public string StoreName { get; set; }

        // Address fields used in your Register action
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; } // Note: Logic uses PostalCode, Store uses PostCode
        public string Country { get; set; }
    }
}