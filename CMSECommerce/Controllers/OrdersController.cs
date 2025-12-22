using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        /// <summary>
        /// Retrieves a paginated, filtered list of orders. 
        /// Synchronizes the 'Shipped' status: Sets to true only if ALL associated items are processed.
        /// </summary>
        public async Task<IActionResult> MyOrders(
            int? page,
            string orderId,
            string status,
            decimal? minTotal,
            decimal? maxTotal,
            DateTime? minDate,
            DateTime? maxDate)
        {
            // 1. Authentication Guard Clause
            if (!User.Identity.IsAuthenticated)
            {
                TempData["Error"] = "You must be logged in to view your orders.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            try
            {
                // 2. Identity Handling (Using UserId for reliable database linking)
                var userId = _userManager.GetUserId(User);

                // Fetch profile for UI display purposes (ViewBag)
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                const int pageSize = 10;
                int pageNumber = page ?? 1;

                // 3. Status Synchronization Logic (Architectural Efficiency)
                // Check completion status in a single DB round-trip using projection
                var ordersToCheck = await _context.Orders
                    .Where(o => o.UserId == userId && !o.Shipped) // Only check orders not yet shipped
                    .Select(o => new
                    {
                        Order = o,
                        // Condition: True if EVERY item in OrderDetails is processed
                        AllItemsProcessed = o.OrderDetails.All(od => od.IsProcessed)
                    })
                    .ToListAsync();

                bool hasChanges = false;
                foreach (var item in ordersToCheck)
                {
                    if (item.AllItemsProcessed)
                    {
                        item.Order.Shipped = true;
                        _context.Update(item.Order);
                        hasChanges = true;
                    }
                }

                if (hasChanges)
                {
                    await _context.SaveChangesAsync();
                }

                // 4. Build Filtered Query for UI Display
                var filteredOrders = _context.Orders
                    .AsNoTracking()
                    .Where(o => o.UserId == userId);

                // Apply Filters
                if (!string.IsNullOrEmpty(orderId) && int.TryParse(orderId, out int id))
                {
                    filteredOrders = filteredOrders.Where(o => o.Id == id);
                }

                if (!string.IsNullOrEmpty(status))
                {
                    if (status.Equals("Shipped", StringComparison.OrdinalIgnoreCase))
                        filteredOrders = filteredOrders.Where(o => o.Shipped);
                    else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        filteredOrders = filteredOrders.Where(o => !o.Shipped);
                }

                if (minTotal.HasValue) filteredOrders = filteredOrders.Where(o => o.GrandTotal >= minTotal.Value);
                if (maxTotal.HasValue) filteredOrders = filteredOrders.Where(o => o.GrandTotal <= maxTotal.Value);

                if (minDate.HasValue) filteredOrders = filteredOrders.Where(o => o.DateTime.Date >= minDate.Value.Date);
                if (maxDate.HasValue) filteredOrders = filteredOrders.Where(o => o.DateTime.Date <= maxDate.Value.Date);

                // 5. Store UI State for the Filter Form
                ViewData["CurrentOrderId"] = orderId;
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentMinTotal"] = minTotal?.ToString();
                ViewData["CurrentMaxTotal"] = maxTotal?.ToString();
                ViewData["CurrentMinDate"] = minDate?.ToString("yyyy-MM-dd");
                ViewData["CurrentMaxDate"] = maxDate?.ToString("yyyy-MM-dd");

                // 6. Pagination & Execution
                var totalCount = await filteredOrders.CountAsync();
                var pagedItems = await filteredOrders
                    .OrderByDescending(o => o.DateTime)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.UserProfile = userProfile;

                return View(new PagedList<Order>(pagedItems, totalCount, pageNumber, pageSize));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "A database error occurred. Your history might be temporarily unavailable.";
                return View(new PagedList<Order>(new List<Order>(), 0, page ?? 1, 10));
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred while loading your order history.";
                return RedirectToAction("Index", "Home");
            }
        }

        // Note: This method should be placed in the appropriate controller (likely OrdersController or CheckoutController).

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create()
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

            // Scenario 1: Empty Cart
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your shopping cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // Scenario 2: Unauthenticated User
            var user = await _signInManager.UserManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            // 1. Safely retrieve the UserProfile for display metadata
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(x => x.UserId == user.Id);

            string fullName = userProfile != null
                ? $"{userProfile.FirstName} {userProfile.LastName}"
                : user.UserName;

            // Use a transaction to ensure stock is only deducted if the order is fully recorded
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 2. Critical Stock Check
                var productIds = cart.Select(item => item.ProductId).Distinct().ToList();
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                var productMap = products.ToDictionary(p => p.Id);
                var stockIssues = new List<string>();

                foreach (var item in cart)
                {
                    if (productMap.TryGetValue(item.ProductId, out var product))
                    {
                        if (item.Quantity > product.StockQuantity)
                        {
                            stockIssues.Add($"{item.ProductName}: Only {product.StockQuantity} left.");
                        }
                    }
                    else
                    {
                        stockIssues.Add($"{item.ProductName} is no longer available.");
                    }
                }

                if (stockIssues.Any())
                {
                    await transaction.RollbackAsync();
                    TempData["Error"] = "Stock levels changed. Please review: <br/>- " + string.Join("<br/>- ", stockIssues);
                    return RedirectToAction("Index", "Cart");
                }

                // 3. Create the Order Header
                Order order = new Order
                {
                    UserId = user.Id, // Link by ID for database stability
                    UserName = fullName,
                    PhoneNumber = user.PhoneNumber ?? "No Phone Provided",
                    GrandTotal = cart.Sum(x => x.Price * x.Quantity),
                    DateTime = DateTime.Now,
                    Shipped = false // Default status
                };

                _context.Add(order);
                await _context.SaveChangesAsync();

                // 4. Process Order Details & Stock Deduction
                foreach (var item in cart)
                {
                    var product = productMap[item.ProductId];

                    // Core Logic: Deduct Stock
                    product.StockQuantity -= item.Quantity;

                    OrderDetail orderDetail = new()
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Image = item.Image,
                        ProductOwner = item.ProductOwner ?? "Store",
                        Customer = user.UserName,
                        CustomerNumber = user.PhoneNumber ?? "N/A",
                        IsProcessed = false // Link for your MyOrders sync logic
                    };

                    _context.Add(orderDetail);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 5. Success
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = $"Order #{order.Id} placed successfully!";

                return RedirectToAction("MyOrders", "Account"); // Redirecting to the list we just built
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // _logger.LogError(ex, "Checkout failure");
                TempData["Error"] = "A system error occurred. Your card was not charged.";
                return RedirectToAction("Index", "Cart");
            }
        }

        public async Task<IActionResult> Invoice(int id)
        {
            // 1. Fetch the Order
            // Using AsNoTracking() as this is a read-only view for an invoice
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // 2. Fetch Order Details (the items inside the order)
            var orderDetails = await _context.OrderDetails
                .AsNoTracking()
                .Where(od => od.OrderId == id)
                .ToListAsync();

            // 3. Fetch UserProfile AND the related Store
            // IMPORTANT: .Include(u => u.Store) is required to access store details in the view
            var userProfile = await _context.UserProfiles
                .AsNoTracking()
                .Include(u => u.User)  // Standard Identity User data
                .Include(u => u.Store) // The new Store model data
                .FirstOrDefaultAsync(u => u.UserId == order.UserId);

            // 4. Populate the ViewModel
            var viewModel = new OrderDetailsViewModel
            {
                Order = order,
                OrderDetails = orderDetails,
                UserProfile = userProfile
            };

            return View(viewModel);
        }
    }
}