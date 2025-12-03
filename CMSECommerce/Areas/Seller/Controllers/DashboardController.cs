using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;
using CMSECommerce.Areas.Seller.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize("Subscriber")]
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
            var usersCount = await _userManager.Users.CountAsync();
            var productsCount = await _context.Products.Where(p=> p.OwnerId== _userManager.GetUserName(User)).CountAsync();
            var ordersCount = await _context.OrderDetails.Where(p=> p.ProductOwner== _userManager.GetUserName(User) && p.IsProcessed==false).CountAsync();
            var isProcessedCount = await _context.OrderDetails.Where(p => p.ProductOwner == _userManager.GetUserName(User) && p.IsProcessed == true).CountAsync();
            var pendingRequests = await _context.SubscriberRequests.CountAsync(r => !r.Approved);
            var categories = await _context.Categories.CountAsync();

            var recentOrders = await _context.Orders.OrderByDescending(o => o.Id).Take(5).ToListAsync();

            var model = new SellerDashboardViewModel
            {
                UsersCount = usersCount,
                ProductsCount = productsCount,
                OrdersCount = ordersCount,
                PendingSubscriberRequests = pendingRequests,
                RecentOrders = recentOrders,
                Categories= categories,
                IsProcessedCount = isProcessedCount
            };

            return View(model);
        }
    }
}
