using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelDataReader; // ✅ Required for reading Excel
// using OfficeOpenXml; // ❌ REMOVED: Conflicting/Paid package
using CMSECommerce.Models; // ✅ ADDED: Assuming Category model is here

namespace CMSECommerce.Areas.Admin.Controllers
{
    // Restricts access to users with the "Admin" role
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public class CategoriesController : Controller
    {
        private readonly DataContext _context;

        public CategoriesController(DataContext context)
        {
            _context = context;
            // ✅ FIX: Register encoding provider for ExcelDataReader to work with XLSX files.
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        // 1. READ (Retrieve All)
        // GET /admin/categories
        public async Task<IActionResult> Index()
        {
            try
            {
                return View(await _context.Categories.OrderBy(x => x.Id).AsNoTracking().ToListAsync());
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "A database error occurred while loading categories.";
                return View(new List<Category>());
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred.";
                return View(new List<Category>());
            }
        }

        // 2. CREATE (GET View)
        // GET /admin/categories/create
        public IActionResult Create()
        {
            return View();
        }

        // 2. CREATE (POST Logic)
        // POST /admin/categories/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Slug = category.Name?.ToLower().Replace(" ", "-");

                try
                {
                    // Check for duplicate slug
                    var slugCheck = await _context.Categories.AsNoTracking().FirstOrDefaultAsync(x => x.Slug == category.Slug);

                    if (slugCheck != null)
                    {
                        ModelState.AddModelError("", "The category name already exists.");
                        return View(category);
                    }

                    _context.Add(category);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "The category has been added!";

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "A database error prevented the category from being saved.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred while saving the category.");
                }
            }

            return View(category);
        }

        // 3. UPDATE (GET View)
        // GET /admin/categories/edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                Category category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["Error"] = "Category not found.";
                    return NotFound();
                }
                return View(category);
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "A database error occurred while fetching the category for editing.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred.";
                return RedirectToAction("Index");
            }
        }

        // 3. UPDATE (POST Logic)
        // POST /admin/categories/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                category.Slug = category.Name?.ToLower().Replace(" ", "-");

                try
                {
                    // Check for duplicate slug (excluding the current category being edited)
                    var slugCheck = await _context.Categories
                                                 .AsNoTracking()
                                                 .Where(x => x.Id != category.Id)
                                                 .FirstOrDefaultAsync(x => x.Slug == category.Slug);

                    if (slugCheck != null)
                    {
                        ModelState.AddModelError("", "The category name already exists.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "The category has been updated!";

                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Concurrency error: The category was modified by another user. Please re-edit.");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "A database error prevented the category from being updated.");
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "An unexpected error occurred while updating the category.");
                }
            }

            return View(category);
        }

        // 4. DELETE (GET View - Confirmation)
        // GET /admin/categories/delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                Category category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    TempData["Error"] = "Category does not exist!";
                    return NotFound();
                }

                return View(category);
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "A database error occurred while fetching the category for deletion.";
                return RedirectToAction("Index");
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred.";
                return RedirectToAction("Index");
            }
        }

        // 4. DELETE (POST Logic - Actual Deletion)
        // POST /admin/categories/delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Category category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction("Index");
            }

            try
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["Success"] = "The category has been deleted!";

                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Could not delete category. Ensure no products are currently linked to it.";
            }
            catch (Exception)
            {
                TempData["Error"] = "An unexpected error occurred during deletion.";
            }

            return RedirectToAction("Index");
        }

        // -----------------------------------------------------------
        // 5. BULK CREATE (GET View)
        // GET /admin/categories/bulkcreate
        // -----------------------------------------------------------
        public IActionResult BulkCreate()
        {
            return View();
        }

        // -----------------------------------------------------------
        // 5. BULK CREATE (POST Logic)
        // POST /admin/categories/bulkcreate
        // -----------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkCreate(IFormFile excelFile)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid Excel file to upload.");
                return View();
            }

            if (!Path.GetExtension(excelFile.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("", "Only .xlsx files are allowed.");
                return View();
            }

            List<Category> newCategories = new List<Category>();
            HashSet<string> categoryNamesInFile = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                using (var stream = excelFile.OpenReadStream())
                {
                    using (var reader = ExcelReaderFactory.CreateReader(stream))
                    {
                        reader.Read();
                        bool isFirstRow = true;

                        while (reader.Read())
                        {
                            if (isFirstRow)
                            {
                                isFirstRow = false;
                                continue;
                            }

                            string categoryName = reader.GetValue(0)?.ToString()?.Trim();

                            if (!string.IsNullOrWhiteSpace(categoryName))
                            {
                                string slug = categoryName.ToLower().Replace(" ", "-");
                                string currentWarning = TempData["Warning"] as string ?? string.Empty; // Safely get current TempData

                                // 1. Check for in-file duplicates
                                if (categoryNamesInFile.Contains(categoryName))
                                {
                                    // ✅ FIX: Concatenate warning safely
                                    TempData["Warning"] = currentWarning + $"Duplicate category name '{categoryName}' ignored in the file. ";
                                    continue;
                                }

                                categoryNamesInFile.Add(categoryName);

                                // 2. Check for database duplicates (by slug)
                                bool isDuplicate = await _context.Categories.AnyAsync(c => c.Slug == slug);
                                if (isDuplicate)
                                {
                                    // ✅ FIX: Concatenate warning safely
                                    TempData["Warning"] = currentWarning + $"Category '{categoryName}' already exists in the database. ";
                                    continue;
                                }

                                newCategories.Add(new Category
                                {
                                    Name = categoryName,
                                    Slug = slug
                                });
                            }
                        }
                    }
                }

                if (newCategories.Any())
                {
                    _context.Categories.AddRange(newCategories);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"{newCategories.Count} new categories have been added successfully!";
                }
                else
                {
                    TempData["Info"] = "No new unique categories were found in the uploaded file.";
                }

                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "A database error occurred while saving categories. No changes were made.";
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"An unexpected error occurred during file processing: {ex.Message}";
                return View();
            }
        }
    }
}