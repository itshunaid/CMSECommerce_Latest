using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Models;
using Microsoft.Extensions.Logging;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize("Admin")]
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

        public async Task<IActionResult> Index(int categoryId = 0, int p = 1)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryId.ToString());

            ViewBag.SelectedCategory = categoryId.ToString();
            int pageSize = 3;
            ViewBag.PageNumber = p;
            ViewBag.PageRange = pageSize;

            Category category = await _context.Categories
                                 .Where(x => x.Id == categoryId)
                                 .FirstOrDefaultAsync();

            if (category == null)
            {
                ViewBag.TotalPages = (int)Math.Ceiling((decimal)_context.Products.Count() / pageSize);

                List<Product> products =
                                    await _context.Products
                                    .Include(x => x.Category)
                                    .OrderByDescending(x => x.Id)
                                    .Skip((p - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

                return View(products);
            }

            var productsByCategory = _context.Products.Where(x => x.CategoryId == categoryId);

            ViewBag.TotalPages = (int)Math.Ceiling((decimal)productsByCategory.Count() / pageSize);

            return View(await productsByCategory
                                 .Include(x => x.Category)
                                 .OrderByDescending(x => x.Id)
                                 .Skip((p - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync());

        }
        public async Task<IActionResult> PendingProducts(int categoryId = 0, int p = 1)
        {
            ViewBag.Categories = new SelectList(_context.Categories, "Id", "Name", categoryId.ToString());

            ViewBag.SelectedCategory = categoryId.ToString();
            int pageSize = 3;
            ViewBag.PageNumber = p;
            ViewBag.PageRange = pageSize;
            // 🛠️ NEW: Pass the current action name for pagination link building
            ViewBag.PageAction = nameof(Index);

            // Fetch products where status is NOT Approved (Pending or Rejected)
            var productsQuery = _context.Products.Where(x => x.Status != ProductStatus.Approved);

            if (categoryId != 0)
            {
                // Filter by category AND status != Approved
                productsQuery = productsQuery.Where(x => x.CategoryId == categoryId);
            }

            ViewBag.TotalPages = (int)Math.Ceiling((decimal)productsQuery.Count() / pageSize);

            return View("Index", await productsQuery
                                         .Include(x => x.Category)
                                         .OrderByDescending(x => x.Id)
                                         .Skip((p - 1) * pageSize)
                                         .Take(pageSize)
                                         .ToListAsync());
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

            // ⭐ VALIDATION: Check for StockQuantity validation here before proceeding
            // The [Range] attribute handles the check, so we just rely on ModelState.IsValid

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
                product.OwnerId = _userManager.GetUserName(User);

                _context.Add(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The product has been added!";

                return RedirectToAction("Index");
            }

            return View(product);
        }

        public async Task<IActionResult> Edit(int id)
        {
            Product product = await _context.Products.FindAsync(id);

            if (product == null) { return NotFound(); }

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

            // Get the current product from the database to preserve non-form fields (like Image, OwnerId, Status)
            var dbProduct = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id);
            if (dbProduct == null) return NotFound();

            if (ModelState.IsValid)
            {
                product.Slug = product.Name.ToLower().Replace(" ", "-");

                var slug = await _context.Products.Where(x => x.Id != product.Id).FirstOrDefaultAsync(x => x.Slug == product.Slug);

                if (slug != null)
                {
                    ModelState.AddModelError("", "That product already exists!");
                    return View(product);
                }

                // ⭐ Preserve Image and other data if not updated via form
                if (product.ImageUpload == null)
                {
                    // No new image uploaded, keep the existing one from the database
                    product.Image = dbProduct.Image;
                }
                else
                {
                    // New image uploaded, proceed with file operations
                    string uploadsDir = Path.Combine(_webHostEnvironment.WebRootPath, "media/products");

                    if (!string.Equals(dbProduct.Image, "noimage.png"))
                    {
                        string oldImagePath = Path.Combine(uploadsDir, dbProduct.Image);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    string imageName = Guid.NewGuid().ToString() + "_" + product.ImageUpload.FileName;
                    string filePath = Path.Combine(uploadsDir, imageName);

                    FileStream fs = new(filePath, FileMode.Create);
                    await product.ImageUpload.CopyToAsync(fs);
                    fs.Close();

                    product.Image = imageName;
                }

                // ⭐ Copy back OwnerId and Status since they are not typically in the Edit form
                // Using AsNoTracking above prevents issues with Update() below
                product.OwnerId = dbProduct.OwnerId;
                product.Status = dbProduct.Status;
                product.RejectionReason = dbProduct.RejectionReason;


                _context.Update(product);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The product has been updated!";

                return RedirectToAction("Edit", new { product.Id });
            }

            // ⭐ If validation fails, need to load gallery images back for the view
            string uploadsDirForView = Path.Combine(_webHostEnvironment.WebRootPath, "media/gallery/" + product.Id.ToString());
            if (Directory.Exists(uploadsDirForView))
            {
                product.GalleryImages = Directory.EnumerateFiles(uploadsDirForView).Select(x => Path.GetFileName(x));
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