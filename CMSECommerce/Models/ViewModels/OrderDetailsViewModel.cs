namespace CMSECommerce.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public Order Order { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public UserProfile UserProfile { get; set; }
    }
}
