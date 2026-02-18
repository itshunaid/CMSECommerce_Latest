using CMSECommerce.Models;

namespace CMSECommerce.Areas.Admin.Models
{
    public class UserActivityViewModel
    {
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string UserId { get; set; }
        public string Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
