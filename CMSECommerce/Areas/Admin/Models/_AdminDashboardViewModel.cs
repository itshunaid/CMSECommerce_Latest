using CMSECommerce.Areas.Admin.Models;

namespace CMSECommerce.Areas.Admin.Models
{
    public class _AdminDashboardViewModel
    {
        public int UsersCount { get; set; }
        public int ProductsRequestCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
        public int PendingSubscriberRequests { get; set; }
        public int Categories { get; set; }
        public List<Order> RecentOrders { get; set; } = new List<Order>();
        public int UserProfilesCount { get; set; }
        public int DeactivatedStoresCount { get; set; }
        public List<SellerDeclineSummary> SellersWithDeclines { get; set; } = new List<SellerDeclineSummary>();
    }
}
