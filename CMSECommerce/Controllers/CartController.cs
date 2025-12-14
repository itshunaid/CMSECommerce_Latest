using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Primitives; // Needed for StringValues access

namespace CMSECommerce.Controllers
{
    public class CartController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        // Note: ILogger<CartController> logger is often injected here for real-world logging

        public IActionResult Index()
        {
            List<CartItem> cart = [];
            try
            {
                // 1. Retrieve cart from session (Session Operation)
                cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? [];

                if (cart.Count == 0)
                {
                    return View(new CartViewModel { CartItems = [], GrandTotal = 0 });
                }

                // 2. Get a list of all Product IDs in the cart
                IEnumerable<int> productIds = cart.Select(c => c.ProductId).ToList();

                // 3. Fetch Product Data and Stock Quantity from the database (Database Operation)
                var productData = _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new
                    {
                        p.Id,
                        p.StockQuantity,
                        p.Image,
                        p.Slug
                    })
                    .ToDictionary(p => p.Id);

                // 4. Iterate through the cart items, validate stock, and update metadata
                foreach (var item in cart)
                {
                    if (productData.TryGetValue(item.ProductId, out var productDetails))
                    {
                        item.Image = productDetails.Image;
                        item.ProductSlug = productDetails.Slug;

                        // Stock Check Logic
                        if (item.Quantity > productDetails.StockQuantity)
                        {
                            item.IsOutOfStock = true;
                            // Reset quantity to max stock if exceeding
                            if (productDetails.StockQuantity > 0)
                            {
                                item.Quantity = productDetails.StockQuantity;
                            }
                            else
                            {
                                // If stock is zero, set quantity to 1 for display, but keep IsOutOfStock true
                                // A more robust solution might remove items with zero stock entirely.
                                item.Quantity = 1;
                            }
                            TempData["warning"] = $"Quantity for '{item.ProductName}' adjusted due to low stock.";
                        }
                        else
                        {
                            item.IsOutOfStock = false;
                        }
                    }
                    else
                    {
                        // Product no longer exists in DB
                        item.IsOutOfStock = true;
                    }
                }

                // 5. Update the session cart with the modified items (Session Operation)
                HttpContext.Session.SetJson("Cart", cart);

                // 6. Create and return the ViewModel
                CartViewModel cartVM = new()
                {
                    CartItems = cart,
                    GrandTotal = cart.Sum(x => x.Price * x.Quantity)
                };

                return View(cartVM);
            }
            catch (DbUpdateException dbEx)
            {
                // Log the database error
                // _logger.LogError(dbEx, "Database error while fetching product data for cart Index.");
                TempData["error"] = "We couldn't verify product details due to a database issue. Please try refreshing.";
                return View(new CartViewModel { CartItems = cart, GrandTotal = cart.Sum(x => x.Price * x.Quantity) });
            }
            catch (Exception ex)
            {
                // Log general session or unexpected errors
                // _logger.LogError(ex, "Unexpected error in Cart Index action.");
                TempData["error"] = "An error occurred while loading your shopping cart.";
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
        public async Task<IActionResult> UpdateQuantity(int id, int quantity)
        {
            if (quantity <= 0)
            {
                // Use the existing Remove action if quantity is zero or less
                return await Task.FromResult(Remove(id));
            }

            try
            {
                // 1. Get current cart and item
                List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? [];
                CartItem cartItem = cart.Where(x => x.ProductId == id).FirstOrDefault();

                if (cartItem == null)
                {
                    TempData["error"] = "Item not found in cart.";
                    return RedirectToAction("Index");
                }

                // 2. Validate against stock
                var product = await _context.Products.FindAsync(id);
                if (product != null && quantity > product.StockQuantity)
                {
                    TempData["error"] = $"Cannot set quantity to {quantity}. Only {product.StockQuantity} are in stock for '{cartItem.ProductName}'.";
                    cartItem.Quantity = product.StockQuantity; // Adjust quantity to max stock
                }
                else
                {
                    cartItem.Quantity = quantity; // Apply new quantity
                }

                // 3. Save cart
                HttpContext.Session.SetJson("Cart", cart);
                TempData["success"] = $"Quantity for '{cartItem.ProductName}' updated to {cartItem.Quantity}.";
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error updating cart quantity for product ID {Id}.", id);
                TempData["error"] = "A system error occurred while trying to update the quantity.";
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