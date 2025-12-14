using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize("Subscriber")]
    public class OrdersController(DataContext context, UserManager<IdentityUser> userManager) : Controller
    {
        private readonly DataContext _context = context;
        private readonly UserManager<IdentityUser> _userManager=userManager;

        // Assuming this is within your Seller/OrdersController or similar
        // ... using statements and dependencies

        [HttpGet]
        public async Task<IActionResult> Index(string searchString, string searchField)
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
                // To support case-insensitive contains search in EF Core
                var lowerSearch = searchString.ToLower();

                // Apply filtering based on the selected field
                query = searchField switch
                {
                    "OrderId" => query.Where(d => d.OrderId.ToString().Contains(lowerSearch)),
                    "Subtotal" => query.Where(d => (d.Price * d.Quantity).ToString().Contains(lowerSearch)), // Calculate subtotal for filtering
                    "Price" => query.Where(d => d.Price.ToString().Contains(lowerSearch)),
                    "Qty" => query.Where(d => d.Quantity.ToString().Contains(lowerSearch)),
                    "Contact" => query.Where(d => d.CustomerNumber.ToLower().Contains(lowerSearch)),
                    "Customer" => query.Where(d => d.Customer.ToLower().Contains(lowerSearch)),
                    "Product" => query.Where(d => d.ProductName.ToLower().Contains(lowerSearch)),
                    _ => query // Default case: no specific field filter applied
                };

                // Store search parameters in ViewBag for view persistence
                ViewBag.CurrentSearchString = searchString;
                ViewBag.CurrentSearchField = searchField;
            }

            // Execute the query and order the results
            var orderDetails = await query
                .OrderBy(d => d.OrderId) // Group by order for better viewing
                .ToListAsync();

            return View(orderDetails);
        }
        // Apply similar changes to the Shipped action for consistency
        [HttpGet]
        public async Task<IActionResult> Shipped(string searchString, string searchField)
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
                    "Contact" => query.Where(d => d.CustomerNumber.ToLower().Contains(lowerSearch)),
                    "Customer" => query.Where(d => d.Customer.ToLower().Contains(lowerSearch)),
                    "Product" => query.Where(d => d.ProductName.ToLower().Contains(lowerSearch)),
                    _ => query
                };

                // Store search parameters for view persistence
                ViewBag.CurrentSearchString = searchString;
                ViewBag.CurrentSearchField = searchField;
            }

            var orderDetails = await query
                .OrderBy(d => d.OrderId) // Group by order for better viewing
                .ToListAsync();

            return View("Index", orderDetails);
        }
        // NEW ACTION: To toggle the IsProcessed status via POST
        [HttpPost]
        [ValidateAntiForgeryToken] // Security best practice
        public async Task<IActionResult> ToggleProcessed(int detailId, bool setTo)
        {
            var detail = await _context.OrderDetails.FirstOrDefaultAsync(d => d.Id == detailId);

            if (detail == null)
            {
                TempData["Error"] = "Error: Order detail item not found.";
                return RedirectToAction("Index");
            }

            // Ensure the current user is the owner before processing (security check)
            var userName = _userManager.GetUserName(User);
            if (detail.ProductOwner != userName)
            {
                TempData["Error"] = "Unauthorized action.";
                return RedirectToAction("Index");
            }

            detail.IsProcessed = setTo;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Item '{detail.ProductName}' (Order #{detail.OrderId}) status updated to: {(setTo ? "Processed" : "Pending")}.";

            // Reload the index page
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
            var product = await _context.Products.Where(p => p.OwnerId == userId).FirstOrDefaultAsync();
            var isProductOrdered = product == null;
            var order = await _context.Orders.Where(p => p.UserName == userName).FirstOrDefaultAsync(o => o.Id == id);
            if (isProductOrdered) return View(new CMSECommerce.Models.ViewModels.OrderDetailsViewModel { Order = null, OrderDetails = null });
            if (order == null ) return NotFound();
            var details = await _context.OrderDetails.Where(d => d.OrderId == id && d.ProductId==product.Id).ToListAsync();
            return View(new CMSECommerce.Models.ViewModels.OrderDetailsViewModel { Order = order, OrderDetails = details });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ShippedStatus(int id, bool shipped)
        {
            var userName = _userManager.GetUserName(User);
            Order order = await _context.Orders.Where(p=> p.UserName== userName).FirstOrDefaultAsync(x => x.Id == id);

            order.Shipped = shipped;

            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["success"] = "The order has been modified!";

            return RedirectToAction("Index");
        }
    }
}