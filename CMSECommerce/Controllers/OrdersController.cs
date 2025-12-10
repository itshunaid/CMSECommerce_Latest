using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CMSECommerce.Controllers
{
    public class OrdersController(
                        DataContext context,
                         UserManager<IdentityUser> userManager,
                        SignInManager<IdentityUser> signInManager) : Controller
    {
        private readonly DataContext _context = context;
        private SignInManager<IdentityUser> _signInManager = signInManager;
        private UserManager<IdentityUser> _userManager = userManager;

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
            var userName = _userManager.GetUserName(User);
            // 1. Setup Pagination and Filtering Defaults
            int pageSize = 10;
            int pageNumber = page ?? 1; // Default to page 1

            // The current 'username' variable is assumed to hold the case-insensitive username to match.
            string usernameLower = userName.ToLower();

          

            // Start with all orders for the current user
            var orders = _context.Orders.OrderByDescending(x => x.Id).Where(o=> o.UserName.ToLower().Contains(usernameLower)).AsQueryable();

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

            // Fetch current user details
            var user = await _signInManager.UserManager.GetUserAsync(User);
            // 1. Safely retrieve the UserProfile object
            var userProfile = await _context.UserProfiles
                .Where(x => x.UserId == user.Id)
                .FirstOrDefaultAsync();

            // 2. Check if the profile exists and concatenate the names
            string fullName = userProfile != null
                ? userProfile.FirstName + " " + userProfile.LastName
                : "Unknown User"; // Handle the null case

            // Scenario 2: Unauthenticated User (should be rare if [Authorize] is used, but safe to check)
            if (user == null)
            {
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction("Login", "Account"); // Redirect to login page
            }

            try
            {
                // --- 🛠️ Final Stock Check and Product Fetching ---

                // Get IDs of all products in the cart
                var productIds = cart.Select(item => item.ProductId).ToList();

                // Fetch all required Product entities from the database, tracking them for update
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
                        stockIssues.Add($"Product ID {item.ProductId} ({item.ProductName}) is no longer available in the store.");
                    }
                }

                if (stockIssues.Any())
                {
                    // Abort the transaction and notify the user of specific shortages
                    TempData["Error"] = "Order failed due to critical stock shortages. Please review and adjust the following items: <br/>- " + string.Join("<br/>- ", stockIssues);
                    return RedirectToAction("Index", "Cart");
                }


                // --- If all checks pass, proceed with Order Transaction ---

                // 1. Create the Order header
                Order order = new Order
                {
                    UserName = fullName+" ("+User.Identity.Name+")",
                    PhoneNumber = user.PhoneNumber,
                    GrandTotal = cart.Sum(x => x.Price * x.Quantity)
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
                    _context.Update(product); // Mark the product as modified

                    // Create the OrderDetail entry
                    OrderDetail orderDetail = new()
                    {
                        OrderId = orderId,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Image = item.Image,
                        ProductOwner = item.ProductOwner,
                        Customer = user.UserName,
                        CustomerNumber = user.PhoneNumber
                    };

                    _context.Add(orderDetail);
                }

                // 3. Save all Order Details and Stock Quantity updates
                // All changes (OrderDetail inserts and Product updates) are saved in one batch.
                await _context.SaveChangesAsync();

                // 4. Success: Clear the cart and notify the user
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = $"Your order (ID: {orderId}) has been placed successfully!";
                ViewBag.OrderId = orderId;
                if (ViewBag.OrderId != 0)
                {
                    return RedirectToAction("OrderDetails", "Account", new { id = orderId });
                }
                return RedirectToAction("Index", "Orders"); // Redirect to the user's order history/details
            }
            // Scenario 4: Database or General Exception Error
            catch (Exception ex)
            {
                // Log the detailed exception (e.g., using ILogger)
                // _logger.LogError(ex, "Error placing order for user {Username}", User.Identity.Name);

                TempData["Error"] = "An unexpected error occurred while processing your order. Please try again or contact support.";
                return RedirectToAction("Index", "Cart"); // Redirect back to cart or an error page
            }
        }
    }
}
