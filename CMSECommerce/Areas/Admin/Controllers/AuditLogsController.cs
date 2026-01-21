using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Areas.Admin.Models;
using CMSECommerce.Infrastructure;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AuditLogsController : Controller
    {
        private readonly DataContext _context;

        public AuditLogsController(DataContext context)
        {
            _context = context;
        }

        // GET: Admin/AuditLogs/GetActions
        [HttpGet]
        public async Task<IActionResult> GetActions()
        {
            var actions = await _context.AuditLogs
                .Select(a => a.Action)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();

            return Json(actions);
        }

        // GET: Admin/AuditLogs/UserManagement
        public async Task<IActionResult> UserManagement(
            string searchUser,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            int? pageNumber,
            int pageSize = 20)
        {
            int currentPage = pageNumber ?? 1;

            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == "User" || a.EntityType == "UserProfile")
                .Where(a => a.Action == "Created" || a.Action == "Updated" || a.Action == "Deleted" || a.Action == "StatusChanged")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
            {
                query = query.Where(a => a.User.UserName.Contains(searchUser) ||
                                        a.User.Email.Contains(searchUser));
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

            query = query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync();
            var auditLogs = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new UserManagementViewModel
            {
                AuditLogs = auditLogs,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                SearchUser = searchUser,
                Action = action,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: Admin/AuditLogs/ProductApprovals
        public async Task<IActionResult> ProductApprovals(
            string searchUser,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            int? pageNumber,
            int pageSize = 20)
        {
            int currentPage = pageNumber ?? 1;

            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == "Product")
                .Where(a => a.Action == "Approved" || a.Action == "Rejected" || a.Action == "Submitted")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
            {
                query = query.Where(a => a.User.UserName.Contains(searchUser) ||
                                        a.User.Email.Contains(searchUser));
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

            query = query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync();
            var auditLogs = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new ProductApprovalsViewModel
            {
                AuditLogs = auditLogs,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                SearchUser = searchUser,
                Action = action,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: Admin/AuditLogs/OrderProcessing
        public async Task<IActionResult> OrderProcessing(
            string searchUser,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            int? pageNumber,
            int pageSize = 20)
        {
            int currentPage = pageNumber ?? 1;

            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == "Order")
                .Where(a => a.Action == "Created" || a.Action == "Updated" || a.Action == "Processed" || a.Action == "Shipped" || a.Action == "Delivered")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
            {
                query = query.Where(a => a.User.UserName.Contains(searchUser) ||
                                        a.User.Email.Contains(searchUser));
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

            query = query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync();
            var auditLogs = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new OrderProcessingViewModel
            {
                AuditLogs = auditLogs,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                SearchUser = searchUser,
                Action = action,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: Admin/AuditLogs/SubscriptionManagement
        public async Task<IActionResult> SubscriptionManagement(
            string searchUser,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            int? pageNumber,
            int pageSize = 20)
        {
            int currentPage = pageNumber ?? 1;

            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == "SubscriptionRequest")
                .Where(a => a.Action == "Created" || a.Action == "Approved" || a.Action == "Rejected" || a.Action == "Updated")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
            {
                query = query.Where(a => a.User.UserName.Contains(searchUser) ||
                                        a.User.Email.Contains(searchUser));
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

            query = query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync();
            var auditLogs = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new SubscriptionManagementViewModel
            {
                AuditLogs = auditLogs,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                SearchUser = searchUser,
                Action = action,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }

        // GET: Admin/AuditLogs/AdminDashboardActions
        public async Task<IActionResult> AdminDashboardActions(
            string searchUser,
            string action,
            DateTime? startDate,
            DateTime? endDate,
            int? pageNumber,
            int pageSize = 20)
        {
            int currentPage = pageNumber ?? 1;

            var query = _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.EntityType == "AdminDashboard" || a.EntityType == "System")
                .Where(a => a.Action == "Login" || a.Action == "Logout" || a.Action == "DashboardView" || a.Action == "SettingsChanged")
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchUser))
            {
                query = query.Where(a => a.User.UserName.Contains(searchUser) ||
                                        a.User.Email.Contains(searchUser));
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

            query = query.OrderByDescending(a => a.Timestamp);

            var totalCount = await query.CountAsync();
            var auditLogs = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new AdminDashboardActionsViewModel
            {
                AuditLogs = auditLogs,
                CurrentPage = currentPage,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                PageSize = pageSize,
                SearchUser = searchUser,
                Action = action,
                StartDate = startDate,
                EndDate = endDate
            };

            return View(viewModel);
        }
    }
}
