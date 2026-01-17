namespace CMSECommerce.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        public int UsersCount { get; set; }
        public int ProductsCount { get; set; }
        public int OrdersCount { get; set; }
        public int PendingSubscriberRequests { get; set; }
        public int DeactivatedStoresCount { get; set; }

        // recent orders for quick access
        public IEnumerable<CMSECommerce.Models.Order> RecentOrders { get; set; }
        public int ProductsRequestCount { get; internal set; }
        public int Categories { get; set; }
        public int UserProfilesCount { get; set; }
        public int PendingUnlockRequests { get; set; }
        public IEnumerable<SellerDeclineSummary> SellersWithDeclines { get; set; }
    }
}