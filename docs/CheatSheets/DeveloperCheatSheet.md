# Developer Quick Reference — CMSECommerce

Local run
- `dotnet build`
- `dotnet run` (or run from Visual Studio)

DB / Migrations
- Add migration: `dotnet ef migrations add <Name>`
- Apply: `dotnet ef database update`
- Check migrations under `Migrations/`

Key files
- `Infrastructure/DataContext.cs` — EF model configuration and seed data
- `Models/Product.cs` — product model and `ProductStatus` enum
- `Infrastructure/SmtpEmailSender.cs` — email sending implementation
- `Controllers/ProductsController.cs` — public product listing and details
- `Areas/Admin/Controllers/ProductsController.cs` — admin product CRUD and approval

UI components
- `Views/Shared/Components/Menu/Default.cshtml` — main nav component
- `Views/Shared/_IdentityLinksPartial.cshtml` — top-right identity menu

Testing
- Ensure correct connection string in `appsettings.json`
- Use seeded admin `admin@local.local` / `Pass@local110` for admin tasks

Notes
- Subscriber support removed; remove any leftover references to `Subscriber` or `SubscriberRequests` if found.
