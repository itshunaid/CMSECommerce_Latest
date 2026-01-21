using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Areas.SuperAdmin.Models;
using CMSECommerce.Infrastructure;


namespace CMSECommerce.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class AuditLogsController : Controller
    {
        private readonly DataContext _context;

        public AuditLogsController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchString, string entityType, string action,
            DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 50)
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(a =>
                    a.User.UserName.Contains(searchString) ||
                    a.EntityType.Contains(searchString) ||
                    a.Action.Contains(searchString) ||
                    a.EntityId.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(a => a.EntityType == entityType);
            }

            if (!string.IsNullOrEmpty(action))
            {
                query = query.Where(a => a.Action == action);
            }

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            // Get distinct values for dropdowns
            var entityTypes = await _context.AuditLogs
                .Select(a => a.EntityType)
                .Distinct()
                .OrderBy(et => et)
                .ToListAsync();

            var actions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            // Pagination
            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            var auditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AuditLogsViewModel
            {
                AuditLogs = auditLogs,
                SearchString = searchString,
                EntityType = entityType,
                Action = action,
                StartDate = startDate,
                EndDate = endDate,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                TotalRecords = totalRecords,
                EntityTypes = entityTypes,
                Actions = actions
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var auditLog = await _context.AuditLogs
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (auditLog == null)
            {
                return NotFound();
            }

            return View(auditLog);
        }
    }
}
