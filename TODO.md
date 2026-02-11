# TODO: Force Password Change on First Login for Bulk-Registered Users

## Steps to Complete

- [x] Add MustChangePassword field to UserProfile model
- [x] Create database migration for MustChangePassword field
- [x] Update BulkUpload action to set MustChangePassword = true
- [ ] Modify Login action to check MustChangePassword and redirect if true
- [x] Update ResetPassword action to set MustChangePassword = false after change
- [ ] Run database migration
- [ ] Test bulk upload functionality
- [ ] Verify login redirection and password change behavior
- [ ] Ensure proper error handling and user feedback
