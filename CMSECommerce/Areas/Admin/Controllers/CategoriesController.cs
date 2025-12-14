using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace CMSECommerce.Areas.Admin.Controllers
{
    // Restricts access to users with the "Admin" role
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    // It is highly recommended to also inject ILogger<CategoriesController> for production logging.
    public class CategoriesController : Controller
    {
        private readonly DataContext _context;

        public CategoriesController(DataContext context)
        {
            _context = context;
        }

        // 1. READ (Retrieve All)
        // GET /admin/categories
        public async Task<IActionResult> Index()
        {
            try
            {
                // Retrieve all categories and order them by Id
                return View(await _context.Categories.OrderBy(x => x.Id).AsNoTracking().ToListAsync());
            }
            catch (DbUpdateException dbEx)
            {
                // Log database errors (e.g., connection issues)
                // _logger.LogError(dbEx, "Database error retrieving categories.");
                TempData["Error"] = "A database error occurred while loading categories.";
                return View(new List<Category>()); // Return empty list to prevent view crash
            }
            catch (Exception ex)
            {
                // Log general exceptions
                // _logger.LogError(ex, "Unexpected error in Categories Index action.");
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
                catch (DbUpdateException dbEx)
                {
                    // Log database error (e.g., concurrency, failed connection)
                    // _logger.LogError(dbEx, "Database error creating category: {CategoryName}", category.Name);
                    ModelState.AddModelError("", "A database error prevented the category from being saved.");
                }
                catch (Exception ex)
                {
                    // Log general exception
                    // _logger.LogError(ex, "Unexpected error creating category: {CategoryName}", category.Name);
                    ModelState.AddModelError("", "An unexpected error occurred while saving the category.");
                }
            }

            // If ModelState is invalid or an exception occurred
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
            catch (DbUpdateException dbEx)
            {
                // Log database error
                // _logger.LogError(dbEx, "Database error retrieving category ID: {CategoryId}", id);
                TempData["Error"] = "A database error occurred while fetching the category for editing.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log general exception
                // _logger.LogError(ex, "Unexpected error retrieving category ID: {CategoryId}", id);
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
                catch (DbUpdateConcurrencyException cEx)
                {
                    // Specific handling for concurrency issues (if multiple admins edit simultaneously)
                    // _logger.LogWarning(cEx, "Concurrency conflict updating category ID: {CategoryId}", category.Id);
                    ModelState.AddModelError("", "Concurrency error: The category was modified by another user. Please re-edit.");
                    // Fall through to return View(category)
                }
                catch (DbUpdateException dbEx)
                {
                    // Log database error
                    // _logger.LogError(dbEx, "Database error updating category ID: {CategoryId}", category.Id);
                    ModelState.AddModelError("", "A database error prevented the category from being updated.");
                    // Fall through to return View(category)
                }
                catch (Exception ex)
                {
                    // Log general exception
                    // _logger.LogError(ex, "Unexpected error updating category ID: {CategoryId}", category.Id);
                    ModelState.AddModelError("", "An unexpected error occurred while updating the category.");
                    // Fall through to return View(category)
                }
            }

            // If ModelState is invalid or an exception occurred
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
            catch (DbUpdateException dbEx)
            {
                // Log database error
                // _logger.LogError(dbEx, "Database error retrieving category ID: {CategoryId} for deletion.", id);
                TempData["Error"] = "A database error occurred while fetching the category for deletion.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log general exception
                // _logger.LogError(ex, "Unexpected error retrieving category ID: {CategoryId} for deletion.", id);
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
            catch (DbUpdateException dbEx)
            {
                // Log database error (e.g., Foreign Key constraint violation if products still link to this category)
                // _logger.LogError(dbEx, "Database error deleting category ID: {CategoryId}", id);
                TempData["Error"] = "Could not delete category. Ensure no products are currently linked to it.";
            }
            catch (Exception ex)
            {
                // Log general exception
                // _logger.LogError(ex, "Unexpected error deleting category ID: {CategoryId}", id);
                TempData["Error"] = "An unexpected error occurred during deletion.";
            }

            return RedirectToAction("Index");
        }
    }
}