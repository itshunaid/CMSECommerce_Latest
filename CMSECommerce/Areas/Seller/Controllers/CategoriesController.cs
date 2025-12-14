using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Seller.Controllers
{
    // Restricts access to users with the "Subscriber" role
    [Authorize(Roles = "Subscriber")]
    [Area("Seller")]
    public class CategoriesController : Controller
    {
        private readonly DataContext _context;
        private readonly ILogger<CategoriesController> _logger; // ADDED ILogger field

        public CategoriesController(DataContext context, ILogger<CategoriesController> logger) // ADDED ILogger injection
        {
            _context = context;
            _logger = logger;
        }

        // 1. READ (Retrieve All)
        // GET /seller/categories
        public async Task<IActionResult> Index()
        {
            try
            {
                // Retrieve all categories and order them by Id
                return View(await _context.Categories.OrderBy(x => x.Id).ToListAsync());
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving categories list in Index.");
                TempData["Error"] = "A database error occurred while loading the category list.";
                return View(new List<Category>()); // Return empty list on failure
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving categories list in Index.");
                TempData["Error"] = "An unexpected error occurred while loading categories.";
                return View(new List<Category>());
            }
        }

        // 2. CREATE (GET View)
        // GET /seller/categories/create
        public IActionResult Create()
        {
            return View();
        }

        // 2. CREATE (POST Logic)
        // POST /seller/categories/create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set the Slug based on the Name, converting to lowercase
                    category.Slug = category.Name.ToLower().Replace(" ", "-");

                    // Check for duplicate slug
                    var slugCheck = await _context.Categories.FirstOrDefaultAsync(x => x.Slug == category.Slug);

                    if (slugCheck != null)
                    {
                        ModelState.AddModelError("Name", "The category name already exists.");
                        return View(category);
                    }

                    _context.Add(category);
                    await _context.SaveChangesAsync(); // Database operation

                    TempData["Success"] = "The category has been added!";

                    return RedirectToAction("Index");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error creating category: {CategoryName}", category.Name);
                    ModelState.AddModelError("", "A database error occurred while saving the category.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error creating category: {CategoryName}", category.Name);
                    ModelState.AddModelError("", "An unexpected error occurred during category creation.");
                }
            }
            return View(category);
        }

        // 3. UPDATE (GET View)
        // GET /seller/categories/edit/5
        public async Task<IActionResult> Edit(int id)
        {
            try
            {
                Category category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    TempData["Error"] = $"Category with ID {id} not found.";
                    return NotFound();
                }
                return View(category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category ID: {CategoryId} for editing.", id);
                TempData["Error"] = "Failed to load category details for editing.";
                return RedirectToAction("Index");
            }
        }

        // 3. UPDATE (POST Logic)
        // POST /seller/categories/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Set the Slug based on the Name, converting to lowercase
                    category.Slug = category.Name.ToLower().Replace(" ", "-");

                    // Check for duplicate slug (excluding the current category being edited)
                    var slugCheck = await _context.Categories
                                                    .Where(x => x.Id != category.Id)
                                                    .FirstOrDefaultAsync(x => x.Slug == category.Slug);

                    if (slugCheck != null)
                    {
                        ModelState.AddModelError("Name", "The category name already exists.");
                        return View(category);
                    }

                    _context.Update(category);
                    await _context.SaveChangesAsync(); // Database operation

                    TempData["Success"] = "The category has been updated!";

                    return RedirectToAction("Index");
                }
                catch (DbUpdateConcurrencyException dbEx)
                {
                    _logger.LogError(dbEx, "Concurrency error updating category ID: {CategoryId}", category.Id);
                    ModelState.AddModelError("", "The category was modified by another user. Please re-load and try again.");
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error updating category ID: {CategoryId}", category.Id);
                    ModelState.AddModelError("", "A database error occurred while saving the category changes.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error updating category ID: {CategoryId}", category.Id);
                    ModelState.AddModelError("", "An unexpected error occurred during category update.");
                }
            }
            return View(category);
        }

        // 4. DELETE (GET View - Confirmation)
        // GET /seller/categories/delete/5
        // Note: The original code showed a GET to a DELETE action which is unusual but kept the logic for finding the category
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading category ID: {CategoryId} for delete confirmation.", id);
                TempData["Error"] = "Failed to load category details for deletion.";
                return RedirectToAction("Index");
            }
        }

        // 4. DELETE (POST Logic - Actual Deletion)
        // POST /seller/categories/delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Category category = null;

            try
            {
                category = await _context.Categories.FindAsync(id);

                if (category == null)
                {
                    TempData["Error"] = "Category not found.";
                    return RedirectToAction("Index");
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync(); // Database operation

                TempData["Success"] = $"The category '{category.Name}' has been deleted!";
            }
            catch (DbUpdateException dbEx) when (dbEx.InnerException?.Message.Contains("FOREIGN KEY constraint") ?? false)
            {
                // Specific handler for FK constraint violation (e.g., if products still use this category)
                _logger.LogError(dbEx, "Attempted to delete category {CategoryId} with dependent records.", id);
                TempData["Error"] = $"Cannot delete category '{category?.Name ?? id.ToString()}'. It is currently used by one or more products.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error deleting category ID: {CategoryId}", id);
                TempData["Error"] = "A database error occurred while deleting the category.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting category ID: {CategoryId}", id);
                TempData["Error"] = "An unexpected error occurred during deletion.";
            }

            return RedirectToAction("Index");
        }
    }
}