using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
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
        public async Task<IActionResult> Index()
        {
            var userName = _userManager.GetUserName(User);
            ViewBag.IsProcessed = false;
            // Get only UNPROCESSED order details that belong to the seller
            var orderDetails = await _context.OrderDetails
                .Where(p => p.ProductOwner == userName && p.IsProcessed == false)
                .OrderBy(d => d.OrderId) // Group by order for better viewing
                .ToListAsync();

            return View(orderDetails);
        }
        [HttpGet]
        public async Task<IActionResult> Shipped()
        {
            var userName = _userManager.GetUserName(User);

            // Get only UNPROCESSED order details that belong to the seller
            var orderDetails = await _context.OrderDetails
                .Where(p => p.ProductOwner == userName && p.IsProcessed == true)
                .OrderBy(d => d.OrderId) // Group by order for better viewing
                .ToListAsync();
            ViewBag.IsProcessed = true;
            return View("Index",orderDetails);
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