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
        public int IsOrderCancelledCount { get; set; }

        // Product Visibility Counts
        public int VisibleProductsCount { get; set; }
        public int HiddenProductsCount { get; set; }

        // Sales Analytics
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, decimal> MonthlySales { get; set; } = new Dictionary<string, decimal>();
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new List<TopSellingProduct>();

        public class TopSellingProduct
        {
            public string ProductName { get; set; }
            public int TotalSold { get; set; }
            public decimal TotalRevenue { get; set; }
        }
    }
}
