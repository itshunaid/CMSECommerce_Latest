# TODO: Remove Chat Feature Completely - COMPLETED

## Steps Completed
- [x] Remove ChatHub.cs from Hubs folder
- [x] Remove _ChatWidget.cshtml from Views/Shared
- [x] Remove chat-widget-init.js from wwwroot/js
- [x] Remove chat.js from wwwroot/js
- [x] Remove chat-widget.css from wwwroot/css
- [x] Remove ChatMessage.cs model
- [x] Remove chat widget references from _Layout.cshtml
- [x] Remove chat-related SignalR configuration from Program.cs
- [x] Remove ChatMessage DbSet from DataContext.cs
- [x] Remove ChatMessage model configuration from DataContext.cs
- [x] Create migration to drop ChatMessages table
- [x] Clean up any remaining chat references in other files

# TODO: Make GSTIN Optional Throughout the Project

## Steps to Complete
- [ ] Remove GSTIN property from RegisterViewModel.cs
- [ ] Remove GSTIN input field from Views/Account/Register.cshtml
- [ ] Remove GSTIN from JavaScript validation in Views/Account/Register.cshtml
- [ ] Update Controllers/AccountController.cs to remove GSTIN from uniqueness check and Store creation in Register action
- [ ] Test registration process to ensure GSTIN is not required
