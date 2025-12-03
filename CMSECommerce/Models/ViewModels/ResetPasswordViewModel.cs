
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}
