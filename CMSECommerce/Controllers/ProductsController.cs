using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System;

namespace CMSECommerce.Controllers
{
    public class ProductsController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly DataContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IUserStatusService _userStatusService;

        public ProductsController(DataContext context, IWebHostEnvironment webHostEnvironment, UserManager<IdentityUser> userManager, IUserStatusService userStatusService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _userManager = userManager;
            _userStatusService = userStatusService;
        }

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
            var currentUserId = _userManager.GetUserId(User);

            var userStatusDtos = await _userStatusService.GetAllOtherUsersStatusAsync(currentUserId);

            // Find current logged-in user info to display separately (do not show in lists)
            IdentityUser currentUser = null;
            if (!string.IsNullOrEmpty(currentUserId))
            {
                currentUser = await _userManager.FindByIdAsync(currentUserId);
            }

            // ---9. Create and return the View Model ---
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
                AllUsers = userStatusDtos,
                CurrentUser = currentUser
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