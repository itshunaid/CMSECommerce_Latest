using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// NOTE: It is recommended to inject ILogger in a real application for logging exceptions.

namespace CMSECommerce.Controllers
{
    // Injecting dependencies in the primary constructor (C# 12 feature, or standard convention)
    public class ProductsController(
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        UserManager<IdentityUser> userManager,
        IUserStatusService userStatusService
    ) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IUserStatusService _userStatusService = userStatusService;

        // The Index action handles category filtering, searching, pagination, and sorting.
        public async Task<IActionResult> Index(
         string slug = "",
         int p = 1,
         string searchTerm = "",
         string sortOrder = "")
        {
            // --- Paging Configuration ---
            int pageSize = 12;
            int pageNumber = p;

            // Initialize view model variables outside the try block for scope
            string categoryName = null;
            string categorySlug = "";
            string searchFilter = searchTerm?.Trim();
            int totalProducts = 0;
            int totalPages = 1;
            List<Product> pagedProducts = new List<Product>();
            IdentityUser currentUser = null;
            // User list for sidebar (should contain ALL OTHER users)
            List<UserStatusDTO> allOtherUsersStatus = new List<UserStatusDTO>();
            var currentUserId = _userManager.GetUserId(User);
            allOtherUsersStatus=await _userStatusService.GetAllOtherUsersStatusAsync(currentUserId);

            try
            {
                // 1. Start with all approved products
                IQueryable<Product> products = _context.Products
                    .Where(x => x.Status == Models.ProductStatus.Approved)
                    .AsNoTracking();

                // --- 2. Apply Category Filter ---
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    Category category = await _context.Categories
                        .Where(x => x.Slug == slug)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (category == null)
                    {
                        TempData["Error"] = $"Category '{slug}' was not found.";
                        return RedirectToAction("Index");
                    }

                    products = products.Where(x => x.CategoryId == category.Id);
                    categoryName = category.Name;
                    categorySlug = slug;
                }

                // --- 3. Apply Search Term Filter ---
                if (!string.IsNullOrWhiteSpace(searchFilter))
                {
                    products = products.Where(x => x.Name.ToLower().Contains(searchFilter.ToLower()));
                }

                // --- 4. Apply Sorting ---
                products = sortOrder switch
                {
                    "price-asc" => products.OrderBy(x => (double)x.Price),
                    "price-desc" => products.OrderByDescending(x => (double)x.Price),
                    "name-asc" => products.OrderBy(x => x.Name),
                    _ => products.OrderByDescending(x => x.Id), // Default sort by ID
                };

                // --- 5. Calculate Total Pages (Fetch Count) ---
                totalProducts = await products.CountAsync();
                totalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

                // --- 6. Validate Page Number ---
                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalPages && totalProducts > 0) pageNumber = totalPages;

                // --- 7. Apply Paging and Execute Query ---
                pagedProducts = await products
                    .Include(x => x.Category) // Eager load category data
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // --- 8. Fetch User List (Required for the sidebar) ---
                

                if (!string.IsNullOrEmpty(currentUserId))
                {
                    // Fetch status for all users EXCEPT the current one
                    allOtherUsersStatus = await _userStatusService.GetAllOtherUsersStatusAsync(currentUserId);

                    // Fetch the current user details
                    currentUser = await _userManager.FindByIdAsync(currentUserId);
                }
            }
            catch (DbUpdateException dbEx)
            {
                // In a real application, inject ILogger and log this error
                // _logger.LogError(dbEx, "Database error in Products Index action.");
                TempData["Error"] = "A database error occurred while fetching products. Please try again later.";
            }
            catch (Exception ex)
            {
                // In a real application, inject ILogger and log this error
                // _logger.LogError(ex, "Unexpected error in Products Index action.");
                TempData["Error"] = "An unexpected error occurred while loading the product list.";
            }

            // --- 9. Create and return the View Model ---
            var viewModel = new ProductListViewModel
            {
                Products = pagedProducts,

                // Product Metadata
                CategoryName = categoryName,
                CurrentSearchTerm = searchFilter,
                // ... other metadata properties ...

                // User List Data (for the sidebar)
                AllUsers = allOtherUsersStatus, // Contains all other users (online and offline)
                CurrentUser = currentUser // Contains the currently logged-in user (for the "You" section)
            };

            // Populate ViewBags needed by the view (since they weren't in the VM)
            ViewBag.CategorySlug = categorySlug;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageRange = pageSize;

            return View(viewModel);
        }


        // --- Product Detail Page ---
        public async Task<IActionResult> Product(string slug = "")
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                TempData["Error"] = "Invalid product identifier.";
                return RedirectToAction("Index");
            }

            Product product = null;

            try
            {
                product = await _context.Products
                    .Where(x => x.Slug == slug)
                    .Include(x => x.Category)
                    .Include(x => x.Reviews)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    TempData["Warning"] = $"The product with slug '{slug}' was not found.";
                    return RedirectToAction("Index");
                }

                // Only load gallery images if the product is found
                string galleryDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());

                // Safely check and enumerate files
                if (Directory.Exists(galleryDir))
                {
                    // Use a simple try/catch for the file operation, as it's separate from the DB
                    try
                    {
                        product.GalleryImages = Directory.EnumerateFiles(galleryDir).Select(x => Path.GetFileName(x)).ToList();
                    }
                    catch (IOException ioEx)
                    {
                        // Log file system errors but allow the product page to load without gallery
                        // _logger.LogError(ioEx, "File system error loading product gallery for ID {ProductId}.", product.Id);
                        product.GalleryImages = new List<string>();
                        TempData["Warning"] = "Could not load all image files for this product.";
                    }
                }
                else
                {
                    product.GalleryImages = new List<string>();
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Log database errors
                // _logger.LogError(dbEx, "Database error in Products Product detail action for slug {Slug}.", slug);
                TempData["Error"] = "A database error occurred while fetching product details. Please try again later.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log general exceptions
                // _logger.LogError(ex, "Unexpected error in Products Product detail action for slug {Slug}.", slug);
                TempData["Error"] = "An unexpected error occurred while loading the product page.";
                return RedirectToAction("Index");
            }

            // Return the view with the product data
            return View(product);
        }
    }
}