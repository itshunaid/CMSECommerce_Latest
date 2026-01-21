using CMSECommerce.Areas.Admin.Models;

namespace CMSECommerce.Areas.SuperAdmin.Models
{
    public class SuperAdminDashboardViewModel : AdminDashboardViewModel
    {
        // Additional metrics for SuperAdmin
        public int TotalAdmins { get; set; }
        public int TotalSubscribers { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalStores { get; set; }
        public int TotalSubscriptionTiers { get; set; }
        public int TotalReviews { get; set; }
        public int TotalChatMessages { get; set; }
        public int TotalUnlockRequests { get; set; }
        public int TotalUserAgreements { get; set; }
        public int TotalUserStatuses { get; set; }
        public int TotalUserStatusSettings { get; set; }

        // System health metrics
        public DateTime LastMigrationDate { get; set; }
        public string DatabaseSize { get; set; }
        public int ActiveUsersLast24Hours { get; set; }
        public int FailedLoginAttempts { get; set; }

        // Recent admin activities
        public IEnumerable<AdminActivity> RecentAdminActivities { get; set; }

        // Recent audit logs
        public List<AuditLog> RecentAuditLogs { get; set; }
    }

    public class AdminActivity
    {
        public string AdminName { get; set; }
        public string Action { get; set; }
        public DateTime Timestamp { get; set; }
        public string Details { get; set; }
    }
}
