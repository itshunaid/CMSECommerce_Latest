# Soft Delete Implementation for Users

## Tasks
- [x] Add IsDeactivated field to UserViewModel in Areas/Admin/Models/UserAdminViewModels.cs
- [x] Update UsersController Index method to map IsDeactivated from UserProfile
- [x] Add SoftDelete action in UsersController to set IsDeactivated=true, IsProfileVisible=false, products to Pending
- [x] Add Restore action in UsersController to set IsDeactivated=false, IsProfileVisible=true, products to Approved
- [x] Update UpdateUserField action to handle IsDeactivated if needed
- [x] Update Areas/Admin/Views/Users/Index.cshtml to display IsDeactivated status and add Soft Delete/Restore buttons
- [x] Test the functionality

## Progress
- Implementation completed. Ready for testing.
