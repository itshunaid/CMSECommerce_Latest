# Segregated Audit Log Sections Implementation - TODO

## Overview
Add segregated audit log sections for the following categories:
1. User registration and management
2. Product approvals and rejections
3. Order processing and status changes
4. Subscription request management
5. All admin dashboard actions

## Tasks
- [ ] Add UserManagement action to AuditLogsController (EntityType: User/UserProfile, Actions: Created, Updated, Deleted, StatusChanged)
- [ ] Add ProductApprovals action to AuditLogsController (EntityType: Product, Actions: Approved, Rejected)
- [ ] Add OrderProcessing action to AuditLogsController (EntityType: Order, Actions: StatusChanged, Shipped, etc.)
- [ ] Add SubscriptionManagement action to AuditLogsController (EntityType: SubscriptionRequest, Actions: Approved, Rejected, Reverted)
- [ ] Add AdminDashboardActions action to AuditLogsController (filter by admin roles or specific admin actions)
- [ ] Create or reuse view models for each new action (e.g., UserManagementViewModel, ProductApprovalsViewModel, etc.)
- [ ] Create corresponding views in Areas/Admin/Views/AuditLogs/ for each new action
- [ ] Update navigation/menu to include links to new segregated sections
- [ ] Test each new section for proper filtering and display
- [ ] Ensure pagination and search work correctly for each section

## Notes
- Reuse existing filtering, pagination, and search logic from current actions
- Use consistent naming and structure
- Ensure proper authorization (Admin, SuperAdmin roles)
