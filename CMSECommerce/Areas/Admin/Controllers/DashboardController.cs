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
            // Initialize the model with default (zero) values in case of failure
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
                // Execute all required counts concurrently (though await is sequential here, 
                // the IQueryable queries benefit from optimization)

                // Fetching all counts
                var usersCountTask = _userManager.Users.CountAsync();
                var productsCountTask = _context.Products.CountAsync();
                var productsRequestCountTask = _context.Products
                    .Where(p => p.Status == ProductStatus.Pending || p.Status == ProductStatus.Rejected)
                    .CountAsync();
                var ordersCountTask = _context.Orders.CountAsync();
                var categoriesCountTask = _context.Categories.CountAsync();
                var userprofilesCountTask = _context.UserProfiles
                    .Where(p => !p.IsImageApproved && p.ProfileImagePath != null)
                    .CountAsync();
                var pendingRequestsTask = _context.SubscriberRequests
                    .CountAsync(r => r.Approved == false); // Assuming false means pending review/rejection

                // Fetch recent orders
                var recentOrdersTask = _context.Orders
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync();

                // Wait for all tasks to complete
                await Task.WhenAll(
                    usersCountTask,
                    productsCountTask,
                    productsRequestCountTask,
                    ordersCountTask,
                    categoriesCountTask,
                    userprofilesCountTask,
                    pendingRequestsTask,
                    recentOrdersTask
                );

                // Assign results to the model
                model.UsersCount = usersCountTask.Result;
                model.ProductsCount = productsCountTask.Result;
                model.ProductsRequestCount = productsRequestCountTask.Result;
                model.OrdersCount = ordersCountTask.Result;
                model.Categories = categoriesCountTask.Result;
                model.UserProfilesCount = userprofilesCountTask.Result;
                model.PendingSubscriberRequests = pendingRequestsTask.Result;
                model.RecentOrders = recentOrdersTask.Result;

            }
            catch (DbUpdateException dbEx)
            {
                // Log database errors (e.g., connection string issues, timeouts)
                // _logger.LogError(dbEx, "Database error in Admin Dashboard Index action.");
                TempData["Error"] = "A database error occurred while loading dashboard statistics. Data displayed may be incomplete.";
            }
            catch (Exception ex)
            {
                // Log general exceptions
                // _logger.LogError(ex, "Unexpected error in Admin Dashboard Index action.");
                TempData["Error"] = "An unexpected error occurred while processing the dashboard data. Data displayed may be incomplete.";
            }

            // Return the model (which contains partial/zero data if an exception occurred)
            return View(model);
        }
    }
}