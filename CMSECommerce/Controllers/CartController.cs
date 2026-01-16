using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Controllers
{
    public class CartController(DataContext context, ILogger<CartController> logger, ICartService cartService) : BaseController
    {
        private readonly DataContext _context = context;
        private readonly ILogger<CartController> _logger= logger; // 1. Add this field
        private readonly ICartService _cartService = cartService;

        // Note: ILogger<CartController> logger is often injected here for real-world logging

        public async Task<IActionResult> Index()
        {
            try
            {
                var cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

                if (cart.Count == 0)
                {
                    return View(new CartViewModel { CartItems = [], GrandTotal = 0 });
                }

                var productIds = cart.Select(c => c.ProductId).Distinct().ToList();

                // 1. Fetch data into a lightweight dictionary
                var productLookup = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new
                    {
                        p.Id,
                        p.StockQuantity,
                        p.Image,
                        p.Slug,
                        // Flatten the Store Name or Username right here in the query
                        DisplayName = _context.UserProfiles
                            .Where(up => up.UserId == p.UserId)
                            .Select(up => up.Store.StoreName)
                            .FirstOrDefault() ?? p.User.UserName
                    })
                    .ToDictionaryAsync(x => x.Id);

                // 2. Map data to the cart items
                foreach (var item in cart)
                {
                    if (productLookup.TryGetValue(item.ProductId, out var dbProduct))
                    {
                        item.Image = dbProduct.Image;
                        item.ProductSlug = dbProduct.Slug;
                        item.SellerName = dbProduct.DisplayName; // Simple string mapping

                        // Handle stock levels
                        if (item.Quantity > dbProduct.StockQuantity)
                        {
                            item.IsOutOfStock = true;
                            item.Quantity = Math.Max(1, dbProduct.StockQuantity);
                            TempData["warning"] = $"Quantity for '{item.ProductName}' adjusted due to stock.";
                        }
                        else
                        {
                            item.IsOutOfStock = dbProduct.StockQuantity <= 0;
                        }
                    }
                    else
                    {
                        item.IsOutOfStock = true; // Product deleted from DB
                    }
                }

                // 3. Save clean strings to Session
                HttpContext.Session.SetJson("Cart", cart);

                return View(new CartViewModel
                {
                    CartItems = cart,
                    GrandTotal = cart.Sum(x => x.Price * x.Quantity)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering shopping cart.");
                TempData["error"] = "An error occurred. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> Add(int id)
        {
            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(id);
            }
            catch (Exception dbEx)
            {
                // _logger.LogError(dbEx, "Database error while looking up product ID {Id} for cart Add.", id);
                TempData["error"] = "Failed to retrieve product details. Please try again.";
                string safeReferer = Request.Headers.Referer.FirstOrDefault() ?? "/";
                return Redirect(safeReferer);
            }

            if (product == null) { return NotFound(); }

            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? [];
                CartItem cartItem = cart.Where(x => x.ProductId == id).FirstOrDefault();

                if (cartItem == null)
                {
                    cart.Add(new CartItem(product));
                }
                else
                {
                    cartItem.Quantity += 1;
                }

                HttpContext.Session.SetJson("Cart", cart);
                TempData["success"] = "The product has been added!";
            }
            catch (Exception sessionEx)
            {
                // _logger.LogError(sessionEx, "Session error while adding product ID {Id} to cart.", id);
                TempData["error"] = "An error occurred while saving the cart data.";
            }

            string finalReferer = Request.Headers.Referer.FirstOrDefault() ?? "/";
            return Redirect(finalReferer);
        }

        // --- NEW ACTION: Handle Quantity Change from Amazon-style Dropdown ---
        [HttpPost]
        [ValidateAntiForgeryToken] // Added for security
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            // 1. If quantity is 0 or less, remove the item entirely
            if (quantity <= 0)
            {
                return Remove(id);
            }

            try
            {
                // 2. Retrieve current cart
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? [];
                CartItem cartItem = cart.FirstOrDefault(x => x.ProductId == id);

                if (cartItem == null)
                {
                    TempData["error"] = "Item not found in cart.";
                    return RedirectToAction("Index");
                }

                // 3. Optimized Stock Check: Fetch only what is necessary
                var productInfo = await _context.Products
                    .Where(p => p.Id == id)
                    .Select(p => new { p.StockQuantity, p.Name })
                    .FirstOrDefaultAsync();

                if (productInfo != null)
                {
                    if (quantity > productInfo.StockQuantity)
                    {
                        // Cap the quantity at available stock
                        cartItem.Quantity = productInfo.StockQuantity;
                        cartItem.IsOutOfStock = productInfo.StockQuantity == 0;

                        TempData["warning"] = $"Only {productInfo.StockQuantity} units available for '{productInfo.Name}'. Quantity adjusted.";
                    }
                    else
                    {
                        cartItem.Quantity = quantity;
                        cartItem.IsOutOfStock = false;
                        TempData["success"] = $"Updated '{cartItem.ProductName}' quantity to {quantity}.";
                    }
                }
                else
                {
                    // Product no longer exists in Database
                    TempData["error"] = "This product is no longer available.";
                    return Remove(id);
                }

                // 4. Save the updated cart to session
                HttpContext.Session.SetJson("Cart", cart);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error updating cart quantity for product ID {Id}.", id);
                TempData["error"] = "An error occurred while updating the quantity.";
            }

            return RedirectToAction("Index");
        }


        public IActionResult Decrease(int id)
        {
            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

                if (cart == null)
                {
                    TempData["info"] = "Your cart is empty.";
                    return RedirectToAction("Index");
                }

                CartItem cartItem = cart.Where(x => x.ProductId == id).FirstOrDefault();

                if (cartItem != null)
                {
                    if (cartItem.Quantity > 1)
                    {
                        --cartItem.Quantity;
                    }
                    else
                    {
                        cart.RemoveAll(x => x.ProductId == id);
                    }
                }

                if (cart.Count == 0)
                {
                    HttpContext.Session.Remove("Cart");
                }
                else
                {
                    HttpContext.Session.SetJson("Cart", cart);
                }

                TempData["success"] = "The product quantity has been updated!";
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Session error in Cart Decrease action for product ID {Id}.", id);
                TempData["error"] = "An error occurred while decreasing the item quantity.";
            }

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

                if (cart == null)
                {
                    TempData["info"] = "Your cart is already empty.";
                    return RedirectToAction("Index");
                }

                cart.RemoveAll(x => x.ProductId == id);

                if (cart.Count == 0)
                {
                    HttpContext.Session.Remove("Cart");
                }
                else
                {
                    HttpContext.Session.SetJson("Cart", cart);
                }

                TempData["success"] = "The product has been removed!";
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Session error in Cart Remove action for product ID {Id}.", id);
                TempData["error"] = "An error occurred while removing the item.";
            }

            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            try
            {
                HttpContext.Session.Remove("Cart");
                TempData["success"] = "Your cart has been cleared!";
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Session error in Cart Clear action.");
                TempData["error"] = "An error occurred while clearing the cart.";
            }

            // FIX: Safely access Referer header
            string refererUrl = Request.Headers.Referer.FirstOrDefault() ?? "/";
            return Redirect(refererUrl);
        }
    }
}