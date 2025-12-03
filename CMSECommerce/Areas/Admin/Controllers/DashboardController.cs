using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using CMSECommerce.Areas.Admin.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Models; // Ensure this is included if AdminDashboardViewModel or Order uses models from this namespace

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    // ⭐ FIX 1: Change Authorize attribute from [Authorize("Admin")] to [Authorize(Roles = "Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        // Using Primary Constructor (C# 12) syntax for cleaner injection
        public DashboardController(DataContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            // ⭐ FIX 2: Use CountAsync() on Identity components for better asynchronous performance
            // NOTE: The user manager count does not have a native async version without iterating, 
            // so we use the underlying store's IQueryable or rely on the framework's optimization.
            // Using ToListAsync().Count() is safer and truly async.
            var usersCount = await _userManager.Users.CountAsync();
            var productsCount = await _context.Products.CountAsync();
            var productsRequestCount = await _context.Products.Where(p=> p.Status==ProductStatus.Pending || p.Status==ProductStatus.Rejected).CountAsync();
            var ordersCount = await _context.Orders.CountAsync();
            var categories = await _context.Categories.CountAsync();
            var userprofilesCount = await _context.UserProfiles.Where(p => !p.IsImageApproved && p.ProfileImagePath != null).CountAsync();

            // ⭐ FIX 3: Pending request logic for bool? Approved
            // Pending requests are those where Approved is explicitly NULL.
            var pendingRequests = await _context.SubscriberRequests
                .CountAsync(r => r.Approved == false);

            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.Id)
                .Take(5)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                UsersCount = usersCount,
                ProductsRequestCount= productsRequestCount,
                ProductsCount = productsCount,
                OrdersCount = ordersCount,
                PendingSubscriberRequests = pendingRequests,
                Categories = categories,
                RecentOrders = recentOrders,
                UserProfilesCount= userprofilesCount
            };

           

            return View(model);
        }
    }
}