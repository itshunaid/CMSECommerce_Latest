using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CMSECommerce.Areas.Admin.Models
{
    public class AuditLogsViewModel
    {
        // The actual list of logs for the current page
        public List<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

        // Pagination Properties
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        // Search & Filter Properties (to persist values in search boxes)
        public string? SearchUser { get; set; }
        public string? SearchAction { get; set; }
        public string? SearchEntityType { get; set; }

        // Date Range Filtering
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Helper property to check if "Previous" button should be enabled
        public bool HasPreviousPage => CurrentPage > 1;

        // Helper property to check if "Next" button should be enabled
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    // This represents the structure of a single Audit Log entry
   
}
