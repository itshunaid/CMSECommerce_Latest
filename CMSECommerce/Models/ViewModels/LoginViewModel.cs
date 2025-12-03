using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required, MinLength(2, ErrorMessage = "Minimum length is 2")]
        [DisplayName("Username")]
        public string UserName { get; set; }

        [DataType(DataType.Password), Required, MinLength(4, ErrorMessage = "Minimum length is 4")]
        public string Password { get; set; }
        
        public bool RememberMe { get; set; }

        public string ReturnUrl { get; set; }
    }
}
