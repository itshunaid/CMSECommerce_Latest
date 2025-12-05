using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CMSECommerce.Controllers
{
    public class ProductsController(
        DataContext context,
        IWebHostEnvironment webHostEnvironment) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        // The Index action handles category filtering, searching, pagination, and sorting.
        public async Task<IActionResult> Index(
            string slug = "",
            int p = 1,
            string searchTerm = "",
            string sortOrder = "")
        {
            // --- Paging Configuration ---
            int pageSize = 12;
            ViewBag.PageRange = pageSize;

            // 1. Start with all approved products
            IQueryable<Product> products = _context.Products
                .Where(x => x.Status == Models.ProductStatus.Approved);

            // --- 2. Apply Category Filter ---
            if (!string.IsNullOrWhiteSpace(slug))
            {
                // Retrieve the category to get its ID and Name
                Category category = await _context.Categories
                    .Where(x => x.Slug == slug)
                    .FirstOrDefaultAsync();

                if (category == null)
                    return RedirectToAction("Index"); // Redirect if category is invalid

                // Filter by category ID
                products = products.Where(x => x.CategoryId == category.Id);
                ViewBag.CategoryName = category.Name; // Set name for view display
                ViewBag.CategorySlug = slug;
            }
            else
            {
                ViewBag.CategorySlug = ""; // Main shop page
            }

            // --- 3. Apply Search Term Filter ---
            string searchFilter = searchTerm?.Trim();
            ViewBag.CurrentSearchTerm = searchFilter;

            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                // Filter by product name containing the search term
                products = products.Where(x => x.Name.ToLower().Contains(searchTerm.ToLower()));
            }

            // --- 4. Apply Sorting ---
            products = sortOrder switch
            {
                // RESOLUTION: Cast Price (decimal) to double for SQLite sorting (if required by provider)
                "price-asc" => products.OrderBy(x => (double)x.Price),     // Price: Low to High
                "price-desc" => products.OrderByDescending(x => (double)x.Price), // Price: High to Low

                // Existing sorting for other fields remains the same
                "name-asc" => products.OrderBy(x => x.Name),
                _ => products.OrderByDescending(x => x.Id),
            };
            ViewBag.SortOrder = sortOrder;

            // --- 5. Calculate Total Pages (Fetch Count) ---
            int totalProducts = await products.CountAsync();
            ViewBag.TotalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

            // --- 6. Validate Page Number ---
            if (p < 1) p = 1;
            if (p > (int)ViewBag.TotalPages && totalProducts > 0) p = (int)ViewBag.TotalPages;
            ViewBag.PageNumber = p;

            // --- 7. Apply Paging and Execute Query ---
            List<Product> pagedProducts = await products
                .Include(x => x.Category) // Eager load category data
                .Skip((p - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View(pagedProducts);
        }

        // --- Product Detail Page ---
        public async Task<IActionResult> Product(string slug = "")
        {
            Product product = await _context.Products
                                                .Where(x => x.Slug == slug)
                                                .Include(x => x.Category) // Include category
                                                                          // 🌟 NECESSARY CHANGE: Eagerly load reviews 🌟
                                                .Include(x => x.Reviews)
                                                .FirstOrDefaultAsync();

            if (product == null) return RedirectToAction("Index");

            string galleryDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());

            if (Directory.Exists(galleryDir))
            {
                product.GalleryImages = Directory.EnumerateFiles(galleryDir).Select(x => Path.GetFileName(x));
            }

            return View(product);
        }
    }
}