using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using CMSECommerce.Models; // Need this for Order, PagedList<Order>
using System;
using System.Collections.Generic; // For initializing lists on error

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    // NOTE: If using role-based security, the Authorize attribute should be [Authorize(Roles = "Admin")]
    // Based on the original code, I will use [Authorize(Roles = "Admin")] which is the standard, 
    // assuming "Admin" is a role, otherwise keep it as [Authorize("Admin")] if it's a policy name.
    [Authorize(Roles = "Admin")]
    public class OrdersController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

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

            // Initialize variables outside the try block
            IQueryable<Order> orders = _context.Orders.OrderByDescending(x => x.Id).AsQueryable();
            int count = 0;
            List<Order> items = new List<Order>();

            try
            {
                // 2. Apply Filters to the Query

                // Filter by Order ID
                if (!string.IsNullOrEmpty(orderId))
                {
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
                    orders = orders.Where(o => o.DateTime.Date >= minDate.Value.Date);
                }
                if (maxDate.HasValue)
                {
                    orders = orders.Where(o => o.DateTime.Date <= maxDate.Value.Date);
                }

                // 4. Execute Query and Apply Pagination
                count = await orders.CountAsync();
                items = await orders
                    .OrderByDescending(o => o.DateTime) // Always sort for consistent pagination
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

            }
            catch (DbUpdateException dbEx)
            {
                // Log database errors
                // _logger.LogError(dbEx, "Database error retrieving orders for Admin.");
                TempData["Error"] = "A database error occurred while loading the orders list.";
                // Continue with zeroed items and count
            }
            catch (Exception ex)
            {
                // Log general exceptions
                // _logger.LogError(ex, "Unexpected error in Orders Index action.");
                TempData["Error"] = "An unexpected error occurred while processing orders.";
                // Continue with zeroed items and count
            }

            // 3. Store Current Filter Values for the View (outside the try-catch)
            ViewData["CurrentOrderId"] = orderId;
            ViewData["CurrentStatus"] = status;
            ViewData["CurrentMinTotal"] = minTotal?.ToString();
            ViewData["CurrentMaxTotal"] = maxTotal?.ToString();
            ViewData["CurrentMinDate"] = minDate?.ToString("yyyy-MM-dd"); // Format for HTML date input
            ViewData["CurrentMaxDate"] = maxDate?.ToString("yyyy-MM-dd"); // Format for HTML date input

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
            Order order = null;
            List<OrderDetail> details = new List<OrderDetail>();

            try
            {
                order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);
                if (order == null)
                {
                    TempData["Warning"] = $"Order ID {id} not found.";
                    return RedirectToAction("Index");
                }

                details = await _context.OrderDetails.AsNoTracking().Where(d => d.OrderId == id).ToListAsync();
            }
            catch (DbUpdateException dbEx)
            {
                // Log database errors
                // _logger.LogError(dbEx, "Database error retrieving order details for ID: {OrderId}", id);
                TempData["Error"] = "A database error occurred while fetching order details.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log general exceptions
                // _logger.LogError(ex, "Unexpected error retrieving order details for ID: {OrderId}", id);
                TempData["Error"] = "An unexpected error occurred.";
                return RedirectToAction("Index");
            }

            return View(new OrderDetailsViewModel { Order = order, OrderDetails = details });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShippedStatus(int id, bool shipped)
        {
            Order order;

            try
            {
                order = await _context.Orders.FirstOrDefaultAsync(x => x.Id == id);

                if (order == null)
                {
                    TempData["Error"] = $"Order ID {id} not found for status update.";
                    return RedirectToAction("Index");
                }

                order.Shipped = shipped;

                // This performs the update directly on the database with a single command 
                // (less memory usage than loading all details)
                var rowsAffected = await _context.OrderDetails
                    .Where(od => od.OrderId == id)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(od => od.IsProcessed, shipped)
                    );

                // Update the main Order record
                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order ID {id} status has been updated to {(shipped ? "Shipped" : "Pending")}!";
            }
            catch (DbUpdateException dbEx)
            {
                // Log database errors
                // _logger.LogError(dbEx, "Database error updating shipment status for Order ID: {OrderId}", id);
                TempData["Error"] = "A database error occurred while saving the shipment status. Please try again.";
            }
            catch (Exception ex)
            {
                // Log general exceptions
                // _logger.LogError(ex, "Unexpected error updating shipment status for Order ID: {OrderId}", id);
                TempData["Error"] = "An unexpected error occurred during the status update.";
            }

            return RedirectToAction("Index");
        }
    }
}