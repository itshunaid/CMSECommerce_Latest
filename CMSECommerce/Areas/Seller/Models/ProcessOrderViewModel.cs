namespace CMSECommerce.Areas.Seller.Models
{
    public class ProcessOrderViewModel
    {
        public int OrderDetailId { get; set; }
        public string Note { get; set; }
        public IFormFile DeliveryImage { get; set; }
    }
}
