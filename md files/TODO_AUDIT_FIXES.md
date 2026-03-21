# TODO: Fix Audit Log Implementation

## Overview
Fix audit log EntityType and Action values to match the filters in AuditLogsController actions so that logs appear in the respective pages.

## Changes Needed

### User Management Logs
- [x] AccountController Register: Change LogEntityCreationAsync to LogActionAsync("Created", "User", newUser.Id, HttpContext)
- [x] UsersController Create: Change LogEntityCreationAsync to LogActionAsync("Created", "User", user.Id, HttpContext)
- [x] UsersController Edit: Change LogEntityUpdateAsync to LogActionAsync("Updated", "User", user.Id, HttpContext)
- [x] UsersController Delete: Change LogEntityDeletionAsync to LogActionAsync("Deleted", "User", user.Id, HttpContext)

### Product Approvals Logs
- [x] ProductsController Create: Change LogEntityCreationAsync to LogActionAsync("Submitted", "Product", product.Id.ToString(), HttpContext)
- [x] ProductsController Approve: Change LogStatusChangeAsync to LogActionAsync("Approved", "Product", product.Id.ToString(), HttpContext)
- [x] ProductsController Reject: Change LogStatusChangeAsync to LogActionAsync("Rejected", "Product", product.Id.ToString(), HttpContext)

### Order Processing Logs
- [x] OrdersController: Update actions to use "Shipped", "Processed", "Delivered", "Cancelled" instead of current values

### Subscription Management Logs
- [x] SubscriberRequestsController: Change "Approve Subscriber Request" to "Approved", "Reject Subscriber Request" to "Rejected"
- [x] SubscriptionController: Change "Approve Subscription Request" to "Approved", "Reject Subscription Request" to "Rejected"

### Admin Dashboard Actions Logs
- [x] AccountController: Add login/logout audit logs with EntityType "AdminDashboard"
- [x] UnlockRequestsController: Change "Approve Unlock Request" to "Approved"

## Testing
- [ ] Test each audit log page to ensure logs are now visible
- [ ] Verify no existing functionality is broken
