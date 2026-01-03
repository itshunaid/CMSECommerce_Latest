using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
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



        //[AllowAnonymous]
        //public async Task<IActionResult> StoreFront(int id)
        //{
        //    // 1. If you need the profile with store info
        //    var userProfile = await _context.UserProfiles
        //        .Include(p => p.Store)
        //        .FirstOrDefaultAsync(p => p.Id == id); // Assuming 'id' refers to Profile ID here

        //    // 2. To get the Store and its associated Products
        //    var store = await _context.Stores
        //        .Include(s => s.Products) // This works because Store has a collection of Products
        //        .FirstOrDefaultAsync(s => s.Id == id); // Here 'id' is the Store ID

        //    if (store == null) return NotFound();

        //    return View(store);
        //}

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

            // --- State Variables Initialization (for full method scope) ---
            string categoryName = null;
            string categorySlug = "";
            string searchFilter = searchTerm?.Trim();
            int totalProducts = 0;
            int totalPages = 1;

            // Ensure these lists are initialized for the final ViewModel creation, even on error
            List<Product> pagedProducts = new List<Product>();
            IdentityUser currentUser = null;
            List<UserStatusDTO> allOtherUsersStatus = new List<UserStatusDTO>();
            List<string> userNamesToLookUp = new List<string>();

            var currentUserId = _userManager.GetUserId(User);
            var userName = _userManager.GetUserName(User);

            // Set ViewBags here to ensure they are available even if the try block fails
            ViewBag.IsProcessed = false;
            ViewBag.CategorySlug = categorySlug;
            ViewBag.PageNumber = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.SortOrder = sortOrder;
            ViewBag.PageRange = pageSize;


            try
            {
                // --- 1. User and Order Data Retrieval (Vulnerable to DB and Null Exceptions) ---

                // Fetch unprocessed order details for the seller (current user)
                List<OrderDetail> orderDetails = await _context.OrderDetails
                    .Where(p => p.ProductOwner == userName && p.IsProcessed == false)
                    .OrderBy(d => d.OrderId)
                    .ToListAsync();

                // Get distinct customers with unprocessed orders for the seller
                List<string> customerUserNames = await _context.OrderDetails
                    .Where(p => p.ProductOwner == userName && p.IsProcessed == false)
                    .Select(p => p.Customer)
                    .Distinct()
                    .ToListAsync();


                // Get the current user's username in lowercase for case-insensitive matching
                string usernameLower = userName?.ToLower() ?? "";

                // 1. Get ALL relevant Order IDs for the current user
                // We use a single query to get all IDs that match the user's name.
                List<int> orderIds = await _context.Orders
                    .Where(o => o.UserName.ToLower().Contains(usernameLower) && o.Shipped==false)
                    // We only need the IDs, so we project (Select) them.
                    .Select(p => p.Id)
                    .ToListAsync(); // Execute this query now.

                // 2. Fetch DISTINCT Product Owners in ONE database query
                // We query the OrderDetails table where the OrderId is contained within the list of relevant IDs.
                // This is the efficient alternative to the inefficient foreach loop.
                List<string> distinctProductOwners = await _context.OrderDetails
                    // Use the 'Contains' operator to check OrderId against the list of IDs
                    .Where(x => orderIds.Contains(x.OrderId))
                    // Select only the ProductOwner username
                    .Select(detail => detail.ProductOwner)
                    // Ensure only unique names are returned
                    .Distinct()
                    .ToListAsync(); // Execute this single query.

                // Combine all relevant users for status lookup
                userNamesToLookUp.AddRange(distinctProductOwners);
                userNamesToLookUp.AddRange(customerUserNames);
                userNamesToLookUp = userNamesToLookUp.Distinct().ToList();


                // --- 2. Product Catalog Data Retrieval and Paging Logic ---

                // Start with all approved products
                IQueryable<Product> products = _context.Products
                    .Where(x => x.Status == Models.ProductStatus.Approved)
                    .AsNoTracking();

                // Apply Category Filter
                if (!string.IsNullOrWhiteSpace(slug))
                {
                    Category category = await _context.Categories
                        .Where(x => x.Slug == slug)
                        .AsNoTracking()
                        .FirstOrDefaultAsync();

                    if (category == null)
                    {
                        TempData["Error"] = $"Category '{slug}' was not found.";
                        return RedirectToAction("Index"); // Redirect on category error
                    }

                    products = products.Where(x => x.CategoryId == category.Id);
                    categoryName = category.Name;
                    categorySlug = slug;
                }

                // Apply Search Term Filter
                if (!string.IsNullOrWhiteSpace(searchFilter))
                {
                    products = products.Where(x => x.Name.ToLower().Contains(searchFilter.ToLower()));
                }

                // Apply Sorting
                products = sortOrder switch
                {
                    "price-asc" => products.OrderBy(x => (double)x.Price),
                    "price-desc" => products.OrderByDescending(x => (double)x.Price),
                    "name-asc" => products.OrderBy(x => x.Name),
                    _ => products.OrderByDescending(x => x.Id),
                };

                // Calculate Total Pages (Fetch Count)
                totalProducts = await products.CountAsync();
                totalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

                // Validate Page Number
                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalPages && totalProducts > 0) pageNumber = totalPages;

                // Apply Paging and Execute Query
                pagedProducts = await products
                    .Include(x => x.Category)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // --- 3. Fetch User Status Data ---
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    // Fetch status for all users (This service call is also a database/network operation)
                    allOtherUsersStatus = await _userStatusService.GetAllOtherUsersStatusAsync(currentUserId);

                    // Filter the fetched status list to only include relevant users
                    List<UserStatusDTO> relevantUsersStatus = allOtherUsersStatus
                        .Where(userStatus => userNamesToLookUp.Contains(userStatus.User.UserName))
                        .ToList();

                    // Replace the full list with the filtered list for the ViewModel
                    allOtherUsersStatus = relevantUsersStatus;

                    // Fetch the current user details
                    currentUser = await _userManager.FindByIdAsync(currentUserId);
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Handle database-specific errors (e.g., connection issues, constraint violations during complex queries)
                // Log the error: _logger.LogError(dbEx, "Database error while processing Index action data.");
                TempData["Error"] = "A database connection error occurred. Please try again later.";
                // Reset lists/variables to safe defaults for the view
                pagedProducts = new List<Product>();
                allOtherUsersStatus = new List<UserStatusDTO>();
            }
            catch (NullReferenceException nullEx)
            {
                // Handle unexpected null references, though explicit checks above minimize this.
                // Log the error: _logger.LogError(nullEx, "Null reference error in Index action.");
                TempData["Error"] = "An internal data error occurred. The requested order data may be missing.";
                pagedProducts = new List<Product>();
                allOtherUsersStatus = new List<UserStatusDTO>();
            }
            catch (Exception ex)
            {
                // Handle all other unexpected errors (e.g., service call failure, general processing errors)
                // Log the error: _logger.LogError(ex, "Unexpected error in Index action.");
                TempData["Error"] = "An unexpected error occurred while loading the shop page.";
                pagedProducts = new List<Product>();
                allOtherUsersStatus = new List<UserStatusDTO>();
            }

            // --- 4. Create and return the View Model (Safe to execute, as all variables are initialized) ---
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