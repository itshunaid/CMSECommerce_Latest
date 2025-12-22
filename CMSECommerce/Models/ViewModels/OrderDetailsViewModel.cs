namespace CMSECommerce.Models.ViewModels
{
    public class OrderDetailsViewModel
    {
        public Order Order { get; set; }
        public List<OrderDetail> OrderDetails { get; set; }
        public UserProfile UserProfile { get; set; }
        // Helper property to access store data directly from the profile
        public Store Store => UserProfile?.Store;

        // New: Dictionary to hold seller profiles keyed by ProductOwner ID
        public Dictionary<string, UserProfile> SellerProfiles { get; set; } = new Dictionary<string, UserProfile>();
    }
}
