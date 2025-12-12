namespace CMSECommerce.Areas.Seller.Models
{
 public class SellerDashboardViewModel
 {
 public int UsersCount { get; set; }
 public int ProductsCount { get; set; }
 public int OrdersCount { get; set; }
 public int PendingSubscriberRequests { get; set; }

 // recent orders for quick access
 public IEnumerable<Order> RecentOrders { get; set; }
        public int IsProcessedCount { get; set; }
        public int Categories { get;  set; }
        public int LowProductsCount { get;  set; }
    }
}