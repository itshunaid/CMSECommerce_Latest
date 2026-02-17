using CMSECommerce.Controllers;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Subscriber,Admin,SuperAdmin")] // Using Roles for standard Identity
    public class ProductsController(
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        IEmailSender emailSender,
        UserManager<IdentityUser> userManager,
        ILogger<ProductsController> logger) : BaseController
    {
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly IEmailSender _emailSender = emailSender;
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly ILogger<ProductsController> _logger = logger;

        // GET /seller/products/index
        public async Task<IActionResult> Index(int categoryId = 0, string search = "", string status = "", int p = 1)
        {
            int pageSize = 8; // Set to 3 as per your requirement
            try
            {
                ViewBag.Categories = BuildCategorySelectList(categoryId);
                ViewBag.SelectedCategory = categoryId.ToString();
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentStatus = status;

                // Filter products by OwnerId
                var currentUserId = _userManager.GetUserName(User);
                var productsQuery = _context.Products
                    .Where(x => x.OwnerName == currentUserId && x.StockQuantity > 0)
                    .AsQueryable();

                // 1. Apply category filter
                if (categoryId != 0)
                {
                    productsQuery = productsQuery.Where(x => x.CategoryId == categoryId);
                }

                // 2. Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string searchLower = search.ToLower();
                    productsQuery = productsQuery.Where(x =>
                        EF.Functions.Like(x.Name.ToLower(), $"%{searchLower}%") ||
                        EF.Functions.Like(x.Description.ToLower(), $"%{searchLower}%")
                    );
                }

                // 3. Apply status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse(status, true, out ProductStatus productStatus))
                    {
                        productsQuery = productsQuery.Where(x => x.Status == productStatus);
                    }
                }

                // --- PAGINATION CALCULATIONS ---
                int totalItems = await productsQuery.CountAsync();
                int totalPages = (int)Math.Ceiling((decimal)totalItems / pageSize);

                // Safety check: ensure 'p' is within valid range
                p = p < 1 ? 1 : p;
                if (totalPages > 0 && p > totalPages) p = totalPages;

                ViewBag.TotalPages = totalPages;
                ViewBag.PageNumber = p;
                ViewBag.PageRange = pageSize;
                ViewBag.TotalItems = totalItems; // Useful for "Showing 1-3 of 10 items"

                List<Product> products = await productsQuery                    
                    .Include(x => x.Category)
                    .OrderByDescending(x => x.Id)
                    .Skip((p - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Products Index for seller {User}.", _userManager.GetUserName(User));
                TempData["Error"] = "Failed to load products due to a system error.";
                ViewBag.TotalPages = 1;
                ViewBag.PageNumber = 1;
                ViewBag.Categories = new SelectList(new List<Category>());
                return View(new List<Product>());
            }
        }
        public async Task<IActionResult> OutOfStock(int categoryId = 0, string search = "", string status = "", int p = 1)
        {
            int pageSize = 3;
            try
            {
                ViewBag.Categories = BuildCategorySelectList(categoryId);

                ViewBag.SelectedCategory = categoryId.ToString();
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentStatus = status;

                ViewBag.PageNumber = p;
                ViewBag.PageRange = pageSize;

                // Filter products by OwnerId (the current logged-in seller)
                var currentUserId = _userManager.GetUserName(User);
                var productsQuery = _context.Products
    .Where(x => x.OwnerName == currentUserId && x.StockQuantity == 0)
    .AsQueryable();

                // 1. Apply category filter
                if (categoryId != 0)
                {
                    productsQuery = productsQuery.Where(x => x.CategoryId == categoryId);
                }

                // 2. Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string searchLower = search.ToLower();
                    productsQuery = productsQuery.Where(x =>
                        EF.Functions.Like(x.Name.ToLower(), $"%{searchLower}%") ||
                        EF.Functions.Like(x.Description.ToLower(), $"%{searchLower}%")
                    );
                }

                // 3. Apply status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse(status, true, out ProductStatus productStatus))
                    {
                        productsQuery = productsQuery.Where(x => x.Status == productStatus);
                    }
                }

                ViewBag.TotalPages = (int)Math.Ceiling((decimal)await productsQuery.CountAsync() / pageSize);

                List<Product> products = await productsQuery
                    .Include(x => x.Category)
                    .OrderByDescending(x => x.Id)
                    .Skip((p - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return View("Index",products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Products Index for seller {User}.", _userManager.GetUserName(User));
                TempData["Error"] = "Failed to load products due to a system error.";
                // Return empty list and initialize view bag values to prevent runtime errors in the view
                ViewBag.TotalPages = 1;
                ViewBag.Categories = new SelectList(new List<Category>());
                return View(new List<Product>());
            }
        }

        // GET /seller/products/inventory
        public async Task<IActionResult> Inventory(int categoryId = 0, string search = "", string status = "", int p = 1)
        {
            int pageSize = 3;
            try
            {
                ViewBag.Categories = BuildCategorySelectList(categoryId);

                ViewBag.SelectedCategory = categoryId.ToString();
                ViewBag.CurrentSearch = search;
                ViewBag.CurrentStatus = status;

                ViewBag.PageNumber = p;
                ViewBag.PageRange = pageSize;

                // Filter products by OwnerId and StockQuantity == 0
                var currentUserId = _userManager.GetUserName(User);
                var productsQuery = _context.Products.Where(x => x.OwnerName == currentUserId && x.StockQuantity == 0).AsQueryable();

                // 1. Apply category filter
                if (categoryId != 0)
                {
                    productsQuery = productsQuery.Where(x => x.CategoryId == categoryId);
                }

                // 2. Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    string searchLower = search.ToLower();
                    productsQuery = productsQuery.Where(x =>
                        EF.Functions.Like(x.Name.ToLower(), $"%{searchLower}%") ||
                        EF.Functions.Like(x.Description.ToLower(), $"%{searchLower}%")
                    );
                }

                // 3. Apply status filter
                if (!string.IsNullOrWhiteSpace(status))
                {
                    if (Enum.TryParse(status, true, out ProductStatus productStatus))
                    {
                        productsQuery = productsQuery.Where(x => x.Status == productStatus);
                    }
                }

                ViewBag.TotalPages = (int)Math.Ceiling((decimal)await productsQuery.CountAsync() / pageSize);

                List<Product> products = await productsQuery
                    .Include(x => x.Category)
                    .OrderByDescending(x => x.Id)
                    .Skip((p - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                ViewBag.ProductsCount = products.Count();
                return View("Index", products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Inventory Index for seller {User}.", _userManager.GetUserName(User));
                TempData["Error"] = "Failed to load inventory due to a system error.";
                ViewBag.TotalPages = 1;
                ViewBag.Categories = new SelectList(new List<Category>());
                return View("Index", new List<Product>());
            }
        }

        // GET /seller/products/create
        // GET /seller/products/create
        public IActionResult Create()
        {
            try
            {
                ViewBag.Categories = BuildCategorySelectList();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Category list for Create View.");
                TempData["Error"] = "System error: Could not load categories.";
                return RedirectToAction("Index");
            }
        }

        // POST /seller/products/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Validate Profile and Subscription
            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile?.Store == null)
            {
                TempData["Error"] = "You must have an active store to add products.";
                return RedirectToAction("Index", "UserProfiles");
            }

            if (!profile.SubscriptionEndDate.HasValue || profile.SubscriptionEndDate.Value < DateTime.Now)
            {
                ModelState.AddModelError("", "Subscription expired. Please renew to continue.");
            }

            // 2. Check Product Limits
            var currentProductCount = await _context.Products.CountAsync(p => p.UserId == userId);
            if (currentProductCount >= profile.CurrentProductLimit)
            {
                ModelState.AddModelError("", $"Limit reached ({profile.CurrentProductLimit} products).");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // 3. Generate Unique Slug (Allows duplicate Names)
                    string baseSlug = product.Name.ToLower().Trim().Replace(" ", "-");                    
                    string uniqueSlug = baseSlug;
                    int suffix = 1;

                    // Loop to find a truly unique slug in the DB
                    while (await _context.Products.AnyAsync(p => p.Slug == uniqueSlug))
                    {
                        uniqueSlug = $"{baseSlug}-{suffix}";
                        suffix++;
                    }
                    product.Slug = uniqueSlug;

                    // 4. Handle Image Upload Safely
                    if (product.ImageUpload != null)
                    {
                        string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");
                        if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                        string fileName = $"{Guid.NewGuid()}_{Path.GetFileName(product.ImageUpload.FileName)}";
                        string filePath = Path.Combine(uploadsDir, fileName);

                        using (var fs = new FileStream(filePath, FileMode.Create))
                        {
                            await product.ImageUpload.CopyToAsync(fs);
                        }
                        product.Image = fileName;
                    }
                    else
                    {
                        product.Image = "noimage.png";
                    }

                    // 5. Assign Ownership and Metadata
                    product.UserId = userId;
                    product.OwnerName = _userManager.GetUserName(User);
                    product.StoreId = (int)profile.StoreId;
                    product.Status = ProductStatus.Pending;

                    _context.Add(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Product created successfully and is awaiting approval.";
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Critical error creating product for user {UserId}", userId);
                    ModelState.AddModelError("", "A server error occurred. Please try again.");
                }
            }

            // Repopulate Categories if validation fails
            ViewBag.Categories = BuildCategorySelectList(product.CategoryId);
            return View(product);
        }

        // GET /seller/products/edit/5
        public async Task<IActionResult> Edit(int id)
        {
            // 1. Declare the variable here so it's accessible in both try and catch
            Product product = null;

            try
            {
                product = await _context.Products.FindAsync(id);

                if (product == null) { return NotFound(); }

                // Authorization check
                if (product.OwnerName != _userManager.GetUserName(User))
                {
                    TempData["Error"] = "You are not authorized to edit this product.";
                    return RedirectToAction("Index");
                }

                ViewBag.Categories = BuildCategorySelectList(product.CategoryId);

                // File operation
                string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + id.ToString());

                if (Directory.Exists(uploadsDir))
                {
                    product.GalleryImages = Directory.EnumerateFiles(uploadsDir).Select(x => Path.GetFileName(x));
                }

                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Edit View for product ID {ProductId} by seller {User}.", id, _userManager.GetUserName(User));

                TempData["Error"] = "Failed to load categories, but you can still edit the product.";
                ViewBag.Categories = new SelectList(new List<Category>());

                // 2. Now 'product' is accessible here. 
                // Note: If FindAsync failed, product might still be null.
                if (product == null)
                {
                    return RedirectToAction("Index");
                }

                return View(product);
            }
        }

        // POST /seller/products/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            ViewBag.Categories = BuildCategorySelectList(product.CategoryId);

            // Fetch the existing product from the database
            Product dbProduct = null;
            try
            {
                // We use AsNoTracking because we will update the 'product' object later
                dbProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
                if (dbProduct == null) return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error while fetching product ID {ProductId} for edit POST.", product.Id);
                TempData["Error"] = "A database error occurred while verifying the product.";
                return RedirectToAction("Index");
            }

            // Authorization check
            if (dbProduct.OwnerName != _userManager.GetUserName(User))
            {
                TempData["Error"] = "You are not authorized to modify this product.";
                return RedirectToAction("Index");
            }

            // --- PRESERVE EXISTING DATA & WORKFLOW ---
            product.Status = ProductStatus.Pending; // Force re-approval on edit
            product.OwnerName = dbProduct.OwnerName;
            product.UserId = dbProduct.UserId;       // Preserve the original owner ID
            product.StoreId = dbProduct.StoreId;     // Preserve the Store association
            product.RejectionReason = dbProduct.RejectionReason;

            if (ModelState.IsValid)
            {
                try
                {
                    product.Slug = product.Name.ToLower().Replace(" ", "-");

                    // Check for slug duplication excluding the current product ID
                    var slug = await _context.Products
                        .Where(x => x.Id != product.Id)
                        .FirstOrDefaultAsync(x => x.Slug == product.Slug);

                    if (slug != null)
                    {
                        ModelState.AddModelError("", "That product name already exists (slug conflict)!");
                        goto ReloadViewOnFail;
                    }

                    if (product.ImageUpload != null)
                    {
                        string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                        // Delete old image if it's not the default
                        if (!string.Equals(dbProduct.Image, "noimage.png"))
                        {
                            string oldImagePath = Path.Combine(uploadsDir, dbProduct.Image);
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        // Save new image
                        string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                        string filePath = Path.Combine(uploadsDir, imageName);
                        await using (FileStream fs = new(filePath, FileMode.Create))
                        {
                            await product.ImageUpload.CopyToAsync(fs);
                        }

                        product.Image = imageName;
                    }
                    else
                    {
                        // No new image uploaded, keep the existing one from DB
                        product.Image = dbProduct.Image;
                    }

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "The product has been updated and is pending admin approval!";
                    return RedirectToAction("Edit", new { product.Id });
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error during product update for ID {ProductId} by seller {User}.", product.Id, _userManager.GetUserName(User));
                    ModelState.AddModelError("", "A database error occurred while saving the product changes.");
                }
                catch (IOException ioEx)
                {
                    _logger.LogError(ioEx, "File system error during image handling for product ID {ProductId} by seller {User}.", product.Id, _userManager.GetUserName(User));
                    ModelState.AddModelError("", "An error occurred while saving/deleting the product image.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error during product update for ID {ProductId} by seller {User}.", product.Id, _userManager.GetUserName(User));
                    ModelState.AddModelError("", "An unexpected error occurred.");
                }
            }

        ReloadViewOnFail:
            // If validation or exception occurred, re-load gallery images and return view
            string uploadsDirForViewOnFail = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
            if (Directory.Exists(uploadsDirForViewOnFail))
            {
                product.GalleryImages = Directory.EnumerateFiles(uploadsDirForViewOnFail).Select(x => Path.GetFileName(x));
            }

            return View(product);
        }
        // GET /seller/products/delete/5
        public async Task<IActionResult> Delete(int id)
        {
            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database error finding product ID {ProductId} for deletion by seller {User}.", id, _userManager.GetUserName(User));
                TempData["Error"] = "A database error occurred while trying to find the product.";
                return RedirectToAction("Index");
            }

            if (product == null)
            {
                TempData["Error"] = "The product does not exist!";
                return RedirectToAction("Index");
            }

            // Authorization check
            if (product.OwnerName != _userManager.GetUserName(User))
            {
                TempData["Error"] = "You are not authorized to delete this product.";
                return RedirectToAction("Index");
            }

            try
            {
                // File operation: Delete main image
                if (!string.Equals(product.Image, "noimage.png"))
                {
                    string productImage = Path.Combine(_webHostEnvironment.WebRootPath, "media/products/" + product.Image);
                    if (System.IO.File.Exists(productImage))
                    {
                        System.IO.File.Delete(productImage);
                    }
                }

                // File operation: Delete gallery folder
                string gallery = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
                if (Directory.Exists(gallery))
                {
                    Directory.Delete(gallery, true);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync(); // Database operation

                TempData["Success"] = "The product has been deleted!";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error deleting product ID {ProductId} by seller {User}.", id, _userManager.GetUserName(User));
                TempData["Error"] = "A database error occurred while deleting the product.";
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "File system error deleting images/gallery for product ID {ProductId} by seller {User}.", id, _userManager.GetUserName(User));
                TempData["Error"] = "A file system error occurred while cleaning up product images.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting product ID {ProductId} by seller {User}.", id, _userManager.GetUserName(User));
                TempData["Error"] = "An unexpected error occurred during deletion.";
            }

            return RedirectToAction("Index");
        }


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

                return Content("ok");
            }

            return Content("ok");
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


        

        // GET /seller/products/approve/5
        public async Task<IActionResult> Approve(int id)
        {
            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Authorization check (Should be Admin role, but since the controller is only 'Subscriber' we skip that check)
                if (product.OwnerName != _userManager.GetUserName(User))
                {
                    TempData["Error"] = "You are not authorized to approve this product.";
                    return RedirectToAction("Index");
                }


                product.Status = ProductStatus.Approved;
                product.RejectionReason = null;
                _context.Update(product);
                await _context.SaveChangesAsync(); // Database operation

                // Email notification (Handled below for isolation)

                TempData["Success"] = "Product has been approved.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error approving product ID {ProductId}.", id);
                TempData["Error"] = "A database error occurred while approving the product.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving product ID {ProductId}.", id);
                TempData["Error"] = "An unexpected error occurred during approval.";
            }

            // Isolate Email sender logic to prevent it from blocking the DB operation save
            if (product != null && !string.IsNullOrEmpty(product.OwnerName))
            {
                try
                {
                    var owner = await _userManager.FindByIdAsync(product.OwnerName);
                    if (owner != null && !string.IsNullOrEmpty(owner.Email))
                    {
                        await _emailSender.SendEmailAsync(owner.Email, "Your product has been approved", $"Your product '{product.Name}' has been approved by admin.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending approval email to owner of product {ProductId}.", product.Id);
                    // Do not block redirect, failure to send email is a secondary concern.
                }
            }

            return RedirectToAction("Index");
        }

        // GET /seller/products/unapprove/5
        public async Task<IActionResult> Unapprove(int id)
        {
            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Authorization check
                if (product.OwnerName != _userManager.GetUserName(User))
                {
                    TempData["Error"] = "You are not authorized to change the status of this product.";
                    return RedirectToAction("Index");
                }

                product.Status = ProductStatus.Pending;
                product.RejectionReason = null;
                _context.Update(product);
                await _context.SaveChangesAsync(); // Database operation

                TempData["Success"] = "Product has been set to pending.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error unapproving product ID {ProductId}.", id);
                TempData["Error"] = "A database error occurred while setting the product to pending.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error unapproving product ID {ProductId}.", id);
                TempData["Error"] = "An unexpected error occurred during status change.";
            }

            return RedirectToAction("Index");
        }

        // POST /seller/products/reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            Product product = null;
            try
            {
                product = await _context.Products.FindAsync(id);
                if (product == null) return NotFound();

                // Authorization check
                if (product.OwnerName != _userManager.GetUserName(User))
                {
                    TempData["Error"] = "You are not authorized to reject this product.";
                    return RedirectToAction("Index");
                }

                product.Status = ProductStatus.Rejected;
                product.RejectionReason = reason;
                _context.Update(product);
                await _context.SaveChangesAsync(); // Database operation

                TempData["Success"] = "Product has been rejected.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error rejecting product ID {ProductId}.", id);
                TempData["Error"] = "A database error occurred while rejecting the product.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error rejecting product ID {ProductId}.", id);
                TempData["Error"] = "An unexpected error occurred during rejection.";
            }

            // Isolate Email sender logic
            if (product != null && !string.IsNullOrEmpty(product.OwnerName))
            {
                try
                {
                    var owner = await _userManager.FindByIdAsync(product.OwnerName);
                    if (owner != null && !string.IsNullOrEmpty(owner.Email))
                    {
                        await _emailSender.SendEmailAsync(owner.Email, "Your product was rejected", $"Your product '{product.Name}' was rejected by admin. Reason: {reason}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending rejection email to owner of product {ProductId}.", product.Id);
                }
            }

            return RedirectToAction("Index");
        }

        // POST: /Seller/Products/ToggleVisibility
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleVisibility(int id, bool isVisible)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found." });
                }

                // Authorization check
                if (product.OwnerName != _userManager.GetUserName(User))
                {
                    return Json(new { success = false, message = "You are not authorized to modify this product." });
                }

                product.IsVisible = isVisible;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Product visibility set to {(isVisible ? "visible" : "hidden")}." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling visibility for product ID {ProductId} by seller {User}.", id, _userManager.GetUserName(User));
                return Json(new { success = false, message = "An error occurred while updating product visibility." });
            }
        }

        // Helper method to build hierarchical category select list
        private SelectList BuildCategorySelectList(int selectedCategoryId = 0)
        {
            var categories = _context.Categories
                .OrderBy(c => c.Level)
                .ThenBy(c => c.Name)
                .ToList();

            var selectListItems = new List<SelectListItem>();

            foreach (var category in categories)
            {
                var indent = new string('—', category.Level);
                selectListItems.Add(new SelectListItem
                {
                    Value = category.Id.ToString(),
                    Text = $"{indent} {category.Name}",
                    Selected = category.Id == selectedCategoryId
                });
            }

            return new SelectList(selectListItems, "Value", "Text");
        }
    }
}
