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
                UserProfilesCount = 0
            };

            try
            {
                // To avoid "A second operation was started on this context", 
                // we await each query individually. 
                // This ensures the DbContext finishes one task before starting the next.

                model.UsersCount = await _userManager.Users.CountAsync();

                model.ProductsCount = await _context.Products.CountAsync();

                model.ProductsRequestCount = await _context.Products
                    .Where(p => p.Status == ProductStatus.Pending || p.Status == ProductStatus.Rejected)
                    .CountAsync();

                model.OrdersCount = await _context.Orders.CountAsync();

                model.Categories = await _context.Categories.CountAsync();

                model.UserProfilesCount = await _context.UserProfiles
                    .Where(p => !p.IsImageApproved && p.ProfileImagePath != null)
                    .CountAsync();

                model.PendingSubscriberRequests = await _context.SubscriberRequests
                    .CountAsync(r => r.Approved == false);

                model.RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log general exceptions (It's better to catch general Exception to handle both DB and Logic errors)
                // _logger.LogError(ex, "Error in Admin Dashboard Index action.");
                TempData["Error"] = "An error occurred while loading dashboard statistics. Data displayed may be incomplete.";
            }

            return View(model);
        }
    }
}