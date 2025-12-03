# CMSECommerce — User Guide

This document describes how to run and use the CMSECommerce web application.

## Overview
CMSECommerce is an ASP.NET Core (Razor Pages / MVC) storefront targeted at .NET8. Key features:
- Public product catalog (public product listing shows only approved products)
- Customer registration, login, and order placement
- Admin area to manage products, users and orders
- SMTP email notifications for product approval/rejection and other events

## Quick start (local)
1. Prerequisites
 - .NET8 SDK
 - SQL Server / LocalDB reachable from the app
 - Update `ConnectionStrings:DbConnection` in `appsettings.json` (or use environment variables)
 - Optional: SMTP settings in `appsettings.json` under `Smtp:` if you want email sending

2. Build & run
 - From project root: `dotnet build` then `dotnet run` (or run from Visual Studio)
 - App runs at `https://localhost:{port}`; Visual Studio shows the port in the debug toolbar

3. First-time initialization
 - On startup the app applies EF migrations and ensures roles exist. A default admin account is created if missing:
 - Email: `admin@local.local`
 - Username: `admin`
 - Password: `Pass@local110`
 - Roles created by startup: `Admin`, `Customer`.

## Roles and capabilities
- Admin
 - Manage products (create/edit/delete, approve/reject)
 - Manage users
 - Manage orders (view details, toggle shipped)
- Customer
 - Register/login
 - Browse catalog and place orders

## Key pages / controllers
- Public product listing and details
 - `Controllers/ProductsController.cs` (actions: `Index`, `Product`)
- Account / authentication
 - `Controllers/AccountController.cs` (actions: `Register`, `Login`, `Logout`, `OrderDetails`)
- Admin area
 - `Areas/Admin/Controllers/ProductsController.cs` — product CRUD + Approve/Reject
 - `Areas/Admin/Controllers/OrdersController.cs` — list, details, shipped status
 - `Areas/Admin/Controllers/UsersController.cs` — manage users

## Product approval workflow
- Admin approves products via the admin product list (`/admin/products`)
- Approve: sets `Product.Status = Approved` and clears rejection reason
- Reject: admin enters a rejection reason in a modal; the owner receives an email (if SMTP configured)
- Public product listing filters to only show approved products

## Email (SMTP)
- Configure SMTP in `appsettings.json` or environment variables:
 - `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:From`, `Smtp:EnableSsl`
- Implementation: `Infrastructure/SmtpEmailSender.cs` — logs and gracefully handles errors

## Database & migrations
- Use EF Core migrations in the `Migrations/` folder
- Create migration: `dotnet ef migrations add <Name>`
- Apply migration: `dotnet ef database update`
- Note: a migration `DiscardSubscriber` was added that drops subscriber-related tables when subscriber support was removed

## Admin tasks (how-to)
- Sign in as admin via `/account/login` with the seeded admin or a created admin
- Manage products: `/admin/products` — Create/Edit/Delete. Use Approve/Reject buttons to control public visibility
- Orders: `/admin/orders` — view orders and click Details to see full line items. Toggle shipped status inline.
- Users: `/admin/users` — manage roles and lock/unlock users

## Customer tasks
- Register: `/account/register` — customers are assigned the `Customer` role on registration
- Login: `/account/login`
- Catalog: `/products`
- Place order using cart; order history available under `/account`

## Access / Authorization
- App configures roles and policies in `Program.cs`
- Access denied path: `/Account/AccessDenied` (friendly view exists)

## Troubleshooting
-403 Access Denied: confirm user roles in `AspNetUserRoles` table and ensure the user signs out & signs in to refresh claims after role changes
- SMTP failures: check SMTP credentials and logs from `SmtpEmailSender`
- Images missing: ensure files exist under `wwwroot/media/products` and `wwwroot/media/gallery`
- Migration errors: inspect EF migrations and run `dotnet ef database update`; back up DB before destructive changes

## Developer notes
- UI files: `Areas/Admin/Views` holds admin views; `Views/Shared` has layout and nav
- Models: `Models/Product.cs` contains `ProductStatus` and `RejectionReason`
- Email sender: `Infrastructure/SmtpEmailSender.cs`

## FAQ
- Q: Why isn't my product visible publicly?
 - A: It must be approved by an Admin (Status == Approved).
- Q: Where to change SMTP credentials?
 - A: In `appsettings.json` under `Smtp:` or environment variables.

---
Update this README when you change features or flows.
