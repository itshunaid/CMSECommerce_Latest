namespace CMSECommerce.Areas.Seller.Models
{
    public class OrderActionViewModel
    {
        public int OrderDetailId { get; set; }
        public string? Note { get; set; }

        // For Processing
        public IFormFile? DeliveryImage { get; set; }

        // For Cancellation
        public string? SelectedReason { get; set; }
        public string? CustomReason { get; set; }

        public List<string> PredefinedReasons => new()
    {
        "Out of Stock",
        "Pricing Error",
        "Shipping Area Not Supported",
        "Damaged Item"
    };
    }
}
