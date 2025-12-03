using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
    public class OrdersController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        //public async Task<IActionResult> Index()
        //{
        //    List<Order> orders = await _context.Orders.OrderByDescending(x => x.Id).ToListAsync();

        //    return View(orders);
        //}

        // Your Controller would return PagedList<Order> instead of IEnumerable<Order>
        // Example Controller Action (conceptual):
        // Index action now accepts all filter parameters and the current page
        public async Task<IActionResult> Index(
            int? page,
            string orderId,
            string status,
            decimal? minTotal,
            decimal? maxTotal,
            DateTime? minDate,
            DateTime? maxDate)
        {
            // 1. Setup Pagination and Filtering Defaults
            int pageSize = 10;
            int pageNumber = page ?? 1; // Default to page 1

            // Get the current user ID (IMPORTANT for a customer order history page)
            // You must ensure this logic works with your authentication system (e.g., using ClaimsPrincipal)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Start with all orders for the current user
            var orders = _context.Orders.OrderByDescending(x => x.Id).AsQueryable();

            // 2. Apply Filters to the Query

            // Filter by Order ID
            if (!string.IsNullOrEmpty(orderId))
            {
                // Assuming Order Id (Id) is an integer, we filter based on a partial match or exact match. 
                // If it's a long integer, you might use ToString() and Contains().
                if (int.TryParse(orderId, out int id))
                {
                    orders = orders.Where(o => o.Id == id);
                }
            }

            // Filter by Status (Shipped property is a boolean)
            if (!string.IsNullOrEmpty(status))
            {
                if (status.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
                {
                    orders = orders.Where(o => o.Shipped == true);
                }
                else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                {
                    orders = orders.Where(o => o.Shipped == false);
                }
            }

            // Filter by Total Range
            if (minTotal.HasValue)
            {
                orders = orders.Where(o => o.GrandTotal >= minTotal.Value);
            }
            if (maxTotal.HasValue)
            {
                orders = orders.Where(o => o.GrandTotal <= maxTotal.Value);
            }

            // Filter by Date Range
            if (minDate.HasValue)
            {
                // Use Date to filter orders placed on or after the min date
                orders = orders.Where(o => o.DateTime.Date >= minDate.Value.Date);
            }
            if (maxDate.HasValue)
            {
                // Use Date to filter orders placed on or before the max date
                // Note: Add one day to maxDate to include all orders on that last day
                orders = orders.Where(o => o.DateTime.Date <= maxDate.Value.Date);
            }

            // 3. Store Current Filter Values for the View (used for form persistence and pagination links)
            ViewData["CurrentOrderId"] = orderId;
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentMinTotal"] = minTotal?.ToString();
            ViewData["CurrentMaxTotal"] = maxTotal?.ToString();
            ViewData["CurrentMinDate"] = minDate?.ToString("yyyy-MM-dd"); // Format for HTML date input
            ViewData["CurrentMaxDate"] = maxDate?.ToString("yyyy-MM-dd"); // Format for HTML date input

            // 4. Execute Query and Apply Pagination
            var count = await orders.CountAsync();
            var items = await orders
                .OrderByDescending(o => o.DateTime) // Always sort for consistent pagination
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Create the PagedList instance (assuming you have this custom ViewModel)
            var pagedList = new PagedList<Order>(items, count, pageNumber, pageSize);

            return View(pagedList);
        }



        public IActionResult Create()
        {
            return View();
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            var details = await _context.OrderDetails.Where(d => d.OrderId == id).ToListAsync();
            return View(new CMSECommerce.Models.ViewModels.OrderDetailsViewModel { Order = order, OrderDetails = details });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShippedStatus(int id, bool shipped)
        {
            Order order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == id);

            order.Shipped = shipped;

            // This performs the update directly on the database with a single command.
            var rowsAffected = await _context.OrderDetails
                .Where(od => od.OrderId == id)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(od => od.IsProcessed, shipped)
                );
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["success"] = "The order has been modified!";

            return RedirectToAction("Index");
        }
    }
}