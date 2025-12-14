using CMSECommerce.Infrastructure;
using CMSECommerce.Models; // Assuming Order and OrderDetail are here
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Threading.Tasks;
// using Microsoft.Extensions.Logging; // Include this if logging is required

namespace CMSECommerce.Controllers
{
    // Injecting dependencies in the primary constructor (C# 12 feature)
    public class OrdersController(
                                 DataContext context,
                                 UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager
                                 /*, ILogger<OrdersController> logger */) : Controller
    {
        private readonly DataContext _context = context;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        // private readonly ILogger<OrdersController> _logger = logger; // Uncomment if logging is enabled

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> MyOrders(
        int? page,
        string orderId,
        string status,
        decimal? minTotal,
        decimal? maxTotal,
        DateTime? minDate,
        DateTime? maxDate)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "You must be logged in to view your orders.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            try
            {
                var userName = _userManager.GetUserName(User);
                // 1. Setup Pagination and Filtering Defaults
                int pageSize = 10;
                int pageNumber = page ?? 1; // Default to page 1

                // The current 'username' variable is assumed to hold the case-insensitive username to match.
                string usernameLower = userName.ToLower();


                // Start with all orders for the current user
                // NOTE: Use FindAsync or similar if User has UserId property, otherwise use AsQueryable()
                var orders = _context.Orders
                                     .OrderByDescending(x => x.Id)
                                     .Where(o => o.UserName.ToLower().Contains(usernameLower))
                                     .AsQueryable();

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
                    // Filter orders placed on or before the max date
                    orders = orders.Where(o => o.DateTime.Date <= maxDate.Value.Date);
                }

                // 3. Store Current Filter Values for the View (used for form persistence and pagination links)
                ViewData["CurrentOrderId"] = orderId;
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentMinTotal"] = minTotal?.ToString();
                ViewData["CurrentMaxTotal"] = maxTotal?.ToString();
                ViewData["CurrentMinDate"] = minDate?.ToString("yyyy-MM-dd");
                ViewData["CurrentMaxDate"] = maxDate?.ToString("yyyy-MM-dd");

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
            catch (DbUpdateException dbEx)
            {
                // _logger.LogError(dbEx, "Database error while fetching orders for user {Username}.", _userManager.GetUserName(User));
                TempData["Error"] = "We encountered a database error while retrieving your orders. Please try again later.";
                return View(new PagedList<Order>(new List<Order>(), 0, page ?? 1, 10)); // Return empty list on failure
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Unexpected error in MyOrders action for user {Username}.", _userManager.GetUserName(User));
                TempData["Error"] = "An unexpected error occurred while loading your order history.";
                return RedirectToAction("Index", "Home");
            }
        }


        // Note: This method should be placed in the appropriate controller (likely OrdersController or CheckoutController).

        [HttpPost]
        public async Task<IActionResult> Create()
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

            // Scenario 1: Empty Cart
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your shopping cart is empty. Please add items before checking out.";
                return RedirectToAction("Index", "Cart");
            }

            // Scenario 2: Unauthenticated User
            var user = await _signInManager.UserManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction("Login", "Account", new { area = "Identity" }); // Redirect to login page
            }

            // 1. Safely retrieve the UserProfile object
            var userProfile = await _context.UserProfiles
                .Where(x => x.UserId == user.Id)
                .FirstOrDefaultAsync();

            // 2. Check if the profile exists and concatenate the names
            string fullName = userProfile != null
                ? userProfile.FirstName + " " + userProfile.LastName
                : User.Identity.Name; // Fallback to Identity Username

            // Use a database transaction to ensure atomicity (all or nothing)
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // --- 🛠️ Final Stock Check and Product Fetching ---

                // Get IDs of all products in the cart
                var productIds = cart.Select(item => item.ProductId).ToList();

                // Fetch all required Product entities from the database, tracking them for update
                // Use .Include(p => p.StockQuantity) if StockQuantity is in a related entity, but here it seems direct.
                var products = await _context.Products
                                             .Where(p => productIds.Contains(p.Id))
                                             .ToListAsync();

                // Use a dictionary for fast lookup
                var productMap = products.ToDictionary(p => p.Id);

                // Scenario 3: Critical Stock Issue Check (Prevents Race Conditions)
                var stockIssues = new List<string>();

                foreach (var item in cart)
                {
                    if (productMap.TryGetValue(item.ProductId, out var product))
                    {
                        // Check if ordered quantity exceeds currently available stock
                        if (item.Quantity > product.StockQuantity)
                        {
                            stockIssues.Add($"{item.ProductName} ({item.Quantity} requested) - Only {product.StockQuantity} available.");
                        }
                    }
                    else
                    {
                        // Product missing (deleted from store)
                        stockIssues.Add($"Product ID {item.ProductId} ({item.ProductName}) is no longer available in the store. Please remove it from your cart.");
                    }
                }

                if (stockIssues.Any())
                {
                    // Abort the transaction and notify the user of specific shortages
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Order failed due to critical issues. Please review and adjust the following items: <br/>- " + string.Join("<br/>- ", stockIssues);
                    return RedirectToAction("Index", "Cart");
                }


                // --- If all checks pass, proceed with Order Transaction ---

                // 1. Create the Order header
                Order order = new Order
                {
                    // Use a combination of full name and unique username for clarity
                    UserName = $"{fullName} ({User.Identity.Name})",
                    PhoneNumber = user.PhoneNumber,
                    GrandTotal = cart.Sum(x => x.Price * x.Quantity),
                    DateTime = DateTime.UtcNow // Set creation time
                };

                _context.Add(order);
                await _context.SaveChangesAsync(); // Save to get the Order ID

                int orderId = order.Id;

                // 2. Process Order Details and Update Stock
                foreach (var item in cart)
                {
                    // Retrieve the tracked product entity using the map
                    var product = productMap[item.ProductId];

                    // 🛠️ CORE LOGIC: Subtract the ordered quantity from the stock
                    product.StockQuantity -= item.Quantity;
                    // Note: Since 'products' was fetched using ToListAsync(), EF Core is already tracking it.
                    // _context.Update(product) is technically optional here but harmless.

                    // Create the OrderDetail entry
                    OrderDetail orderDetail = new()
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Image = item.Image,
                        // Assuming these properties exist on OrderDetail, otherwise adjust
                        ProductOwner = item.ProductOwner ?? "N/A",
                        Customer = User.Identity.Name,
                        CustomerNumber = user.PhoneNumber
                    };

                    _context.Add(orderDetail);
                }

                // 3. Save all Order Details and Stock Quantity updates
                await _context.SaveChangesAsync();

                // Commit the transaction only if SaveChangesAsync succeeded
                await transaction.CommitAsync();

                // 4. Success: Clear the cart and notify the user
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = $"Your order (ID: {orderId}) has been placed successfully! A confirmation has been sent.";

                // Redirect to the Order Details page
                return RedirectToAction("OrderDetails", "Account", new { area = "Identity", id = orderId });
            }
            // Scenario 4A: Database error during SaveChanges (e.g., concurrency, constraint violation)
            catch (DbUpdateException dbEx)
            {
                await transaction.RollbackAsync();
                // _logger.LogError(dbEx, "DbUpdateException occurred while placing order for user {Username}.", User.Identity.Name);
                TempData["Error"] = "A database error prevented the order from being placed. This may be a temporary issue or a data conflict. Please try again.";
                return RedirectToAction("Index", "Cart");
            }
            // Scenario 4B: General Exception Error
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // _logger.LogError(ex, "Unexpected error occurred while placing order for user {Username}", User.Identity.Name);

                TempData["Error"] = "An unexpected error occurred while processing your order. The transaction was cancelled. Please try again or contact support.";
                return RedirectToAction("Index", "Cart"); // Redirect back to cart or an error page
            }
        }
    }
}