using System.ComponentModel.DataAnnotations;

namespace CMSECommerce.Models.ViewModels
{
    public class SubscriptionRequestViewModel
    {
        // Hidden field to track the selected tier
        [Required]
        public int TierId { get; set; }

        // Display properties for the UI (Non-editable)
        public string? TierName { get; set; }
        public decimal Price { get; set; }

        // AC 2: ITS Number field
        [Required(ErrorMessage = "ITS Number is required.")]
        [Display(Name = "ITS Number")]
        [StringLength(10, MinimumLength = 8, ErrorMessage = "Please enter a valid ITS Number.")]
        public string ITSNumber { get; set; }

        // AC 2: Payment Receipt field
        [Required(ErrorMessage = "Please upload your payment receipt.")]
        [Display(Name = "Payment Receipt (Image)")]
        [DataType(DataType.Upload)]
        public IFormFile Receipt { get; set; }

        // Optional: Helpful for displaying existing status if they revisit
        public string? StatusMessage { get; set; }
    }
}

