using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CMSECommerce.Controllers
{
    public class CartController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        // ⚠️ Assuming your controller has DataContext injected and stored in _context
        // public DataContext _context;

        public IActionResult Index()
        {
            // 1. Retrieve cart from session
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? [];

            if (cart.Count == 0)
            {
                // Handle empty cart early
                return View(new CartViewModel { CartItems = [], GrandTotal = 0 });
            }

            // 2. Get a list of all Product IDs in the cart
            IEnumerable<int> productIds = cart.Select(c => c.ProductId).ToList();

            // 3. Fetch Product Data and Stock Quantity from the database in one query
            //    We fetch Id, StockQuantity, Image, and Slug.
            var productData = _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.StockQuantity,
                    p.Image,     // <-- ADDED: Product Image
                    p.Slug       // <-- ADDED: Product Slug
                })
                .ToDictionary(p => p.Id); // Dictionary for fast lookup

            // 4. Iterate through the cart items, validate stock, and update metadata
            foreach (var item in cart)
            {
                if (productData.TryGetValue(item.ProductId, out var productDetails))
                {
                    // Update CartItem metadata (Slug and Image)
                    item.Image = productDetails.Image;     // <-- UPDATED
                    item.ProductSlug = productDetails.Slug; // <-- UPDATED

                    // Stock Check Logic
                    if (item.Quantity > productDetails.StockQuantity)
                    {
                        item.IsOutOfStock = true;
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
                    // Optionally remove the item, but setting IsOutOfStock is safer for UX
                }
            }

            // 5. Update the session cart with the modified items (including metadata and IsOutOfStock status)
            HttpContext.Session.SetJson("Cart", cart);

            // 6. Create and return the ViewModel
            CartViewModel cartVM = new()
            {
                CartItems = cart,
                GrandTotal = cart.Sum(x => x.Price * x.Quantity)
            };

            return View(cartVM);
        }

        public async Task<IActionResult> Add(int id)
        {
            Product product = await _context.Products.FindAsync(id);

            if (product == null) { return NotFound(); }

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

            return Redirect(Request.Headers.Referer.ToString());
        }

        public IActionResult Decrease(int id)
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

            CartItem cartItem = cart.Where(x => x.ProductId == id).FirstOrDefault();

            if (cartItem.Quantity > 1)
            {
                --cartItem.Quantity;
            }
            else
            {
                cart.RemoveAll(x => x.ProductId == id);
            }

            if (cart.Count == 0)
            {
                HttpContext.Session.Remove("Cart");
            }
            else
            {
                HttpContext.Session.SetJson("Cart", cart);

            }

            TempData["success"] = "The product has been removed!";

            return RedirectToAction("Index");
        }

        public IActionResult Remove(int id)
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart");

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

            return RedirectToAction("Index");
        }

        public IActionResult Clear()
        {
            HttpContext.Session.Remove("Cart");

            return Redirect(Request.Headers.Referer.ToString());
        }
    }

}
