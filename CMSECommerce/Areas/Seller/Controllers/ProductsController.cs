using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Models;
using Microsoft.Extensions.Logging;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize("Subscriber")]
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

        // UPDATED: Added string search and string status parameters
        public async Task<IActionResult> Index(int categoryId = 0, string search = "", string status = "", int p = 1)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryId.ToString());

            ViewBag.SelectedCategory = categoryId.ToString();
            ViewBag.CurrentSearch = search; // Pass search query to View for persistence
            ViewBag.CurrentStatus = status; // Pass status filter to View for persistence

            int pageSize = 3;
            ViewBag.PageNumber = p;
            ViewBag.PageRange = pageSize;

            // Filter products by OwnerId (the current logged-in seller)
            var currentUserId = _userManager.GetUserName(User);
            var productsQuery = _context.Products.Where(x => x.OwnerId == currentUserId);

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
                    x.Name.ToLower().Contains(searchLower) ||
                    x.Description.ToLower().Contains(searchLower)
                );
            }

            // 3. Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                // Parse the status string back into the enum
                if (Enum.TryParse(status, true, out ProductStatus productStatus))
                {
                    productsQuery = productsQuery.Where(x => x.Status == productStatus);
                }
            }


            ViewBag.TotalPages = (int)Math.Ceiling((decimal)await productsQuery.CountAsync() / pageSize);

            List<Product> products =
                        await productsQuery
                        .Include(x => x.Category)
                        .OrderByDescending(x => x.Id)
                        .Skip((p - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();

            return View(products);
        }

        public IActionResult Create()
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            if (ModelState.IsValid)
            {
                product.Slug = product.Name.ToLower().Replace(" ", "-");

                var slug = await _context.Products.FirstOrDefaultAsync(x => x.Slug == product.Slug);

                if (slug != null)
                {
                    ModelState.AddModelError("", "That product already exists!");
                    return View(product);
                }

                string imageName;

                if (product.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                    imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;

                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new FileStream(filePath, FileMode.Create);

                    await product.ImageUpload.CopyToAsync(fs);
                    fs.Close();
                }
                else
                {
                    imageName = "noimage.png";
                }

                product.Image = imageName;
                product.Status = ProductStatus.Pending;
                product.OwnerId = _userManager.GetUserName(User);
                _context.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The product has been added and is pending approval!";

                return RedirectToAction("Index");
            }

            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {

            Product product = await _context.Products.FindAsync(id);

            if (product == null) { return NotFound(); }

            // Ensure only the owner can edit their product
            if (product.OwnerId != _userManager.GetUserName(User))
            {
                TempData["Error"] = "You are not authorized to edit this product.";
                return RedirectToAction("Index");
            }

            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + id.ToString());

            if (Directory.Exists(uploadsDir))
            {
                product.GalleryImages = Directory.EnumerateFiles(uploadsDir).Select(x => Path.GetFileName(x));
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);

            // Fetch the existing product from the database
            var dbProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);

            if (dbProduct == null) return NotFound();

            // ⭐ IMPORTANT: Preserve workflow-related properties (Status, OwnerId, RejectionReason)
            // since they are not on the seller's Edit form
            product.Status = ProductStatus.Pending; // Force to pending upon any edit by seller
            product.OwnerId = dbProduct.OwnerId;
            product.RejectionReason = dbProduct.RejectionReason;


            if (ModelState.IsValid)
            {
                // Check if the current seller is the owner before proceeding with save
                if (product.OwnerId != _userManager.GetUserName(User))
                {
                    ModelState.AddModelError("", "You are not authorized to modify this product.");
                    // Re-load gallery images before returning the view on error
                    string uploadsDirForView = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
                    if (Directory.Exists(uploadsDirForView))
                    {
                        product.GalleryImages = Directory.EnumerateFiles(uploadsDirForView).Select(x => Path.GetFileName(x));
                    }
                    return View(product);
                }

                product.Slug = product.Name.ToLower().Replace(" ", "-");

                var slug = await _context.Products.Where(x => x.Id != product.Id).FirstOrDefaultAsync(x => x.Slug == product.Slug);

                if (slug != null)
                {
                    ModelState.AddModelError("", "That product already exists!");
                    // Re-load gallery images before returning the view on error
                    string uploadsDirForView = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
                    if (Directory.Exists(uploadsDirForView))
                    {
                        product.GalleryImages = Directory.EnumerateFiles(uploadsDirForView).Select(x => Path.GetFileName(x));
                    }
                    return View(product);
                }

                if (product.ImageUpload != null)
                {
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                    // Delete old image if it's not the default placeholder
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

                    FileStream fs = new(filePath, FileMode.Create);

                    await product.ImageUpload.CopyToAsync(fs);
                    fs.Close();

                    product.Image = imageName;
                }
                else
                {
                    // No new image uploaded, keep the existing one from the database
                    product.Image = dbProduct.Image;
                }

                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The product has been updated and is pending admin approval!";

                return RedirectToAction("Edit", new { product.Id });
            }

            // ⭐ If validation fails, need to load gallery images back for the view
            string uploadsDirForViewOnFail = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
            if (Directory.Exists(uploadsDirForViewOnFail))
            {
                product.GalleryImages = Directory.EnumerateFiles(uploadsDirForViewOnFail).Select(x => Path.GetFileName(x));
            }

            return View(product);
        }

        public async Task<IActionResult> Delete(int id)
        {
            Product product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                TempData["Error"] = "The product does not exist!";
            }
            else
            {
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
                    Directory.Delete(gallery, true);
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The product has been deleted!";
            }

            return RedirectToAction("Index");
        }

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
        public void DeleteImage(int id, string imageName)
        {
            string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + id.ToString() + "/" + imageName);

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }

        // These actions below are typically administrative but exist in the original prompt.
        // They are kept here for completeness but should likely be restricted further (e.g., to Admin role)
        // if this controller is strictly for the Seller/Subscriber area.
        public async Task<IActionResult> Approve(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Status = ProductStatus.Approved;
            product.RejectionReason = null;
            _context.Update(product);
            await _context.SaveChangesAsync();

            // notify owner via email
            if (!string.IsNullOrEmpty(product.OwnerId))
            {
                var owner = await _userManager.FindByIdAsync(product.OwnerId);
                if (owner != null && !string.IsNullOrEmpty(owner.Email))
                {
                    await _emailSender.SendEmailAsync(owner.Email, "Your product has been approved", $"Your product '{product.Name}' has been approved by admin.");
                }
            }

            TempData["Success"] = "Product has been approved.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Unapprove(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Status = ProductStatus.Pending;
            product.RejectionReason = null;
            _context.Update(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Product has been set to pending.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string reason)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.Status = ProductStatus.Rejected;
            product.RejectionReason = reason;
            _context.Update(product);
            await _context.SaveChangesAsync();

            // notify owner via email
            if (!string.IsNullOrEmpty(product.OwnerId))
            {
                var owner = await _userManager.FindByIdAsync(product.OwnerId);
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

            TempData["Success"] = "Product has been rejected.";
            return RedirectToAction("Index");
        }
    }
}