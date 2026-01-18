using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExcelDataReader; // ✅ Required for reading Excel
// using OfficeOpenXml; // ❌ REMOVED: Conflicting/Paid package
using CMSECommerce.Models; // ✅ ADDED: Assuming Category model is here
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin,SuperAdmin")]
    public class CategoriesController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(DataContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // Architect's Note: Use Projection (.Select) to only pull needed columns if list is large
            var categories = await _context.Categories
                .Include(c => c.Parent)
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();
            return View(categories);
        }

        // GET: admin/categories/create
        public async Task<IActionResult> Create()
        {
            await PopulateParentSelectListAsync();
            return View(new Category());
        }

        // POST: admin/categories/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Slug = category.Name?.ToLower().Replace(" ", "-");

                // Unique slug check to avoid DB unique constraint issues
                if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug))
                {
                    ModelState.AddModelError("Name", "A category with this slug/name already exists.");
                    await PopulateParentSelectListAsync();
                    return View(category);
                }

                // Cycle detection
                if (await WouldCreateCycleAsync(category.Id, category.ParentId))
                {
                    ModelState.AddModelError("ParentId", "Invalid parent category selection would create a cycle.");
                    await PopulateParentSelectListAsync();
                    return View(category);
                }

                // compute level
                if (category.ParentId.HasValue)
                {
                    var parent = await _context.Categories.FindAsync(category.ParentId.Value);
                    category.Level = parent != null ? parent.Level + 1 : 0;
                }
                else
                {
                    category.Level = 0;
                }

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();
                TempData["Success"] = "The category has been added!";
                return RedirectToAction(nameof(Index));
            }

            await PopulateParentSelectListAsync();
            return View(category);
        }

        // GET: admin/categories/edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return NotFound();

            await PopulateParentSelectListAsync(category.Id);
            return View(category);
        }

        // POST: admin/categories/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Slug = category.Name?.ToLower().Replace(" ", "-");

                // Ensure slug uniqueness excluding current id
                if (await _context.Categories.AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id))
                {
                    ModelState.AddModelError("Name", "A category with this slug/name already exists.");
                    await PopulateParentSelectListAsync(category.Id);
                    return View(category);
                }

                // prevent parent set to self
                if (category.ParentId == category.Id) category.ParentId = null;

                // Cycle detection for edit
                if (await WouldCreateCycleAsync(category.Id, category.ParentId))
                {
                    ModelState.AddModelError("ParentId", "Invalid parent category selection would create a cycle.");
                    await PopulateParentSelectListAsync(category.Id);
                    return View(category);
                }

                if (category.ParentId.HasValue)
                {
                    var parent = await _context.Categories.FindAsync(category.ParentId.Value);
                    category.Level = parent != null ? parent.Level + 1 : 0;
                }
                else
                {
                    category.Level = 0;
                }

                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "The category has been updated!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error updating category ID {CategoryId}", category.Id);
                    ModelState.AddModelError("", "The category was modified by another user. Please reload and try again.");
                }
            }

            await PopulateParentSelectListAsync(category.Id);
            return View(category);
        }

        // GET: admin/categories/delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.Include(c => c.Parent).FirstOrDefaultAsync(c => c.Id == id);
            if (category == null) return NotFound();
            return View(category);
        }

        // POST: admin/categories/delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null) return RedirectToAction(nameof(Index));

            // prevent deleting category that has children or products
            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentId == id);
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);
            if (hasChildren || hasProducts)
            {
                TempData["Error"] = "Cannot delete category with sub-categories or products assigned.";
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Category deleted.";
            return RedirectToAction(nameof(Index));
        }

        // BulkCreate unchanged
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid Excel file.");
                return View();
            }

            try
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                // 1. Load all existing slugs into memory for O(1) lookup
                // Fetch the list from DB asynchronously, then convert to HashSet in-memory
                var existingSlugs = (await _context.Categories
                    .Select(c => c.Slug)
                    .ToListAsync()) // EF Core Async method
                    .ToHashSet();   // Standard LINQ method

                var newCategories = new List<Category>();
                var fileSlugs = new HashSet<string>();

                using (var stream = excelFile.OpenReadStream())
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    reader.Read(); // Skip Header
                    while (reader.Read())
                    {
                        string name = reader.GetValue(0)?.ToString()?.Trim();
                        if (string.IsNullOrEmpty(name)) continue;

                        string slug = name.ToLower().Replace(" ", "-");

                        // Validate against file duplicates AND database duplicates
                        if (!fileSlugs.Contains(slug) && !existingSlugs.Contains(slug))
                        {
                            fileSlugs.Add(slug);
                            newCategories.Add(new Category { Name = name, Slug = slug });
                        }
                    }
                }

                if (newCategories.Any())
                {
                    _context.Categories.AddRange(newCategories);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    TempData["Success"] = $"{newCategories.Count} categories added.";
                }
                else
                {
                    TempData["Info"] = "No new unique categories found.";
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Log ex here
                ModelState.AddModelError("", "Critical error processing file.");
                return View();
            }
        }

        private async Task PopulateParentSelectListAsync(int? excludeId = null)
        {
            var parents = await _context.Categories
                .Where(c => !excludeId.HasValue || c.Id != excludeId.Value)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();

            var items = parents.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = (c.Parent != null ? c.Parent.Name + " / " : string.Empty) + c.Name
            }).ToList();

            items.Insert(0, new SelectListItem { Value = "", Text = "-- None (Top level) --" });
            ViewBag.ParentCategories = items;
        }

        // New helper: cycle detection to ensure parent assignment doesn't create cycles
        private async Task<bool> WouldCreateCycleAsync(int categoryId, int? newParentId)
        {
            if (!newParentId.HasValue) return false;
            if (newParentId.Value == categoryId) return true; // immediate self-parent

            // Walk up the parent chain from newParentId to see if we encounter categoryId
            var current = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == newParentId.Value);
            while (current != null && current.ParentId.HasValue)
            {
                if (current.ParentId.Value == categoryId) return true;
                current = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == current.ParentId.Value);
            }

            return false;
        }

        // Expose internal helper for unit testing only
        // (This method is not used by runtime; included so unit test can call it.)
        [NonAction]
        public async Task<bool> TestWouldCreateCycle(int categoryId, int? newParentId)
        {
            return await WouldCreateCycleAsync(categoryId, newParentId);
        }
    }
}