using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CMSECommerce.Controllers
{
    // Injecting dependencies in the primary constructor (C#12 feature)
    public class OrdersController(
                                 DataContext context,
                                 UserManager<IdentityUser> userManager,
                                 SignInManager<IdentityUser> signInManager,
                                 IEmailService emailService,
                                 IAuditService auditService
                                 /*, ILogger<OrdersController> logger */) : BaseController
    {
        private readonly DataContext _context = context;
        private readonly SignInManager<IdentityUser> _signInManager = signInManager;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IEmailService _emailService = emailService;
        private readonly IAuditService _auditService = auditService;
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

            // UPDATED: Changed pageSize from 10 to 5
            const int pageSize = 3;
            int pageNumber = page ?? 1;

            try
            {
                // 2. Identity Handling
                var userId = _userManager.GetUserId(User);

                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                // 3. Status Synchronization Logic
                var ordersToCheck = await _context.Orders
                    .Where(o => o.UserId == userId && !o.Shipped && !o.IsCancelled)
                    .Select(o => new
                    {
                        Order = o,
                        AllItemsProcessed = o.OrderDetails.All(od => od.IsProcessed || od.IsCancelled),
                        AllItemsCancelled = o.OrderDetails.All(od => od.IsCancelled)
                    })
                    .ToListAsync();

                bool hasChanges = false;
                foreach (var item in ordersToCheck)
                {
                    if (item.AllItemsCancelled && !item.Order.IsCancelled)
                    {
                        item.Order.IsCancelled = true;
                        _context.Update(item.Order);
                        hasChanges = true;
                    }
                    else if (item.AllItemsProcessed && !item.Order.Shipped)
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

                // 4. Build Filtered Query
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
                        filteredOrders = filteredOrders.Where(o => o.Shipped && !o.IsCancelled);

                    else if (status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                        filteredOrders = filteredOrders.Where(o => !o.Shipped && !o.IsCancelled);

                    else if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                        filteredOrders = filteredOrders.Where(o => o.IsCancelled);
                }

                if (minTotal.HasValue) filteredOrders = filteredOrders.Where(o => o.GrandTotal >= minTotal.Value);
                if (maxTotal.HasValue) filteredOrders = filteredOrders.Where(o => o.GrandTotal <= maxTotal.Value);

                if (minDate.HasValue) filteredOrders = filteredOrders.Where(o => o.OrderDate.Value.Date >= minDate.Value.Date);
                if (maxDate.HasValue) filteredOrders = filteredOrders.Where(o => o.OrderDate.Value.Date <= maxDate.Value.Date);

                // 5. Store UI State
                ViewData["CurrentOrderId"] = orderId;
                ViewData["CurrentStatus"] = status;
                ViewData["CurrentMinTotal"] = minTotal?.ToString();
                ViewData["CurrentMaxTotal"] = maxTotal?.ToString();
                ViewData["CurrentMinDate"] = minDate?.ToString("yyyy-MM-dd");
                ViewData["CurrentMaxDate"] = maxDate?.ToString("yyyy-MM-dd");

                // 6. Pagination & Execution (Uses Updated pageSize)
                var totalCount = await filteredOrders.CountAsync();
                var pagedItems = await filteredOrders
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.UserProfile = userProfile;

                return View(new PagedList<Order>(pagedItems, totalCount, pageNumber, pageSize));
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "A database error occurred. Your history might be temporarily unavailable.";
                // Resetting to updated pageSize here as well
                return View(new PagedList<Order>(new List<Order>(), 0, pageNumber, pageSize));
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred while loading your order history.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string reason)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }

            // --- 24 Hour Logic ---
            var timeElapsed = DateTime.Now - order.OrderDate.Value;
            if (timeElapsed.TotalHours > 24)
            {
                TempData["Error"] = "Orders can only be cancelled within 24 hours of placement.";
                return RedirectToAction("MyOrders");
            }

            if (order.Shipped)
            {
                TempData["Error"] = "Shipped orders cannot be cancelled.";
                return RedirectToAction("MyOrders");
            }

            try
            {
                order.IsCancelled = true;
                foreach (var item in order.OrderDetails)
                {
                    item.IsCancelled = true;
                    item.CancellationReason = reason ?? "Customer requested";
                    item.CancelledByRole = "Customer";
                }

                _context.Update(order);
                await _context.SaveChangesAsync();

                // Audit logging
                await _auditService.LogActionAsync("Cancel Order", "Order", orderId.ToString(), $"Order cancelled by customer. Reason: {reason}", HttpContext);

                TempData["Success"] = $"Order #{orderId} cancelled successfully.";
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred during cancellation.";
            }

            return RedirectToAction("MyOrders");
        }

     

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create()
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

            // Scenario1: Empty Cart
            if (cart == null || !cart.Any())
            {
                TempData["Error"] = "Your shopping cart is empty.";
                return RedirectToAction("Index", "Cart");
            }

            // Scenario2: Unauthenticated User
            var user = await _signInManager.UserManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["Error"] = "You must be logged in to place an order.";
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            //1. Safely retrieve the UserProfile for display metadata
            // Attempt to find existing profile data
            var userProfile = await _context.UserProfiles
                .Include(p => p.Store)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if(userProfile == null)
            {
                return RedirectToAction("Create", "UserProfiles",new {isNewProfile=true });
            }



            string fullName = userProfile != null
                ? $"{userProfile.FirstName} {userProfile.LastName}"
                : user.UserName;

            // Use a transaction to ensure stock is only deducted if the order is fully recorded
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                //2. Critical Stock Check
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

                //3. Create the Order Header
                Order order = new Order
                {
                    UserId = user.Id, // Link by ID for database stability
                    UserName = fullName,
                    PhoneNumber = user.PhoneNumber ?? "No Phone Provided",
                    GrandTotal = cart.Sum(x => x.Price * x.Quantity),
                    OrderDate = DateTime.Now,
                    Shipped = false // Default status
                };

                _context.Add(order);
                await _context.SaveChangesAsync();

                //4. Process Order Details & Stock Deduction
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

                //5. Send notification emails
                try
                {
                    // Get the complete order with details for email
                    var completeOrder = await _context.Orders
                        .Include(o => o.OrderDetails)
                        .FirstOrDefaultAsync(o => o.Id == order.Id);

                    if (completeOrder != null)
                    {
                        // Send email to product owners
                        var productOwners = completeOrder.OrderDetails
                            .Select(od => od.ProductOwner)
                            .Distinct()
                            .Where(po => !string.IsNullOrEmpty(po))
                            .ToList();

                        foreach (var ownerId in productOwners)
                        {
                            var ownerProfile = await _context.UserProfiles
                                .Include(p => p.Store)
                                .FirstOrDefaultAsync(p => p.UserId == ownerId);

                            if (ownerProfile != null && !string.IsNullOrEmpty(ownerProfile.User.Email))
                            {
                                var ownerItems = completeOrder.OrderDetails
                                    .Where(od => od.ProductOwner == ownerId)
                                    .ToList();

                                var itemsList = string.Join("\n", ownerItems.Select(item =>
                                    $"- {item.ProductName} (Qty: {item.Quantity}) - ${item.Price * item.Quantity:F2}"));

                                var subject = $"New Order #{completeOrder.Id} - Products Sold";
                                var body = $@"
<h2>New Order Notification</h2>
<p>Dear {ownerProfile.FirstName} {ownerProfile.LastName},</p>
<p>A new order has been placed containing your products.</p>
<p><strong>Order Details:</strong></p>
<p>Order ID: #{completeOrder.Id}</p>
<p>Customer: {completeOrder.UserName}</p>
<p>Order Date: {completeOrder.OrderDate:yyyy-MM-dd HH:mm}</p>
<p><strong>Items Sold:</strong></p>
<pre>{itemsList}</pre>
<p><strong>Total for your items: ₹{ownerItems.Sum(item => item.Price * item.Quantity):F2}</strong></p>
<p>Please process this order promptly.</p>
<br>
<p>Best regards,<br>Weypaari Team</p>";

                                await _emailService.SendEmailAsync(ownerProfile.User.Email, subject, body);
                            }
                        }

                        // Send order confirmation email to customer
                        var customerSubject = $"Order Confirmation - Order #{completeOrder.Id}";
                        var customerBody = $@"
<h2>Order Confirmation</h2>
<p>Dear {completeOrder.UserName},</p>
<p>Thank you for your order! Your order has been successfully placed.</p>
<p><strong>Order Details:</strong></p>
<p>Order ID: #{completeOrder.Id}</p>
<p>Order Date: {completeOrder.OrderDate:yyyy-MM-dd HH:mm}</p>
<p>Total Amount: ₹{completeOrder.GrandTotal:F2}</p>
<p><strong>Items Ordered:</strong></p>
{string.Join("", completeOrder.OrderDetails.Select(item => $"<p>- {item.ProductName} (Qty: {item.Quantity}) - ${item.Price * item.Quantity:F2}</p>"))}
<p>You can track your order status in your account under 'My Orders'.</p>
<p>If you have any questions, please contact our support team.</p>
<br>
<p>Best regards,<br>Weypaari Team</p>";

                        await _emailService.SendEmailAsync(user.Email, customerSubject, customerBody);
                    }
                }
                catch (Exception emailEx)
                {
                    // Log email failure but don't fail the order
                    // _logger.LogError(emailEx, "Failed to send order notification emails for order {OrderId}", order.Id);
                }

                //6. Success
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = $"Order #{order.Id} placed successfully!";

                return RedirectToAction("MyOrders", "Orders"); // Redirecting to the list we just built
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
            try
            {
                //1. Fetch the Order and its Line Items in a single optimized query
                // Using .Include(o => o.OrderDetails) reduces database round-trips
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                //2. Fetch Buyer Profile (for billing details)
                var buyerProfile = await _context.UserProfiles
                    .Include(u => u.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == order.UserId);

                //3. Fetch unique ProductOwners (sellers) from OrderDetails
                var productOwners = order.OrderDetails
                    .Select(od => od.ProductOwner)
                    .Distinct()
                    .Where(po => !string.IsNullOrEmpty(po))
                    .ToList();

                //4. Fetch Seller Profiles including their Stores
                var sellerProfiles = await _context.UserProfiles
                    .Include(u => u.Store)
                    .AsNoTracking()
                    .Where(u => productOwners.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u);

                //5. Null-Safety for Invoice Rendering
                // Ensure that even if profiles/stores are missing, the invoice doesn't crash
                if (buyerProfile == null)
                {
                    buyerProfile = new UserProfile
                    {
                        UserId = order.UserId,
                        FirstName = "Guest",
                        LastName = "User"
                    };
                }

                //6. Map to the ViewModel
                var viewModel = new OrderDetailsViewModel
                {
                    Order = order,
                    OrderDetails = order.OrderDetails?.ToList() ?? new List<OrderDetail>(),
                    UserProfile = buyerProfile,
                    SellerProfiles = sellerProfiles
                };

                //7. Return to the Invoice view
                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log error if logger is available
                TempData["error"] = "An error occurred while generating the invoice.";
                return RedirectToAction("Index", "Orders");
            }
        }

        // Printable invoice view that renders a minimal page suitable for browser Print -> Save as PDF
        public async Task<IActionResult> Printable(int id, bool autoPrint = true)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound();
                }

                var buyerProfile = await _context.UserProfiles
                    .Include(u => u.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == order.UserId);

                var productOwners = order.OrderDetails
                    .Select(od => od.ProductOwner)
                    .Distinct()
                    .Where(po => !string.IsNullOrEmpty(po))
                    .ToList();

                var sellerProfiles = await _context.UserProfiles
                    .Include(u => u.Store)
                    .AsNoTracking()
                    .Where(u => productOwners.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u);

                if (buyerProfile == null)
                {
                    buyerProfile = new UserProfile
                    {
                        UserId = order.UserId,
                        FirstName = "Guest",
                        LastName = "User"
                    };
                }

                var viewModel = new OrderDetailsViewModel
                {
                    Order = order,
                    OrderDetails = order.OrderDetails?.ToList() ?? new List<OrderDetail>(),
                    UserProfile = buyerProfile,
                    SellerProfiles = sellerProfiles
                };

                ViewData["AutoPrint"] = autoPrint;
                return View("InvoicePrintable", viewModel);
            }
            catch
            {
                TempData["Error"] = "Unable to prepare printable invoice.";
                return RedirectToAction("Invoice", new { id });
            }
        }
    }
}