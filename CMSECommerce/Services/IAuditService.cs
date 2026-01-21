using Microsoft.AspNetCore.Http;

namespace CMSECommerce.Services
{
    public interface IAuditService
    {
        Task LogEntityCreationAsync<T>(T entity, string entityId, HttpContext httpContext) where T : class;
        Task LogEntityUpdateAsync<T>(T oldEntity, T newEntity, string entityId, HttpContext httpContext) where T : class;
        Task LogEntityDeletionAsync<T>(T entity, string entityId, HttpContext httpContext) where T : class;
        Task LogActionAsync(string action, string entityType, string entityId, string details, HttpContext httpContext);
    }
}
