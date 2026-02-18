using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CMSECommerce.Controllers
{
    public class CartController(DataContext context, ILogger<CartController> logger, IAuditService auditService) : BaseController
    {
        private readonly DataContext _context = context;
        private readonly ILogger<CartController> _logger = logger;
        private readonly IAuditService _auditService = auditService;

        public async Task<IActionResult> Index()
        {
            try
            {
                var cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

                if (cart.Count == 0)
                {
                    return View(new CartViewModel { CartItems = new List<CartItem>(), GrandTotal = 0 });
                }

                var productIds = cart.Select(c => c.ProductId).Distinct().ToList();

                var productLookup = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new
                    {
                        p.Id,
                        p.StockQuantity,
                        p.Image,
                        p.Slug,
                        DisplayName = _context.UserProfiles
                            .Where(up => up.UserId == p.UserId)
                            .Select(up => up.Store.StoreName)
                            .FirstOrDefault() ?? p.User.UserName
                    })
                    .ToDictionaryAsync(x => x.Id);

                foreach (var item in cart)
                {
                    if (productLookup.TryGetValue(item.ProductId, out var dbProduct))
                    {
                        item.Image = dbProduct.Image;
                        item.ProductSlug = dbProduct.Slug;
                        item.SellerName = dbProduct.DisplayName;

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
                        item.IsOutOfStock = true;
                    }
                }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            if (quantity <= 0) quantity = 1;

            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(id);
            }
            catch (Exception dbEx)
            {
                _logger.LogError(dbEx, "Database error while looking up product ID {Id} for cart Add.", id);
                TempData["error"] = "Failed to retrieve product details. Please try again.";
                string safeReferer = Request.Headers.Referer.FirstOrDefault() ?? "/";
                return Redirect(safeReferer);
            }

            if (product == null) { return NotFound(); }

            // Prevent adding items that are completely out of stock
            if (product.StockQuantity <= 0)
            {
                TempData["error"] = "This product is currently out of stock and cannot be added to the cart.";
                var referer = Request.Headers.Referer.FirstOrDefault() ?? "/";
                return Redirect(referer);
            }

            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
                CartItem cartItem = cart.FirstOrDefault(x => x.ProductId == id);

                if (cartItem == null)
                {
                    var item = new CartItem(product);
                    item.Quantity = Math.Min(Math.Max(1, quantity), product.StockQuantity > 0 ? product.StockQuantity : 1);
                    cart.Add(item);
                }
                else
                {
                    // increase quantity but cap at stock
                    var desired = cartItem.Quantity + quantity;
                    if (product.StockQuantity > 0)
                    {
                        cartItem.Quantity = Math.Min(desired, product.StockQuantity);
                    }
                    else
                    {
                        cartItem.Quantity = 1;
                        cartItem.IsOutOfStock = true;
                    }
                }

                HttpContext.Session.SetJson("Cart", cart);
                TempData["success"] = "The product has been added!";

                await _auditService.LogActionAsync("AddToCart", "Cart", id.ToString(), $"Added product {product.Name} to cart", HttpContext);
            }
            catch (Exception sessionEx)
            {
                _logger.LogError(sessionEx, "Session error while adding product ID {Id} to cart.", id);
                TempData["error"] = "An error occurred while saving the cart data.";
            }

            string finalReferer = Request.Headers.Referer.FirstOrDefault() ?? "/";
            return Redirect(finalReferer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                return await Remove(id);
            }

            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
                CartItem cartItem = cart.FirstOrDefault(x => x.ProductId == id);

                if (cartItem == null)
                {
                    TempData["error"] = "Item not found in cart.";
                    return RedirectToAction("Index");
                }

                var productInfo = await _context.Products
                    .Where(p => p.Id == id)
                    .Select(p => new { p.StockQuantity, p.Name })
                    .FirstOrDefaultAsync();

                if (productInfo != null)
                {
                    if (quantity > productInfo.StockQuantity)
                    {
                        cartItem.Quantity = productInfo.StockQuantity;
                        cartItem.IsOutOfStock = productInfo.StockQuantity == 0;

                        TempData["warning"] = $"Only {productInfo.StockQuantity} units available for '{productInfo.Name}'. Quantity adjusted.";
                    }
                    else
                    {
                        cartItem.Quantity = quantity;
                        cartItem.IsOutOfStock = false;
                        TempData["success"] = $"Updated '{cartItem.ProductName}' quantity to {quantity}.";

                        await _auditService.LogActionAsync("UpdateQuantity", "Cart", id.ToString(), $"Updated quantity to {quantity}", HttpContext);
                    }
                }
                else
                {
                    TempData["error"] = "This product is no longer available.";
                    return await Remove(id);
                }

                HttpContext.Session.SetJson("Cart", cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity for product ID {Id}.", id);
                TempData["error"] = "An error occurred while updating the quantity.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int id)
        {
            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

                if (cart == null || cart.Count == 0)
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
                _logger.LogError(ex, "Session error in Cart Decrease action for product ID {Id}.", id);
                TempData["error"] = "An error occurred while decreasing the item quantity.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            try
            {
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

                if (cart == null || cart.Count == 0)
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

                await _auditService.LogActionAsync("RemoveFromCart", "Cart", id.ToString(), "Removed product from cart", HttpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session error in Cart Remove action for product ID {Id}.", id);
                TempData["error"] = "An error occurred while removing the item.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                HttpContext.Session.Remove("Cart");
                TempData["success"] = "Your cart has been cleared!";

                await _auditService.LogActionAsync("ClearCart", "Cart", null, "Cleared entire cart", HttpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Session error in Cart Clear action.");
                TempData["error"] = "An error occurred while clearing the cart.";
            }

            string refererUrl = Request.Headers.Referer.FirstOrDefault() ?? "/";
            return Redirect(refererUrl);
        }
    }
}