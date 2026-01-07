namespace CMSECommerce.Areas.Seller.Models
{
    public class FulfillmentViewModel
    {
        public int OrderDetailId { get; set; } // The specific item being shipped
        public string SellerNote { get; set; }
        public IFormFile DeliveryImage { get; set; } // The physical file
    }
}
