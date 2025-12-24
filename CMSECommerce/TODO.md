# TODO: Make GSTIN Optional Throughout the Project

## Steps to Complete
- [ ] Remove GSTIN property from RegisterViewModel.cs
- [ ] Remove GSTIN input field from Views/Account/Register.cshtml
- [ ] Remove GSTIN from JavaScript validation in Views/Account/Register.cshtml
- [ ] Update Controllers/AccountController.cs to remove GSTIN from uniqueness check and Store creation in Register action
- [ ] Test registration process to ensure GSTIN is not required
