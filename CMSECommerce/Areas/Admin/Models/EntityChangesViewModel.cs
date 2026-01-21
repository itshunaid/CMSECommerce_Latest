using CMSECommerce.Models;

namespace CMSECommerce.Areas.Admin.Models
{
    public class EntityChangesViewModel
    {
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string EntityType { get; set; }
        public string EntityId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
