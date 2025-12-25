using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Models.ViewModels; // FIX: Added this to resolve OrderDetailsViewModel errors

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    // Using standard Roles authorization, assuming "Subscriber" is a role name
    [Authorize(Roles = "Subscriber")]
    public class OrdersController(
        DataContext context,
        UserManager<IdentityUser> userManager,
        ILogger<OrdersController> logger) : Controller
    {
        private readonly DataContext _context = context;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly ILogger<OrdersController> _logger = logger;

        // GET /seller/orders/index (Unprocessed Orders)
        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string searchField)
        {
            try
            {
                var userName = _userManager.GetUserName(User);
                ViewBag.IsProcessed = false;

                // Start with the base query for UNPROCESSED items belonging to the seller
                var query = _context.OrderDetails
                    .Where(p => p.ProductOwner == userName && p.IsProcessed == false)
                    .AsQueryable();

                // Apply filtering if a search string is provided
                if (!string.IsNullOrEmpty(searchString))
                {
                    var lowerSearch = searchString.ToLower();

                    // Apply filtering based on the selected field
                    query = searchField switch
                    {
                        "OrderId" => query.Where(d => d.OrderId.ToString().Contains(lowerSearch)),
                        "Subtotal" => query.Where(d => (d.Price * d.Quantity).ToString().Contains(lowerSearch)),
                        "Price" => query.Where(d => d.Price.ToString().Contains(lowerSearch)),
                        "Qty" => query.Where(d => d.Quantity.ToString().Contains(lowerSearch)),
                        "Contact" => query.Where(d => EF.Functions.Like(d.CustomerNumber.ToLower(), $"%{lowerSearch}%")),
                        "Customer" => query.Where(d => EF.Functions.Like(d.Customer.ToLower(), $"%{lowerSearch}%")),
                        "Product" => query.Where(d => EF.Functions.Like(d.ProductName.ToLower(), $"%{lowerSearch}%")),
                        _ => query
                    };

                    ViewBag.CurrentSearchString = searchString;
                    ViewBag.CurrentSearchField = searchField;
                }

                // Execute the query and order the results
                var orderDetails = await query
                    .OrderBy(d => d.OrderId)
                    .ToListAsync();

                return View(orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading UNPROCESSED orders for seller {User}.", _userManager.GetUserName(User));
                TempData["Error"] = "Failed to load pending orders. A database error occurred.";
                return View(new List<OrderDetail>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderDetail(int detailId, string reason)
        {
            // 1. Fetch the specific item and its parent Order
            var detail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == detailId);

            if (detail == null)
            {
                TempData["Error"] = "Order item not found.";
                return RedirectToAction(nameof(Index));
            }

            // 2. Update the item status
            detail.IsCancelled = true;
            detail.CancellationReason = reason;
            detail.CancelledByRole = "Seller"; // Identifies the seller as the initiator

            // 3. Logic Check: If ALL items in this order are now cancelled, 
            // mark the main Order header as cancelled too.
            var parentOrder = detail.Order;
            var allItems = await _context.OrderDetails
                .Where(od => od.OrderId == parentOrder.Id)
                .ToListAsync();

            if (allItems.All(od => od.IsCancelled))
            {
                parentOrder.IsCancelled = true;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Item '{detail.ProductName}' has been cancelled.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while cancelling the item.";
            }

            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Cancelled(string searchString, string searchField)
        {
            string currentUserName = _userManager.GetUserName(User);

            // Fetch only cancelled items for this seller
            var cancelledItems = _context.OrderDetails
                .Where(od => od.ProductOwner == currentUserName && od.IsCancelled == true);

            // Apply Search Logic (Matching your existing Index pattern)
            if (!string.IsNullOrEmpty(searchString))
            {
                cancelledItems = searchField switch
                {
                    "Product" => cancelledItems.Where(s => s.ProductName.Contains(searchString)),
                    "Customer" => cancelledItems.Where(s => s.Customer.Contains(searchString)),
                    "OrderId" => cancelledItems.Where(s => s.OrderId.ToString().Contains(searchString)),
                    _ => cancelledItems
                };
            }

            ViewBag.CurrentSearchString = searchString;
            ViewBag.CurrentSearchField = searchField;

            return View(await cancelledItems.OrderByDescending(od => od.Id).ToListAsync());
        }

        // GET /seller/orders/shipped (Processed Orders)
        [HttpGet]
        public async Task<IActionResult> Shipped(string searchString, string searchField)
        {
            try
            {
                var userName = _userManager.GetUserName(User);
                ViewBag.IsProcessed = true;

                // Start with the base query for PROCESSED items belonging to the seller
                var query = _context.OrderDetails
                    .Where(p => p.ProductOwner == userName && p.IsProcessed == true)
                    .AsQueryable();

                // Apply filtering if a search string is provided (same logic as Index)
                if (!string.IsNullOrEmpty(searchString))
                {
                    var lowerSearch = searchString.ToLower();

                    query = searchField switch
                    {
                        "OrderId" => query.Where(d => d.OrderId.ToString().Contains(lowerSearch)),
                        "Subtotal" => query.Where(d => (d.Price * d.Quantity).ToString().Contains(lowerSearch)),
                        "Price" => query.Where(d => d.Price.ToString().Contains(lowerSearch)),
                        "Qty" => query.Where(d => d.Quantity.ToString().Contains(lowerSearch)),
                        "Contact" => query.Where(d => EF.Functions.Like(d.CustomerNumber.ToLower(), $"%{lowerSearch}%")),
                        "Customer" => query.Where(d => EF.Functions.Like(d.Customer.ToLower(), $"%{lowerSearch}%")),
                        "Product" => query.Where(d => EF.Functions.Like(d.ProductName.ToLower(), $"%{lowerSearch}%")),
                        _ => query
                    };

                    ViewBag.CurrentSearchString = searchString;
                    ViewBag.CurrentSearchField = searchField;
                }

                var orderDetails = await query
                    .OrderBy(d => d.OrderId)
                    .ToListAsync();
                

                return View("Index", orderDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading PROCESSED orders for seller {User}.", _userManager.GetUserName(User));
                TempData["Error"] = "Failed to load shipped orders. A database error occurred.";
                return View("Index", new List<OrderDetail>());
            }
        }

        // POST /seller/orders/ToggleProcessed
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleProcessed(int detailId, bool setTo)
        {
            var userName = _userManager.GetUserName(User);
            var detail = await _context.OrderDetails.FirstOrDefaultAsync(d => d.Id == detailId);

            if (detail == null)
            {
                TempData["Error"] = "Error: Order detail item not found.";
                return RedirectToAction("Index");
            }

            // Security Check: Ensure the current user is the owner
            if (detail.ProductOwner != userName)
            {
                _logger.LogWarning("Unauthorized attempt to toggle order detail ID {DetailId} by user {User}.", detailId, userName);
                TempData["Error"] = "Unauthorized action: You do not own this order item.";
                return RedirectToAction("Index");
            }

            try
            {
                detail.IsProcessed = setTo;
                if(setTo)
                {
                    detail.IsReturned = false;
                }
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Item '{detail.ProductName}' (Order #{detail.OrderId}) status updated to: {(setTo ? "Processed" : "Pending")}.";
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Concurrency error processing order detail ID: {DetailId} by user {User}.", detailId, userName);
                TempData["Error"] = "Concurrency error: The item was modified by another process. Please re-load and try again.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error processing order detail ID: {DetailId} by user {User}.", detailId, userName);
                TempData["Error"] = "A database error occurred while updating the status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing order detail ID: {DetailId} by user {User}.", detailId, userName);
                TempData["Error"] = "An unexpected error occurred during status update.";
            }

            return RedirectToAction("Index");
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userName = _userManager.GetUserName(User);
            var userId = _userManager.GetUserId(User);

            try
            {
                var product = await _context.Products.Where(p => p.OwnerId == userId).FirstOrDefaultAsync();

                if (product == null)
                {
                    TempData["Warning"] = "You have no products listed, so no related order details can be shown.";
                    return View(new OrderDetailsViewModel { Order = null, OrderDetails = new List<OrderDetail>() });
                }

                // Check if the requested Order ID exists and contains items owned by the seller
                var details = await _context.OrderDetails
                    .Where(d => d.OrderId == id && d.ProductOwner == userName)
                    .ToListAsync();

                if (!details.Any())
                {
                    TempData["Error"] = "Order not found or you do not have permission to view this order's items.";
                    return NotFound();
                }

                // Retrieve the main order header using the ID
                var order = await _context.Orders.FindAsync(id);

                if (order == null)
                {
                    _logger.LogError("Order header ID {OrderId} found in details but missing in Orders table.", id);
                    TempData["Error"] = "Order header information is missing.";
                    return NotFound();
                }

                return View(new OrderDetailsViewModel { Order = order, OrderDetails = details });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Order Details for Order ID: {OrderId} by seller {User}.", id, userName);
                TempData["Error"] = "An error occurred while loading the order details.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShippedStatus(int id, bool shipped)
        {
            var userName = _userManager.GetUserName(User);

            try
            {
                Order order = await _context.Orders.FindAsync(id);

                if (order == null)
                {
                    TempData["Error"] = "Order header not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Security check: Verify the seller owns items in this order before modifying the header status
                bool sellerHasItemsInOrder = await _context.OrderDetails.AnyAsync(d => d.OrderId == id && d.ProductOwner == userName);

                if (!sellerHasItemsInOrder)
                {
                    _logger.LogWarning("Unauthorized attempt to change ShippedStatus for Order ID {OrderId} by user {User}.", id, userName);
                    TempData["Error"] = "Unauthorized action: You do not own items in this order.";
                    return RedirectToAction(nameof(Index));
                }

                // Update the Shipped status on the Order header
                order.Shipped = shipped;

                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"The order (ID: {id}) overall Shipped status has been modified to: {shipped}!";
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Concurrency error updating ShippedStatus for Order ID: {OrderId} by user {User}.", id, userName);
                TempData["Error"] = "Concurrency error: The order was modified by another process. Please re-load and try again.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error updating ShippedStatus for Order ID: {OrderId} by user {User}.", id, userName);
                TempData["Error"] = "A database error occurred while updating the order status.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating ShippedStatus for Order ID: {OrderId} by user {User}.", id, userName);
                TempData["Error"] = "An unexpected error occurred during status update.";
            }

            return RedirectToAction("Index");
        }
    }
}