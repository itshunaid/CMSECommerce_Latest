using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Areas.Seller.Controllers
{
    // Restricts access to users with the "Admin" role
    [Authorize(Roles = "Subscriber")]
    [Area("Seller")]
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
            // Retrieve all categories and order them by Id
            return View(await _context.Categories.OrderBy(x => x.Id).ToListAsync());
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
                // Set the Slug based on the Name, converting to lowercase
                category.Slug = category.Name.ToLower().Replace(" ", "-");

                // Check for duplicate slug
                var slugCheck = await _context.Categories.FirstOrDefaultAsync(x => x.Slug == category.Slug);

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
            return View(category);
        }

        // 3. UPDATE (GET View)
        // GET /admin/categories/edit/5
        public async Task<IActionResult> Edit(int id)
        {
            Category category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // 3. UPDATE (POST Logic)
        // POST /admin/categories/edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                // Set the Slug based on the Name, converting to lowercase
                category.Slug = category.Name.ToLower().Replace(" ", "-");

                // Check for duplicate slug (excluding the current category being edited)
                var slugCheck = await _context.Categories.Where(x => x.Id != category.Id)
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
            return View(category);
        }

        // 4. DELETE (GET View - Confirmation)
        // GET /admin/categories/delete/5
        public async Task<IActionResult> Delete(int id)
        {
            Category category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                TempData["Error"] = "Category does not exist!";
                return NotFound();
            }

            return View(category);
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

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "The category has been deleted!";

            return RedirectToAction("Index");
        }
    }
}