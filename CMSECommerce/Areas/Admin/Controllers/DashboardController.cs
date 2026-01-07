using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    // It is highly recommended to also inject ILogger<DashboardController> for production logging.
    public class DashboardController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public DashboardController(DataContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // Initialize the model with default (zero) values
            var model = new AdminDashboardViewModel
            {
                UsersCount = 0,
                ProductsRequestCount = 0,
                ProductsCount = 0,
                OrdersCount = 0,
                PendingSubscriberRequests = 0,
                Categories = 0,
                RecentOrders = new List<Order>(),
                UserProfilesCount = 0,
                DeactivatedStoresCount = 0 // New property initialized
            };

            try
            {
                // 1. Identity User Count
                model.UsersCount = await _userManager.Users.CountAsync();

                // 2. Total Products
                model.ProductsCount = await _context.Products.CountAsync();

                // 3. Pending/Rejected Product Requests
                model.ProductsRequestCount = await _context.Products
                    .Where(p => p.Status == ProductStatus.Pending || p.Status == ProductStatus.Rejected)
                    .CountAsync();

                // 4. Global Order Metrics
                model.OrdersCount = await _context.Orders.CountAsync();

                // 5. Taxonomy Metrics
                model.Categories = await _context.Categories.CountAsync();

                // 6. Profile Image Moderation Queue
                model.UserProfilesCount = await _context.UserProfiles
                    .Where(p => !p.IsImageApproved && p.ProfileImagePath != null)
                    .CountAsync();

                // 7. Seller Onboarding Queue
                model.PendingSubscriberRequests = await _context.SubscriberRequests
                    .CountAsync(r => r.Approved == false);

                // 8. NEW: Deactivated Stores Metric (Architecture Churn Metric)
                // We count stores where IsActive is explicitly false
                model.DeactivatedStoresCount = await _context.Stores
                    .CountAsync(s => !s.IsActive);

                // 9. Recent Activity Feed
                model.RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // ARCHITECT NOTE: Ensure you have an ILogger injected to capture 'ex' details
                // _logger.LogError(ex, "Error fetching Admin Dashboard stats");

                TempData["Error"] = "An error occurred while loading dashboard statistics. Data displayed may be incomplete.";
            }

            return View(model);
        }
    }
}