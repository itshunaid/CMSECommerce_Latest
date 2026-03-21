# CMSECommerce Project Explanation & Overview

**Project Name:** CMSECommerce (aka Weypaari Platform)  
**Purpose:** Community e-commerce marketplace for ITS networks (Hyderabad/Mumbai). Enables tiered seller subscriptions, product sales, order management, real-time chat, and admin oversight.  

## What It Does (Simple Explanation)
Think of it as a private Flipkart for your community:
- **Buyers (Customers):** Browse/search products → Cart → Buy → Track/Review.
- **Sellers:** Create store → List products (pending admin approval) → Manage orders/ship → Reports.
- **Admins/SuperAdmins:** Approve sellers/products → Monitor audits → Broadcast messages.

**Key Features:**
- Hierarchical categories (e.g., Shirts > T-Shirts).
- Subscription tiers (Trial:5 products → Premium:120).
- Real-time chat (SignalR).
- PDF invoices, Gmail notifications.
- Auto-services: Expire subscriptions, decline old orders, clean user status.

## How It Works (High-Level Flow)
```
Homepage (Categories + Featured) 
↓
Products (Filter/Search/Slug URLs) 
↓
Cart/Checkout → Order DB + Email 
↓
Seller Dashboard: Process/Ship 
↓
Admin: Approve + Audit Logs
Background: Cron jobs (HostedServices)
```

## Tech Stack (For Devs)
- **Backend:** .NET 8 ASP.NET Core MVC + Areas (Admin/Seller/SuperAdmin).
- **DB:** SQL Server (Azure prod; LocalDB dev) + EF Core Migrations/Seeding.
- **Auth:** ASP.NET Identity (Roles) + OAuth (Google/FB).
- **Real-time:** SignalR ChatHub.
- **Other:** MailKit SMTP, Redis cache, Playwright PDFs.

## Getting Started
```
1. dotnet restore
2. dotnet ef database update  # Applies 50+ migrations + seeds admins
3. dotnet run  # https://localhost:7121
Login: weypaari@gmail.com / Pass@local110 (SuperAdmin)
Prod: weypaari.runasp.net
```

## Files Structure (Key Locations)
```
├── Program.cs          # Startup (DI, middleware, routes)
├── DataContext.cs      # Models + EF config/seeding
├── Areas/Admin/        # Dashboard/Users/Products/Orders
├── Areas/Seller/       # Seller products/orders
├── Controllers/        # Public: Products/Cart/Orders
├── Migrations/         # 50+ DB changes (e.g., AuditLogs)
├── Services/           # HostedServices (e.g., OrderAutoDecline)
├── docs/               # Support-Team-Reference.md + this file
├── wwwroot/            # JS/CSS/Images
```

## Production Setup
- Azure App Service + SQL db36483.public.databaseasp.net.
- appsettings.Production.json: Connstr + Gmail SMTP.
- Deploy: VS Publish (WebDeploy package).

## Common Use Cases
1. **New Seller:** /Subscription → Admin approve → Limits apply.
2. **Order Lifecycle:** Cart → Order → Ship (nullable date) → Review.
3. **Troubleshoot:** AuditLogs table + Support-Team-Reference.md scripts.

For details: See CMSECommerce-Handover-Development-Document.md. Extend via new Areas/Controllers + Migrations.

**Contact:** weypaari@gmail.com

