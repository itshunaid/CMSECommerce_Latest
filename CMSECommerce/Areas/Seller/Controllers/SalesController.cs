using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Areas.Seller.Models;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Subscriber,Admin,SuperAdmin")]
    public class SalesController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<SalesController> _logger;

        public SalesController(DataContext context, UserManager<IdentityUser> userManager, ILogger<SalesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<IActionResult> Report()
        {
            var model = new SellerDashboardViewModel();
            string currentUserName = _userManager.GetUserName(User);

            try
            {
                // Total Revenue
                model.TotalRevenue = await _context.OrderDetails
                    .Where(od => od.ProductOwner == currentUserName && !od.IsCancelled)
                    .SumAsync(od => od.Quantity * od.Price);

                // Monthly Sales Trends
                var orderDetailsWithOrders = await _context.OrderDetails
                    .Where(od => od.ProductOwner == currentUserName && !od.IsCancelled)
                    .Include(od => od.Order)
                    .Where(od => od.Order != null && od.Order.OrderDate.HasValue)
                    .ToListAsync();

                model.MonthlySales = orderDetailsWithOrders
                    .GroupBy(od => od.Order.OrderDate.Value.ToString("yyyy-MM"))
                    .ToDictionary(g => g.Key, g => g.Sum(od => od.Quantity * od.Price));

                // Top Selling Products
                var topProducts = await _context.OrderDetails
                    .Where(od => od.ProductOwner == currentUserName && !od.IsCancelled)
                    .GroupBy(od => od.ProductName)
                    .Select(g => new
                    {
                        ProductName = g.Key,
                        TotalSold = g.Sum(od => od.Quantity),
                        TotalRevenue = g.Sum(od => od.Quantity * od.Price)
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(10) // Show more products in detailed report
                    .ToListAsync();

                model.TopSellingProducts = topProducts.Select(p => new SellerDashboardViewModel.TopSellingProduct
                {
                    ProductName = p.ProductName,
                    TotalSold = p.TotalSold,
                    TotalRevenue = p.TotalRevenue
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing sales analytics for seller {User}.", currentUserName);
                TempData["Error"] = "Error loading sales report data.";
            }

            return View(model);
        }
    }
}
