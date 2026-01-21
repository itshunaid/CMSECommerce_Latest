using CMSECommerce.Models;
using System.Collections.Generic;

namespace CMSECommerce.Areas.SuperAdmin.Models
{
    public class AuditLogsViewModel
    {
        public List<AuditLog> AuditLogs { get; set; }
        public string SearchString { get; set; }
        public string EntityType { get; set; }
        public string Action { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public List<string> EntityTypes { get; set; }
        public List<string> Actions { get; set; }
    }
}
