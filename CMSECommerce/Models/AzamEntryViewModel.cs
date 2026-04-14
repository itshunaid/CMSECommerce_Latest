using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models
{
    public class AzamEntryViewModel
    {
        [Required(ErrorMessage = "ITS Number is required")]
        [RegularExpression(@"^\d{4,10}$", ErrorMessage = "ITS Number must be 4-10 digits")]
        public string ITSNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(200, ErrorMessage = "Full Name cannot exceed 200 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Consent is mandatory")]
        public bool ConsentGiven { get; set; }
    }
}
