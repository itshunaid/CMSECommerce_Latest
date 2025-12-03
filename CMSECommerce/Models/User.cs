using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{
    public class User
    {
        public string Id { get; set; }

        [Required, MinLength(2, ErrorMessage = "Minimum length is 2")]
        [DisplayName("Username")]
        public string UserName { get; set; }

        
        [DisplayName("First Name")]
        public string FirstName { get; set; }

       
        [DisplayName("Last Name")]
        public string LastName { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [DataType(DataType.Password), Required, MinLength(4, ErrorMessage = "Minimum length is 4")]
        public string Password { get; set; }

        [DataType(DataType.Password), Required, MinLength(4, ErrorMessage = "Minimum length is 4")]
        [DisplayName("Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords must match!")]
        public string ConfirmPassword { get; set; }


        // --- NEW FIELD ADDED ---
        [Required(ErrorMessage = "The mobile number is required.")]
        [Phone] // Ensures the format is validated as a phone number
        [Display(Name = "Mobile Number")]
        [DataType(DataType.PhoneNumber)]
        public string PhoneNumber { get; set; }


        // New: choose role at registration
        [DisplayName("Register as")]
        public string UserType { get; set; } = "Customer"; // values: "Customer" or "Subscriber"

        [Required]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required]
        public string Country { get; set; }
    }
}
