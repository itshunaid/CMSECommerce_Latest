
namespace CMSECommerce.Areas.Admin.Controllers
{
    internal class StatusChangesViewModel
    {
        public List<AuditLog> AuditLogs { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string EntityType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}