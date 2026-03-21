# Segregated Audit Logs Implementation - TODO

## Tasks
- [ ] Move StatusChangesViewModel from Controllers to Models and make public
- [ ] Create UserManagementViewModel in Areas/Admin/Models
- [ ] Create ProductApprovalsViewModel in Areas/Admin/Models
- [ ] Create OrderProcessingViewModel in Areas/Admin/Models
- [ ] Create SubscriptionManagementViewModel in Areas/Admin/Models
- [ ] Create AdminDashboardActionsViewModel in Areas/Admin/Models
- [ ] Add UserManagement action to AuditLogsController (EntityType: User/UserProfile, Actions: Created, Updated, Deleted, StatusChanged)
- [ ] Add ProductApprovals action to AuditLogsController (EntityType: Product, Actions: Approved, Rejected)
- [ ] Add OrderProcessing action to AuditLogsController (EntityType: Order, Actions: StatusChanged, Shipped, etc.)
- [ ] Add SubscriptionManagement action to AuditLogsController (EntityType: SubscriptionRequest, Actions: Approved, Rejected, Reverted)
- [ ] Add AdminDashboardActions action to AuditLogsController (filter by admin roles or specific admin actions)
- [ ] Create UserManagement.cshtml view in Areas/Admin/Views/AuditLogs/
- [ ] Create ProductApprovals.cshtml view in Areas/Admin/Views/AuditLogs/
- [ ] Create OrderProcessing.cshtml view in Areas/Admin/Views/AuditLogs/
- [ ] Create SubscriptionManagement.cshtml view in Areas/Admin/Views/AuditLogs/
- [ ] Create AdminDashboardActions.cshtml view in Areas/Admin/Views/AuditLogs/
- [ ] Update Views/Shared/_Layout.cshtml to include links to new segregated sections
- [ ] Test each new section for proper filtering and display
- [ ] Ensure pagination and search work correctly for each section
