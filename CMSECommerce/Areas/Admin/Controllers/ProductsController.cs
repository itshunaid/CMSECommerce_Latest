using CMSECommerce.Areas.Admin.Models;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")] // Using standard Authorize attribute for roles
    public class ProductsController(
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        IEmailSender emailSender,
        UserManager<IdentityUser> userManager,
        ILogger<ProductsController> logger) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly ILogger<ProductsController> _logger = logger;

        // 1. Index Action (Approved Products View with Filtering/Sorting/Pagination)
        public async Task<IActionResult> Index(
            string sortOrder,
            string SearchName,
            string SearchDescription,
            string SearchCategory,
            string SearchStatus,
            int? pageNumber,
            int pageSize = 6)
        {
            // Initialization for graceful failure using the new Empty() method
            PaginatedList<Product> paginatedProducts = PaginatedList<Product>.Empty();
            int currentPage = pageNumber ?? 1;

            try
            {
                ViewBag.AllProducts = true;

                // 2. Base Query
                var products = _context.Products.Include(p => p.Category).AsQueryable();

                // If no status is explicitly searched, default to Approved products
                if (string.IsNullOrEmpty(SearchStatus))
                {
                    products = products.Where(p => p.Status == ProductStatus.Approved);
                }


                // 3. Filtering (Case-Insensitive)
                if (!string.IsNullOrEmpty(SearchName))
                {
                    products = products.Where(p => p.Name.ToLower().Contains(SearchName.ToLower()));
                }
                if (!string.IsNullOrEmpty(SearchDescription))
                {
                    products = products.Where(p => p.Description.ToLower().Contains(SearchDescription.ToLower()));
                }
                if (!string.IsNullOrEmpty(SearchCategory))
                {
                    products = products.Where(p => p.Category.Name.ToLower().Contains(SearchCategory.ToLower()));
                }

                // Status Filtering Logic
                if (!string.IsNullOrEmpty(SearchStatus))
                {
                    if (Enum.TryParse(SearchStatus, true, out ProductStatus status))
                    {
                        products = products.Where(p => p.Status == status);
                    }
                }

                // 4. Sorting (Original logic preserved)
                products = sortOrder switch
                {
                    "name_desc" => products.OrderByDescending(p => p.Name),
                    "Name" => products.OrderBy(p => p.Name),
                    "price_desc" => products.OrderByDescending(p => p.Price),
                    "Price" => products.OrderBy(p => p.Price),
                    "category_desc" => products.OrderByDescending(p => p.Category.Name),
                    "Category" => products.OrderBy(p => p.Category.Name),
                    _ => products.OrderByDescending(p => p.Id), // Default sort by ID descending
                };

                // 5. Pagination
                paginatedProducts = await PaginatedList<Product>.CreateAsync(products.AsNoTracking(), currentPage, pageSize);

            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving product list.");
                TempData["Error"] = "A database error occurred while loading the products list.";
                // paginatedProducts remains PaginatedList<Product>.Empty()
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Admin Products Index.");
                TempData["Error"] = "An unexpected error occurred while loading products.";
                // paginatedProducts remains PaginatedList<Product>.Empty()
            }

            // 6. Return ViewModel
            var viewModel = new ProductListViewModel
            {
                Products = paginatedProducts,
                CurrentSortOrder = sortOrder,
                CurrentSearchName = SearchName,
                CurrentSearchDescription = SearchDescription,
                CurrentSearchCategory = SearchCategory,
                CurrentSearchStatus = SearchStatus, // Fix 3: Now included in the model
                CurrentPageSize = pageSize
            };

            // REMOVED: ViewBag.CurrentSearchStatus is no longer needed since it's in the model
            return View(viewModel);
        }

        // 2. PendingProducts Action
        public async Task<IActionResult> PendingProducts(int categoryId = 0, int p = 1)
        {
            PaginatedList<Product> paginatedProducts = PaginatedList<Product>.Empty(); // Fix 1
            int pageSize = 3;
            List<Category> categories = new List<Category>();
            string selectedCategoryName = null;

            try
            {
                // 1. Setup
                categories = await _context.Categories.AsNoTracking().ToListAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId.ToString());
                ViewBag.SelectedCategory = categoryId.ToString();
                ViewBag.AllProducts = false;

                // 2. Base Query and Filtering
                var productsQuery = _context.Products
                                             .Include(x => x.Category)
                                             .Where(x => x.Status != ProductStatus.Approved)
                                             .AsQueryable();

                if (categoryId != 0)
                {
                    productsQuery = productsQuery.Where(x => x.CategoryId == categoryId);
                    selectedCategoryName = categories.FirstOrDefault(c => c.Id == categoryId)?.Name;
                }

                // 3. Counting and Pagination
                int totalCount = await productsQuery.CountAsync();

                // Apply sorting, skipping, and taking
                var products = await productsQuery
                                             .OrderByDescending(x => x.Id)
                                             .Skip((p - 1) * pageSize)
                                             .Take(pageSize)
                                             .ToListAsync();

                // 4. Instantiate PaginatedList
                paginatedProducts = new PaginatedList<Product>(products, totalCount, p, pageSize);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving pending products list.");
                TempData["Error"] = "A database error occurred while loading pending products.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Admin Products PendingProducts.");
                TempData["Error"] = "An unexpected error occurred while loading pending products.";
            }

            // 5. Return the ViewModel
            var viewModel = new ProductListViewModel
            {
                Products = paginatedProducts,
                CurrentSearchCategory = selectedCategoryName,
                CurrentPageSize = pageSize,
                // Add default status filter for the pending view
                CurrentSearchStatus = "Pending"
            };

            return View("Index", viewModel);
        }

        public IActionResult Create()
        {
            try
            {
                ViewBag.Categories = BuildCategorySelectList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories for Product Create view.");
                TempData["Error"] = "Could not load categories for the form.";
                ViewBag.Categories = new List<SelectListItem>();
            }

            return View();
        }

        // 3. Create (POST Logic)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Validate Profile and Store (Consistent with Seller logic)
            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile?.Store == null)
            {
                TempData["Error"] = "You must have an active store to add products.";
                return RedirectToAction("Index", "Dashboard"); // Redirect to admin dashboard
            }

            // Ensure categories are loaded for the View in case of validation failure
            try
            {
                ViewBag.Categories = BuildCategorySelectList(product.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories for Product Create view during post back.");
                ViewBag.Categories = new List<SelectListItem>();
            }

            if (ModelState.IsValid)
            {
                product.Slug = product.Name?.ToLower().Replace(" ", "-");

                try
                {
                    var slug = await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == product.Slug);

                    if (slug != null)
                    {
                        ModelState.AddModelError("", "That product already exists!");
                        return View(product);
                    }

                    string imageName = "noimage.png";

                    if (product.ImageUpload != null)
                    {
                        string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                        imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                        string filePath = Path.Combine(uploadsDir, imageName);

                        // File System Operation (I/O Exception possible)
                        using (FileStream fs = new(filePath, FileMode.Create))
                        {
                            await product.ImageUpload.CopyToAsync(fs);
                        }
                    }

                    product.Image = imageName;
                    product.OwnerName = _userManager.GetUserName(User);
                    product.UserId = userId;
                    product.StoreId = (int)profile.StoreId;
                    product.Status = ProductStatus.Approved; // Admin created products are approved immediately

                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "The product has been added!";
                    return RedirectToAction("Index");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error creating product: {ProductName}", product.Name);
                    ModelState.AddModelError("", "A database error occurred while saving the product.");
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(ioEx, "File system error saving image for product: {ProductName}", product.Name);
                    ModelState.AddModelError("", "An error occurred while uploading the product image.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error creating product: {ProductName}", product.Name);
                    ModelState.AddModelError("", "An unexpected error occurred while creating the product.");
                }
            }

            return View(product);
        }

        // 4. Edit (GET View)
        public async Task<IActionResult> Edit(int id)
        {
            Product product = null;

            try
            {
                product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    TempData["Error"] = "Product not found.";
                    return NotFound();
                }

                ViewBag.Categories = BuildCategorySelectList(product.CategoryId);

                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + id.ToString());

                // File System Operation (I/O Exception possible)
                if (Directory.Exists(uploadsDir))
                {
                    product.GalleryImages = Directory.EnumerateFiles(uploadsDir).Select(x => Path.GetFileName(x));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product for edit, ID: {ProductId}", id);
                TempData["Error"] = "Failed to load categories, but you can still edit the product.";
                ViewBag.Categories = new List<SelectListItem>();
                return View(product);
            }

            return View(product);
        }

        // 5. Edit (POST Logic)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            // Ensure categories are loaded for the View in case of validation failure
            try
            {
                ViewBag.Categories = BuildCategorySelectList(product.CategoryId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load categories for Product Edit view during post back.");
                ViewBag.Categories = new List<SelectListItem>();
            }

            // Get the current product from the database for existing data preservation
            Product dbProduct = null;
            try
            {
                dbProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "DB Error fetching existing product data for edit, ID: {ProductId}", product.Id);
                ModelState.AddModelError("", "Database error fetching original product data.");
            }

            if (dbProduct == null) return NotFound();

            if (ModelState.IsValid && dbProduct != null)
            {
                product.Slug = product.Name?.ToLower().Replace(" ", "-");

                try
                {
                    var slugCheck = await _context.Products.Where(x => x.Id != product.Id).AsNoTracking().FirstOrDefaultAsync(x => x.Slug == product.Slug);

                    if (slugCheck != null)
                    {
                        ModelState.AddModelError("", "That product already exists!");
                        return View(product);
                    }

                    // --- Image Handling ---
                    if (product.ImageUpload == null)
                    {
                        product.Image = dbProduct.Image; // Keep existing image
                    }
                    else
                    {
                        // Delete old image
                        string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                        if (!string.Equals(dbProduct.Image, "noimage.png"))
                        {
                            string oldImagePath = Path.Combine(uploadsDir, dbProduct.Image);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath); // File System Operation
                            }
                        }

                        // Save new image
                        string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                        string filePath = Path.Combine(uploadsDir, imageName);

                        using (FileStream fs = new(filePath, FileMode.Create))
                        {
                            await product.ImageUpload.CopyToAsync(fs); // File System Operation
                        }
                        product.Image = imageName;
                    }

                    // Preserve original data not in the form
                    product.OwnerName = dbProduct.OwnerName;
                    product.RejectionReason = dbProduct.RejectionReason;

                    // Set status to Pending upon any edit by Admin
                    product.Status = ProductStatus.Pending;

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "The product has been updated! Status set to Pending for review.";
                    return RedirectToAction("Edit", new { product.Id });
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error updating product ID: {ProductId}", product.Id);
                    ModelState.AddModelError("", "A database error occurred while updating the product.");
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(ioEx, "File system error during image update for product ID: {ProductId}", product.Id);
                    ModelState.AddModelError("", "An error occurred while handling the product image files.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error editing product ID: {ProductId}", product.Id);
                    ModelState.AddModelError("", "An unexpected error occurred while updating the product.");
                }
            }

            // If validation fails or exception occurs, reload gallery images for the view
            try
            {
                string uploadsDirForView = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
                if (Directory.Exists(uploadsDirForView))
                {
                    product.GalleryImages = Directory.EnumerateFiles(uploadsDirForView).Select(x => Path.GetFileName(x));
                }
            }
            catch (IOException ioEx)
            {
                _logger.LogWarning(ioEx, "Failed to reload gallery images after failed edit for product ID: {ProductId}", product.Id);
                // Continue, don't crash
            }

            return View(product);
        }

        // 6. Delete
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                Product product = await _context.Products.FindAsync(id);

                if (product == null)
                {
                    TempData["Error"] = "The product does not exist!";
                    return RedirectToAction("Index");
                }

                // --- File System Operations ---
                if (!string.Equals(product.Image, "noimage.png"))
                {
                    string productImage = Path.Combine(_webHostEnvironment.WebRootPath, "media/products/" + product.Image);
                    if (System.IO.File.Exists(productImage))
                    {
                        System.IO.File.Delete(productImage);
                    }
                }

                string gallery = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
                if (Directory.Exists(gallery))
                {
                    Directory.Delete(gallery, true); // Recursive delete
                }

                // --- Database Operation ---
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The product has been deleted!";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error deleting product ID: {ProductId}", id);
                TempData["Error"] = "A database error occurred while deleting the product. Check for related records.";
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "File system error during product deletion for ID: {ProductId}", id);
                TempData["Error"] = "File error occurred while deleting product images. The product was not deleted from the database.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting product ID: {ProductId}", id);
                TempData["Error"] = "An unexpected error occurred during deletion.";
            }

            return RedirectToAction("Index");
        }

        // 7. UploadImages (Partial View/API endpoint)
        // POST: /Seller/Products/UploadImages
        [HttpPost]
        public async Task<IActionResult> UploadImages(int id)
        {
            var files = HttpContext.Request.Form.Files;

            if (files.Any())
            {
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + id.ToString());

                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                foreach (var file in files)
                {
                    string imageName = Guid.NewGuid().ToString() + "_" + file.FileName;

                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new(filePath, FileMode.Create);

                    await file.CopyToAsync(fs);
                    fs.Close();
                }

                return Ok();
            }

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> DeleteImage(int id, string imageName)
        {
            if (string.IsNullOrEmpty(imageName))
            {
                return BadRequest("Image name is required.");
            }

            try
            {
                // 1. Build the correct physical path
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "media", "gallery", id.ToString(), imageName);

                // 2. Perform the deletion
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);

                    // Since this is an AJAX call for immediate UI update, 
                    // we don't need to reload the view or update ViewBag here.
                    // The JavaScript will handle the removal of the element.
                    return Content("ok");
                }

                return NotFound("Image not found on the server.");
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "File system error deleting image {ImageName} for product {Id}", imageName, id);
                return StatusCode(500, "File is in use or system error.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting image {ImageName} for product {Id}", imageName, id);
                return StatusCode(500, "An unexpected error occurred.");
            }
        }


        // 9. Approve
        public async Task<IActionResult> Approve(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    TempData["Error"] = $"Product ID {id} not found.";
                    return RedirectToAction("PendingProducts");
                }

                // Database Update
                product.Status = ProductStatus.Approved;
                product.RejectionReason = null;
                _context.Update(product);
                await _context.SaveChangesAsync();

                // Notification (Email exceptions are caught internally in the utility)
                if (!string.IsNullOrEmpty(product.OwnerName))
                {
                    var owner = await _userManager.FindByIdAsync(product.OwnerName);
                    if (owner != null && !string.IsNullOrEmpty(owner.Email))
                    {
                        await _emailSender.SendEmailAsync(owner.Email, "Your product has been approved", $"Your product '{product.Name}' has been approved by admin.");
                    }
                }

                TempData["Success"] = $"Product '{product.Name}' has been approved.";

                // Redirect logic
                var productsRequestCount = await _context.Products.CountAsync(p => p.Status == ProductStatus.Pending || p.Status == ProductStatus.Rejected);

                return productsRequestCount == 0 ? RedirectToAction("Index", "Dashboard") : RedirectToAction("PendingProducts");
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error approving product ID: {ProductId}", id);
                TempData["Error"] = "A database error occurred while approving the product.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving product ID: {ProductId}", id);
                TempData["Error"] = "An unexpected error occurred during product approval.";
            }
            return RedirectToAction("PendingProducts");
        }

        // 10. Unapprove
        public async Task<IActionResult> Unapprove(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Database Update
                product.Status = ProductStatus.Pending;
                product.RejectionReason = null;
                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Product has been set back to pending for review.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error unapproving product ID: {ProductId}", id);
                TempData["Error"] = "A database error occurred while setting the product to pending.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error unapproving product ID: {ProductId}", id);
                TempData["Error"] = "An unexpected error occurred during status change.";
            }
            return RedirectToAction("Index");
        }

        // 11. Reject
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Database Update
                product.Status = ProductStatus.Rejected;
                product.RejectionReason = reason;
                _context.Update(product);
                await _context.SaveChangesAsync();

                // Notification (Email exceptions are caught internally in the utility)
                if (!string.IsNullOrEmpty(product.OwnerName))
                {
                    var owner = await _userManager.FindByIdAsync(product.OwnerName);
                    if (owner != null && !string.IsNullOrEmpty(owner.Email))
                    {
                        try
                        {
                            await _emailSender.SendEmailAsync(owner.Email, "Your product was rejected", $"Your product '{product.Name}' was rejected by admin. Reason: {reason}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed sending rejection email to {Email} for product {ProductId}", owner.Email, product.Id);
                        }
                    }
                }

                TempData["Success"] = $"Product '{product.Name}' has been rejected.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error rejecting product ID: {ProductId}", id);
                TempData["Error"] = "A database error occurred while rejecting the product.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error rejecting product ID: {ProductId}", id);
                TempData["Error"] = "An unexpected error occurred during product rejection.";
            }

            return RedirectToAction("PendingProducts");
        }

        // Helper: build hierarchical select list of categories
        private IEnumerable<SelectListItem> BuildCategorySelectList(int? selectedId = null)
        {
            try
            {
                var cats = _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();

                // If no categories in database, add some defaults
                if (!cats.Any())
                {
                    cats = new List<Category>
                    {
                        new Category { Id = 1, Name = "Electronics", Slug = "electronics", ParentId = null, Level = 0 },
                        new Category { Id = 2, Name = "Clothing", Slug = "clothing", ParentId = null, Level = 0 },
                        new Category { Id = 3, Name = "Home & Garden", Slug = "home-garden", ParentId = null, Level = 0 },
                        new Category { Id = 4, Name = "Books", Slug = "books", ParentId = null, Level = 0 },
                        new Category { Id = 5, Name = "Sports", Slug = "sports", ParentId = null, Level = 0 },
                        new Category { Id = 6, Name = "Health & Beauty", Slug = "health-beauty", ParentId = null, Level = 0 },
                        new Category { Id = 7, Name = "Toys & Games", Slug = "toys-games", ParentId = null, Level = 0 },
                        new Category { Id = 8, Name = "Automotive", Slug = "automotive", ParentId = null, Level = 0 }
                    };
                }

                var byParent = cats.GroupBy(c => c.ParentId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());
                var items = new List<SelectListItem>();

                void AddChildren(int? parentId, string prefix)
                {
                    if (!byParent.ContainsKey(parentId)) return;
                    foreach (var c in byParent[parentId])
                    {
                        items.Add(new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = prefix + c.Name,
                            Selected = selectedId.HasValue && selectedId.Value == c.Id
                        });
                        AddChildren(c.Id, prefix + "— ");
                    }
                }

                AddChildren(null, "");
                return items;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build category select list.");
                // Return default categories as fallback
                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "1", Text = "Electronics" },
                    new SelectListItem { Value = "2", Text = "Clothing" },
                    new SelectListItem { Value = "3", Text = "Home & Garden" },
                    new SelectListItem { Value = "4", Text = "Books" },
                    new SelectListItem { Value = "5", Text = "Sports" },
                    new SelectListItem { Value = "6", Text = "Health & Beauty" },
                    new SelectListItem { Value = "7", Text = "Toys & Games" },
                    new SelectListItem { Value = "8", Text = "Automotive" }
                };
            }
        }
    }
}