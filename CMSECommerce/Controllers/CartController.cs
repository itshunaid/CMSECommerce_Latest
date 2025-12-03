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
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? [];

            // 🛠️ START: Stock Check Logic

            // 1. Get a list of all Product IDs in the cart
            IEnumerable<int> productIds = cart.Select(c => c.ProductId).ToList();

            // 2. Fetch the corresponding Products and their StockQuantity from the database
            //    We use ToDictionary for fast lookup by ProductId later.
            Dictionary<int, int> productStocks = _context.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionary(p => p.Id, p => p.StockQuantity);

            // 3. Iterate through the cart items and check stock
            foreach (var item in cart)
            {
                if (productStocks.TryGetValue(item.ProductId, out int availableStock))
                {
                    // Check if the requested cart quantity exceeds available stock
                    if (item.Quantity > availableStock)
                    {
                        item.IsOutOfStock = true;
                        // Optionally, you might want to cap the quantity to the available stock
                        // item.Quantity = availableStock; 
                    }
                    else
                    {
                        item.IsOutOfStock = false;
                    }
                }
                else
                {
                    // If the product doesn't exist in the database (e.g., deleted), 
                    // treat it as out of stock.
                    item.IsOutOfStock = true;
                }
            }

            // 🛠️ END: Stock Check Logic

            // 4. Update the session cart with the modified items (including IsOutOfStock status)
            HttpContext.Session.SetJson("Cart", cart);

            CartViewModel cartVM = new()
            {
                CartItems = cart,
                // GrandTotal calculation remains the same
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
