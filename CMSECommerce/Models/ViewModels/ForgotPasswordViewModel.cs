
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; }
    }
}
