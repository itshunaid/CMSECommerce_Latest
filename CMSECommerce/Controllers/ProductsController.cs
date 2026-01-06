using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Security.Claims;

// NOTE: It is recommended to inject ILogger in a real application for logging exceptions.

namespace CMSECommerce.Controllers
{
    // Injecting dependencies in the primary constructor (C# 12 feature, or standard convention)
    public class ProductsController(
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        UserManager<IdentityUser> userManager,
        IUserStatusService userStatusService
    ) : BaseController
    {
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly IUserStatusService _userStatusService = userStatusService;

        [HttpGet("mystore")]
        public async Task<IActionResult> StoreFront(int? id, int p = 1, string search = "", string category = "", string sort = "")
        {
            //Logic to handle Optional ID
            if (id == null)
            {
                // Get the current logged-in user's ID
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userId))
                {
                    // If not logged in and no ID provided, redirect to login or home
                    return RedirectToAction("Login", "Account");
                }

                // Fetch the StoreId belonging to this user
                // Adjust the query below based on your actual schema (e.g., store.OwnerId or userProfile.StoreId)
                var userStore = await _context.Stores
                  
                    .FirstOrDefaultAsync(s => s.UserId == userId); // Or your specific FK column

                if (userStore == null)
                {
                    return NotFound("You do not have a store configured yet.");
                }

                id = userStore.Id;
            }

            int pageSize = 12; // Matches your current grid layout logic

            // 1. Fetch the store including products
            var store = await _context.Stores
                .Include(s => s.Products.Where(p=> p.Status==ProductStatus.Approved))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null) return NotFound();

            ViewBag.Categories = await _context.Products
        .Where(pr => pr.StoreId == id)
        .Select(pr => pr.Category.Name)
        .Distinct()
        .ToListAsync();

            // 2. Start with the full list of products for this store
            IQueryable<Product> productsQuery = _context.Products.Where(p => p.StoreId == id);

            // 3. Apply SEARCH Filter
            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            // 4. Apply CATEGORY Filter (Example logic)
            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category.Name == category);
            }

            // 5. Apply SORTING
            productsQuery = sort switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "newest" => productsQuery.OrderByDescending(p => p.Id),
                _ => productsQuery.OrderBy(p => p.Name) // Default
            };

            // 6. Pagination Logic
            int totalItems = await productsQuery.CountAsync();
            var pagedProducts = await productsQuery
                .Skip((p - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 7. Update the Store model with the filtered/paged products for the View
            store.Products = pagedProducts;

            // 8. Pass metadata to ViewBag for the CSHTML
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageNumber = p;
            ViewBag.SearchQuery = search;
            ViewBag.CurrentSort = sort;
            ViewBag.StoreId = id;

            return View(store);
        }

        // The Index action handles category filtering, searching, pagination, and sorting.
        /// <summary>
        /// Handles the display of the public product catalog, implementing filtering, sorting, and pagination.
        /// Also retrieves the status of relevant users (Product Owners and Customers) involved in pending orders.
        /// </summary>
        public async Task<IActionResult> Index(
    string slug = "",
    int p = 1,
    string searchTerm = "",
    string sortOrder = "")
        {
            // --- Paging Configuration ---
            const int pageSize = 20;
            int pageNumber = p;

            // --- State Variables Initialization ---
            string categoryName = null;
            string categorySlug = "";
            string searchFilter = searchTerm?.Trim();
            int totalProducts = 0;
            int totalPages = 1;

            List<Product> pagedProducts = new List<Product>();
            IdentityUser currentUser = null;
            List<UserStatusDTO> allOtherUsersStatus = new List<UserStatusDTO>();
            List<string> userNamesToLookUp = new List<string>();

            var currentUserId = _userManager.GetUserId(User);
            var userName = _userManager.GetUserName(User);

            // Initial ViewBags
            ViewBag.IsProcessed = false;
            ViewBag.CategorySlug = slug; // Updated to use the incoming slug
            ViewBag.PageNumber = pageNumber;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageRange = pageSize;

            try
            {
                // --- 1. NEW: Fetch All Categories for the Sidebar (Based on StoreFront logic) ---
                // This ensures the sidebar in your new CSHTML has data to display
                ViewBag.Categories = await _context.Categories
                    .OrderBy(c => c.Name)
                    .AsNoTracking()
                    .ToListAsync();

                // --- 2. Existing Order/User Logic (Preserved for SignalR/Chat) ---
                List<string> customerUserNames = await _context.OrderDetails
                    .Where(p => p.ProductOwner == userName && p.IsProcessed == false)
                    .Select(p => p.Customer)
                    .Distinct()
                    .ToListAsync();

                string usernameLower = userName?.ToLower() ?? "";
                List<int> orderIds = await _context.Orders
                    .Where(o => o.UserName.ToLower().Contains(usernameLower) && o.Shipped == false)
                    .Select(p => p.Id)
                    .ToListAsync();

                List<string> distinctProductOwners = await _context.OrderDetails
                    .Where(x => orderIds.Contains(x.OrderId))
                    .Select(detail => detail.ProductOwner)
                    .Distinct()
                    .ToListAsync();

                userNamesToLookUp.AddRange(distinctProductOwners);
                userNamesToLookUp.AddRange(customerUserNames);
                userNamesToLookUp = userNamesToLookUp.Distinct().ToList();

                // --- 3. Product Catalog Retrieval ---
                IQueryable<Product> products = _context.Products
                    .Where(x => x.Status == Models.ProductStatus.Approved)
                    .AsNoTracking();

                // Apply Category Filter (Slug-based)
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    var category = await _context.Categories
                        .Where(x => x.Slug == slug)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (category != null)
                    {
                        products = products.Where(x => x.CategoryId == category.Id);
                        categoryName = category.Name;
                        categorySlug = slug;
                    }
                    else
                    {
                        TempData["Error"] = $"Category '{slug}' not found.";
                        return RedirectToAction("Index", new { slug = "" });
                    }
                }

                // Apply Search Term Filter (Aligned with StoreFront multi-field search)
                if (!string.IsNullOrWhiteSpace(searchFilter))
                {
                    string lowerSearch = searchFilter.ToLower();
                    products = products.Where(x => x.Name.ToLower().Contains(lowerSearch) ||
                                                 x.Description.ToLower().Contains(lowerSearch));
                }

                // Apply Sorting (Preserving your existing keys)
                products = sortOrder switch
                {
                    "price-asc" => products.OrderBy(x => (double)x.Price),
                    "price-desc" => products.OrderByDescending(x => (double)x.Price),
                    "name-asc" => products.OrderBy(x => x.Name),
                    "newest" => products.OrderByDescending(x => x.Id), // Added from StoreFront
                    _ => products.OrderByDescending(x => x.Id),
                };

                // --- 4. Pagination Execution ---
                totalProducts = await products.CountAsync();
                totalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalPages && totalProducts > 0) pageNumber = totalPages;

                pagedProducts = await products
                    .Include(x => x.Category)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.TotalPages = totalPages; // Syncing the calculated total back to ViewBag

                // --- 5. User Status Service (Preserved) ---
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    allOtherUsersStatus = await _userStatusService.GetAllOtherUsersStatusAsync(currentUserId);
                    allOtherUsersStatus = allOtherUsersStatus
                        .Where(u => userNamesToLookUp.Contains(u.User.UserName))
                        .ToList();

                    currentUser = await _userManager.FindByIdAsync(currentUserId);
                }
            }
            catch (Exception ex)
            {
                // General error handling as per your original code
                TempData["Error"] = "An error occurred while loading the shop.";
                pagedProducts = new List<Product>();
            }

            // --- 6. Return ViewModel ---
            var viewModel = new ProductListViewModel
            {
                Products = pagedProducts,
                CategoryName = categoryName,
                CurrentSearchTerm = searchFilter,
                AllUsers = allOtherUsersStatus,
                CurrentUser = currentUser
            };

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
            
            
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.User.UserName.ToLower() == product.OwnerName.ToLower());
            ViewBag.UserProfile = userProfile;
            // Return the view with the product data
            return View(product);
        }


        [Route("policies")]
        public IActionResult Policies()
        {
            return View();
        }

        [Route("privacy-policy")]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}