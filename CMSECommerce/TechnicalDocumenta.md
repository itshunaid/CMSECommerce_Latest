# CMSECommerce — Technical Documentation

Version: workspace branch `WorkingOnStore_Categories`
Framework: .NET 8 (ASP.NET Core, Razor + MVC areas)

---

## Table of contents
1. Overview
2. Architecture & Layers
3. Startup and runtime
4. Data model and EF Core
5. Database migrations & seeding
6. Authentication & authorization
7. Areas, controllers & pages
8. UI & front-end components
9. Services, background jobs & integrations
10. API endpoints
11. Testing
12. Build, run & deployment
13. Operational guidance
14. File map (important files)
15. Troubleshooting
16. Recommended improvements

---

## 1. Overview
This document describes the CMSECommerce application: an ASP.NET Core web application with Razor views and MVC controllers organized into Areas for `Admin` and `Seller`. Identity is used for authentication. Entity Framework Core (SQL Server) is used for persistence.

Purpose: provide a single-source technical reference for developers, DevOps, and architects to understand, run, maintain and extend the application.

---

## 2. Architecture & Layers
- Presentation
  - Razor Views (`Views/`, `Areas/*/Views/*`) and View Components.
  - ViewComponents for shared UI: `Infrastructure/Components/CategoriesViewComponent`.
- Application (Controllers)
  - MVC controllers under `Controllers/` and `Areas/Admin/Controllers/`, `Areas/Seller/Controllers/`.
- Data Access
  - EF Core `DataContext` in `Infrastructure/DataContext.cs`.
  - Migrations in `Migrations/`.
- Services & Background
  - Hosted services under `Services` and `Areas/Admin/Services`.
- Cross-cutting
  - Filters: `Infrastructure/Filters/PopulateCategoriesFilter`.
  - SignalR hub: `Hubs/ChatHub`.

---

## 3. Startup & runtime (Key steps)
Primary configuration in `Program.cs`:
- Register services: `AddControllersWithViews`, `AddRazorPages`, `AddDbContext<DataContext>`, Identity, hosted services, session, SignalR.
- Register `PopulateCategoriesFilter` as a scoped service and add it as a global service filter.
- On application start, runs `context.Database.Migrate()` inside a scoped block and calls `DbSeeder.SeedData()` in Development.
- Middleware pipeline: HTTPS redirect, static files, routing, localization (`en-IN`), session, authentication, authorization.
- Route mapping: areas route and several specialized controller routes for `products`, `cart`, `account`, `orders`, plus a slug pages route.

---

## 4. Data model & EF Core
Key entities located in `Models/`:
- `Category`
  - Fields: `Id`, `Name`, `Slug`, `ParentId?`, `Parent`, `Children`, `Level`.
  - Unique index on `Slug` via model annotation and migration.
  - Self-referencing relationship set with `OnDelete(DeleteBehavior.Restrict)` in `DataContext.OnModelCreating`.
- `Product`
  - Fields: `Id`, `Name`, `Slug`, `Description`, `Price`, `CategoryId`, `Category`, `Image`, `StockQuantity`, `Status`, `OwnerName`, `StoreId`, `UserId`, `Reviews`.
- Other: `Store`, `UserProfile`, `SubscriptionTier`, `Order`, `OrderDetail`, etc.

`DataContext` config highlights:
- Query filter for active `Store` entities.
- Indexes and unique constraints configured.
- Identity role and admin user seeding in `OnModelCreating`.

---

## 5. Database migrations & seeding
- Migrations are under `Migrations/`.
- Notable migration: `20260111000000_AddUniqueIndexOnCategorySlug.cs` — includes a dedupe SQL step to avoid index creation failure when duplicate slugs exist.
- Seeding in `DataContext.OnModelCreating` includes roles, admin user, categories, stores, subscription tiers, products, user profiles.
- `DbSeeder.SeedData()` may apply runtime seeding during development on startup.

Migration & producer guidance:
- Always backup production DB before applying migrations that mutate existing data.
- The dedupe migration appends suffixes (`-1`, `-2`) to duplicate slugs — review after run.

---

## 6. Authentication & Authorization
- Uses ASP.NET Core Identity (`AddIdentity<IdentityUser, IdentityRole>()`).
- Cookie auth configured with custom login and access denied paths.
- Roles seeded: `Admin`, `Customer`, `Subscriber`.
- Controllers use `[Authorize(Roles = "Admin")]` and `[Authorize(Roles = "Subscriber")]` where appropriate.
- Password policy relaxed in config; tighten for production.

---

## 7. Areas, controllers & pages
- `Areas/Admin` — Admin controllers and Razor Views for management (Users, Products, Categories, Orders, Pages, Subscriptions, etc.).
- `Areas/Seller` — Seller controllers and views for seller dashboard, products, orders; note: Category management was removed from the seller area.
- Public controllers: products, account, subscription.

Important controllers:
- `Areas/Admin/Controllers/CategoriesController.cs` — category CRUD, cycle detection for parent assignment, slug uniqueness checks.
- `Controllers/ProductsController.cs` — storefront list and details.

---

## 8. UI & front-end components
- Styling: uses Bootstrap classes in views.
- Category sidebar component: `Views/Shared/Components/Categories/Default.cshtml` + `_CategoryNode.cshtml` partial — renders hierarchical categories with indentation and active highlight.
- Admin product/category edit forms use hierarchical category selects populated by `PopulateCategoriesFilter` or view component.
- Client scripts: `wwwroot/js/category-tree-selector.js` for category selection interactions.

UX notes:
- Active category highlighting, clear CTAs, and friendly validation messages are present; accessibility improvements recommended.

---

## 9. Services, background jobs & integrations
- Hosted services registered via `AddHostedService<>`:
  - `SubscriptionExpiryService` — manages subscription expirations.
  - `UserStatusCleanupService` — cleans old user status entries.
  - `OrderAutoDeclineService` — auto-declines orders when conditions met.
- Email: `IEmailSender` implemented by `SmtpEmailSender`.
- SignalR: `Hubs/ChatHub` registered and a custom `IUserIdProvider` implemented (`NameUserIdProvider`).

---

## 10. API endpoints
- `Controllers/Api/CategoriesApiController.cs` provides JSON endpoints for categories used by client-side scripts.
- Other API controllers follow a similar pattern (search `Controllers/Api`).

---

## 11. Testing
- Unit tests: `tests/UnitTests/CategoriesControllerTests.cs` — tests cycle detection logic using EF InMemory.
- Integration tests: placeholders under `tests/IntegrationTests/`.

Run tests: `dotnet test` at solution level.

---

## 12. Build, run & deployment
Local dev steps:
1. Install .NET 8 SDK.
2. Configure `appsettings.Development.json` `ConnectionStrings:DbConnection`.
3. Restore packages: `dotnet restore`.
4. Build: `dotnet build`.
5. Apply migrations: `dotnet ef database update`.
6. Run: `dotnet run`.

Production guidance:
- Use environment-specific settings and secure secrets (Key Vault, environment vars).
- Use a reverse proxy (IIS, Nginx) and enable HTTPS.
- Backup DB before migrations.

---

## 13. Operational guidance
- Monitoring & logging: ensure `ILogger` sinks are configured (files or telemetry).
- Health checks: add `MapHealthChecks` for readiness/liveness.
- SignalR scale-out: use Redis backplane for multiple instances.
- Migration runbook: test on staging, backup production DB, run `dotnet ef database update` during maintenance window.

---

## 14. File map (important files)
- `Program.cs` — app startup
- `Infrastructure/DataContext.cs` — EF Core DbContext & seeding
- `Infrastructure/DbSeeder.cs` — additional seeding
- `Models/Category.cs`, `Models/Product.cs` — domain models
- `Areas/Admin/Controllers/CategoriesController.cs`
- `Infrastructure/Components/CategoriesViewComponent.cs`
- `Views/Shared/Components/Categories/Default.cshtml`
- `Views/Shared/Components/Categories/_CategoryNode.cshtml`
- `Migrations/*` — EF migrations (incl. `AddUniqueIndexOnCategorySlug`)
- `wwwroot/js/category-tree-selector.js`
- `Controllers/Api/CategoriesApiController.cs`
- `tests/UnitTests/CategoriesControllerTests.cs`

---

## 15. Troubleshooting
- Migration fails due to duplicate slugs: migration `AddUniqueIndexOnCategorySlug` runs a dedupe SQL first; verify DB backup and review changed slugs.
- Category dropdown empty: verify `PopulateCategoriesFilter` is registered and services added in `Program.cs`.
- Missing static content: verify `wwwroot` structure and `WebRootPath` permissions.
- Identity/email issues: ensure SMTP configuration in `appsettings` or secret store.

---

## 16. Recommended improvements (prioritized)
1. Production hardening: stricter Identity password policy, account lockout, MFA for admin accounts.
2. Migrations safety: create a pre-migration audit log and admin preview UI for data-changing migrations.
3. Add health checks, metrics, and centralized logging (AppInsights/ELK).
4. Improve accessibility of category tree (keyboard navigation, ARIA roles).
5. Replace deleted Seller category pages with a friendly redirect or a request-based workflow for sellers to propose categories.
6. Add integration tests that execute migrations on a disposable SQL container (Docker) to validate data-change migrations.

---

### Contact / Ownership
Primary repository and branch: `https://github.com/itshunaid/CMSECommerce_Latest` — branch `WorkingOnStore_Categories`.
For questions, open an issue or submit a PR with tests for proposed changes.

---

End of document.
