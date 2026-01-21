using CMSECommerce.Infrastructure;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")] // Architect Tip: Secure the whole controller, not just one method
    public class StoresController(DataContext context, IAuditService auditService) : Controller
    {
        private readonly DataContext _context = context;
        private readonly IAuditService _auditService = auditService;

        // GET: Admin/Stores
        public async Task<IActionResult> Index(bool? activeOnly)
        {
            // We use IgnoreQueryFilters() here because the Global Filter 
            // usually hides inactive stores from the entire app. 
            // Admins MUST be able to see them to reactivate them.
            var query = _context.Stores.IgnoreQueryFilters().AsQueryable();

            if (activeOnly.HasValue)
            {
                query = query.Where(s => s.IsActive == activeOnly.Value);
            }

            var stores = await query
                .OrderByDescending(s => s.Id)
                .ToListAsync();

            return View(stores);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStoreStatus(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return Json(new { success = false, message = "Invalid User ID." });

            // Use a transaction to ensure both Store and Profile update or neither does
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Fetch entities
                var store = await _context.Stores.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.UserId == userId);
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

                if (store == null)
                    return Json(new { success = false, message = "Store record not found." });

                // 2. Perform Toggle
                store.IsActive = !store.IsActive;

                // 3. Sync Profile Logic (Architectural note: ensures data integrity across domains)
                if (profile != null)
                {
                    profile.IsDeactivated = !store.IsActive;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Audit logging
                await _auditService.LogStatusChangeAsync("Store", store.UserId, !store.IsActive ? "Active" : "Inactive", store.IsActive ? "Active" : "Inactive", HttpContext);

                // 4. Return JSON instead of Redirect for AJAX compatibility
                return Json(new
                {
                    success = true,
                    isActive = store.IsActive,
                    message = $"Store '{store.StoreName}' is now {(store.IsActive ? "Live" : "Offline")}."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log ex here (e.g., ILogger)
                return StatusCode(500, new { success = false, message = "An internal error occurred." });
            }
        }
    }
}