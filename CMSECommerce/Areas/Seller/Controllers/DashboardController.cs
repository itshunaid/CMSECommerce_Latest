using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Areas.Seller.Models;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    // NOTE: Authorization attribute must be used with Roles or Policies if not the default scheme name
    // Assuming 'Subscriber' is a policy name, if it's a role, it should be [Authorize(Roles = "Subscriber")]
    // I will use [Authorize(Roles = "Subscriber")] as per standard practice, but keep the original logic.
    [Authorize(Roles = "Subscriber")]
    public class DashboardController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<DashboardController> _logger; // ADDED ILogger field

        public DashboardController(DataContext context, UserManager<IdentityUser> userManager, ILogger<DashboardController> logger) // ADDED ILogger injection
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var model = new SellerDashboardViewModel();
            string currentUserName = _userManager.GetUserName(User);
            string userId = _userManager.GetUserId(User); // Get user ID for more specific logging

            // Helper function to safely execute database/identity queries
            async Task<int> SafeCountAsync(Func<Task<int>> query, string metricName)
            {
                try
                {
                    return await query();
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error retrieving {Metric} for seller {User}.", metricName, currentUserName);
                    TempData["Warning"] = TempData["Warning"] + $"Error loading {metricName}. ";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error retrieving {Metric} for seller {User}.", metricName, currentUserName);
                    TempData["Warning"] = TempData["Warning"] + $"Error loading {metricName}. ";
                }
                return 0; // Return zero on failure
            }

            // 1. Users Count (Global or related to Identity)
            model.UsersCount = await SafeCountAsync(() => _userManager.Users.CountAsync(), "Total Users Count");

            // 2. Seller-Specific Product Counts
            model.ProductsCount = await SafeCountAsync(() => _context.Products.Where(p => p.OwnerName == currentUserName).CountAsync(), "Products Count");
            model.LowProductsCount = await SafeCountAsync(() => _context.Products.Where(p => p.OwnerName == currentUserName && p.StockQuantity == 0).CountAsync(), "Low Stock Count");

            // Product Visibility Counts
            model.VisibleProductsCount = await SafeCountAsync(() => _context.Products.Where(p => p.OwnerName == currentUserName && p.IsVisible == true).CountAsync(), "Visible Products Count");
            model.HiddenProductsCount = await SafeCountAsync(() => _context.Products.Where(p => p.OwnerName == currentUserName && p.IsVisible == false).CountAsync(), "Hidden Products Count");

            // 3. Seller-Specific Order Counts
            model.OrdersCount = await SafeCountAsync(() => _context.OrderDetails.Where(p => p.ProductOwner == currentUserName && p.IsProcessed == false && !p.IsCancelled).CountAsync(), "Pending Orders Count");
            model.IsProcessedCount = await SafeCountAsync(() => _context.OrderDetails.Where(p => p.ProductOwner == currentUserName && p.IsProcessed == true).CountAsync(), "Processed Orders Count");
            // UPDATED: Count only items that ARE cancelled and belong to this seller
            model.IsOrderCancelledCount = await SafeCountAsync(() => _context.OrderDetails
                .Where(p => p.ProductOwner == currentUserName && p.IsCancelled == true).CountAsync(), "Cancelled Orders Count");
            // 4. Global Counts (Admin-like metrics visible to seller)
            model.Categories = await SafeCountAsync(() => _context.Categories.CountAsync(), "Categories Count");
            // NOTE: The original code showed pending requests (r => !r.Approved) which seems like an Admin metric.
            // Keeping it for implementation, but usually sellers only see their data.
            model.PendingSubscriberRequests = await SafeCountAsync(() => _context.SubscriberRequests.CountAsync(r => r.Approved == false || r.Approved == null), "Pending Subscriber Requests");

            // 5. Recent Orders (Requires special handling if orders are only linked via OrderDetails)
            try
            {
                // This complex query is prone to failure, so it's handled separately.
                // Assuming OrderDetails has an OrderId linking back to Orders table
                var recentOrderIds = await _context.OrderDetails
                    .Where(od => od.ProductOwner == currentUserName)
                    .Select(od => od.OrderId)
                    .Distinct()
                    .OrderByDescending(id => id)
                    .Take(5)
                    .ToListAsync();

                // Fetch the actual Orders
                model.RecentOrders = await _context.Orders
                    .Where(o => recentOrderIds.Contains(o.Id))
                    .OrderByDescending(o => o.Id) // Re-order after fetching
                    .ToListAsync();
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving recent orders for seller {User}.", currentUserName);
                TempData["Warning"] = TempData["Warning"] + "Error loading recent orders list. ";
                model.RecentOrders = new List<Order>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving recent orders for seller {User}.", currentUserName);
                TempData["Warning"] = TempData["Warning"] + "Error loading recent orders list. ";
                model.RecentOrders = new List<Order>();
            }

            // Display a single consolidated error message if any warning occurred
            if (TempData["Warning"] != null)
            {
                TempData["Error"] = $"Dashboard data is incomplete due to errors: {TempData["Warning"]}";
                TempData.Remove("Warning");
            }

            return View(model);
        }
    }
}