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

        // Sales & Financials
        public decimal NetProfit { get; set; }
        public decimal PendingPayouts { get; set; }
        public decimal AverageOrderValue { get; set; }
        public List<SalesTrendData> SalesTrendsData { get; set; } = new List<SalesTrendData>();

        // Operational Tools
        public List<LowStockAlert> LowStockAlerts { get; set; } = new List<LowStockAlert>();
        public OrderStatusCounts OrderStatuses { get; set; } = new OrderStatusCounts();
        public List<TopSellingProduct> TopSellers { get; set; } = new List<TopSellingProduct>();
        public List<SlowMovingProduct> SlowMovers { get; set; } = new List<SlowMovingProduct>();

        // Customer Engagement
        public List<RecentReview> RecentReviews { get; set; } = new List<RecentReview>();
        public int UnreadMessages { get; set; }

        // Storefront Settings
        public StorefrontInfo StoreInfo { get; set; } = new StorefrontInfo();

        public class SalesTrendData
        {
            public string Period { get; set; } // e.g., "2024-01"
            public decimal Revenue { get; set; }
        }

        public class LowStockAlert
        {
            public string ProductName { get; set; }
            public int CurrentStock { get; set; }
            public int MinimumStock { get; set; }
        }

        public class OrderStatusCounts
        {
            public int New { get; set; }
            public int InProgress { get; set; }
            public int Shipped { get; set; }
            public int Returned { get; set; }
        }

        public class SlowMovingProduct
        {
            public string ProductName { get; set; }
            public int DaysSinceLastSale { get; set; }
            public int StockQuantity { get; set; }
        }

        public class RecentReview
        {
            public int ReviewId { get; set; }
            public string ProductName { get; set; }
            public string CustomerName { get; set; }
            public string Comment { get; set; }
            public int Rating { get; set; }
            public DateTime ReviewDate { get; set; }
            public bool HasResponse { get; set; }
        }

        public class StorefrontInfo
        {
            public string StoreName { get; set; }
            public string LogoUrl { get; set; }
            public string Bio { get; set; }
            public string ShippingPolicy { get; set; }
            public string ReturnPolicy { get; set; }
        }
    }
}
