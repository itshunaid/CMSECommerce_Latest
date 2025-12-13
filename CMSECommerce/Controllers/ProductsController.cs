using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CMSECommerce.Controllers
{
    public class ProductsController(
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        UserManager<IdentityUser> userManager) : Controller
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
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
            // NOTE: PageRange will be set in the View Model initializer

            // 1. Start with all approved products
            IQueryable<Product> products = _context.Products
                .Where(x => x.Status == Models.ProductStatus.Approved);

            // --- Initialize View Model Properties ---
            string categoryName = null;
            string categorySlug = "";

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
                categoryName = category.Name; // Capture for VM
                categorySlug = slug;         // Capture for VM
            }
            // ELSE: categorySlug remains "" for main shop page

            // --- 3. Apply Search Term Filter ---
            string searchFilter = searchTerm?.Trim();

            if (!string.IsNullOrWhiteSpace(searchFilter))
            {
                // Filter by product name containing the search term
                products = products.Where(x => x.Name.ToLower().Contains(searchFilter.ToLower()));
            }

            // --- 4. Apply Sorting ---
            products = sortOrder switch
            {
                "price-asc" => products.OrderBy(x => (double)x.Price),
                "price-desc" => products.OrderByDescending(x => (double)x.Price),
                "name-asc" => products.OrderBy(x => x.Name),
                _ => products.OrderByDescending(x => x.Id),
            };
            // sortOrder is already captured

            // --- 5. Calculate Total Pages (Fetch Count) ---
            int totalProducts = await products.CountAsync();
            int totalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

            // --- 6. Validate Page Number ---
            if (p < 1) p = 1;
            if (p > totalPages && totalProducts > 0) p = totalPages;
            int pageNumber = p;

            // --- 7. Apply Paging and Execute Query ---
            List<Product> pagedProducts = await products
                .Include(x => x.Category) // Eager load category data
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();



            // ---8. Fetch User List (Required for the sidebar) ---
            // a) Fetch all users
            var fetchedUsers = await _userManager.Users.ToListAsync();

            // b) Fetch all current statuses from the new table
            // Consider a user "online" only if the IsOnline flag is true AND LastActivity is recent
            var statusEntities = await _context.UserStatuses.ToListAsync();
            var recentThreshold = DateTime.UtcNow.AddMinutes(-5); // consider activity within last5 minutes
            var statuses = statusEntities
                .ToDictionary(s => s.UserId, s => (s.IsOnline && s.LastActivity >= recentThreshold));

            // c) Combine data into DTOs
            var userStatusDtos = fetchedUsers.Select(user => new UserStatusDto
            {
                User = user,
                // Lookup status: Default to false if no entry is found (user never logged in)
                IsOnline = statuses.GetValueOrDefault(user.Id, false)
            }).ToList();

            // Note: GetRealTimeUserStatuses was a placeholder and is not used in production.
            // Real online state comes from UserStatuses table and recent LastActivity check above.

            // --- 9. Create and return the View Model ---
            var viewModel = new ProductListViewModel
            {
                Products = pagedProducts,

                // Product Metadata (From Paging/Filtering Logic)
                CategoryName = categoryName,
                CategorySlug = categorySlug,
                CurrentSearchTerm = searchFilter,
                SortOrder = sortOrder,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                PageRange = pageSize, // Reusing pageSize as the PageRange value

                // User List Data (for the sidebar)
                AllUsers = userStatusDtos
            };

            return View(viewModel);
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