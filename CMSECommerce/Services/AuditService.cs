using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CMSECommerce.Services
{
    public class AuditService : IAuditService
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;

        public AuditService(DataContext context, IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task LogAsync(string action, string entityType, string entityId = null,
            string oldValues = null, string newValues = null, HttpContext httpContext = null)
        {
            var context = httpContext ?? _httpContextAccessor.HttpContext;
            var user = context?.User;
            var userId = user?.Identity?.IsAuthenticated == true
                ? _userManager.GetUserId(user)
                : null;

            var ipAddress = context?.Connection?.RemoteIpAddress?.ToString();
            var userAgent = context?.Request?.Headers["User-Agent"].ToString();
            var sessionId = context?.Session?.Id;
            var controller = context?.Request?.RouteValues["controller"]?.ToString();
            var actionMethod = context?.Request?.RouteValues["action"]?.ToString();

            await LogAsync(userId, action, entityType, entityId, oldValues, newValues,
                ipAddress, userAgent, sessionId, controller, actionMethod);
        }

        public async Task LogAsync(string userId, string action, string entityType, string entityId = null,
            string oldValues = null, string newValues = null, string ipAddress = null,
            string userAgent = null, string sessionId = null, string controller = null, string actionMethod = null)
        {
            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                SessionId = sessionId,
                Controller = controller,
                ActionMethod = actionMethod,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();
        }

        // Helper methods for common audit scenarios
        public async Task LogEntityCreationAsync<T>(T entity, string entityId, HttpContext httpContext = null) where T : class
        {
            var newValues = JsonSerializer.Serialize(entity);
            await LogAsync("Created", typeof(T).Name, entityId, null, newValues, httpContext);
        }

        public async Task LogEntityUpdateAsync<T>(T oldEntity, T newEntity, string entityId, HttpContext httpContext = null) where T : class
        {
            var oldValues = JsonSerializer.Serialize(oldEntity);
            var newValues = JsonSerializer.Serialize(newEntity);
            await LogAsync("Updated", typeof(T).Name, entityId, oldValues, newValues, httpContext);
        }

        public async Task LogEntityDeletionAsync<T>(T entity, string entityId, HttpContext httpContext = null) where T : class
        {
            var oldValues = JsonSerializer.Serialize(entity);
            await LogAsync("Deleted", typeof(T).Name, entityId, oldValues, null, httpContext);
        }

        public async Task LogActionAsync(string action, string entityType, string entityId, string details, HttpContext httpContext = null)
        {
            await LogAsync(action, entityType, entityId, null, details, httpContext);
        }

        public async Task LogStatusChangeAsync(string entityType, string entityId, string oldStatus, string newStatus, HttpContext httpContext = null)
        {
            var oldValues = JsonSerializer.Serialize(new { Status = oldStatus });
            var newValues = JsonSerializer.Serialize(new { Status = newStatus });
            await LogAsync("StatusChanged", entityType, entityId, oldValues, newValues, httpContext);
        }
    }
}
