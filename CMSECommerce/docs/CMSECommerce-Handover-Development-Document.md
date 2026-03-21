# CMSECommerce Application Handover & Development Document

**Project Name:** CMSECommerce (Weypaari Platform)  
**Version:** 1.0  
**Date:** Current Build (post-Migrations/Seeding Updates)  
**Audience:** Non-technical stakeholders (e.g., business owners, support teams) and technical support engineers/developers  

---

## 1. Executive Summary

CMSECommerce is a comprehensive e-commerce platform designed as a community-driven marketplace ('Weypaari Platform') for sellers, buyers, and administrators. It solves the problem of fragmented online selling/buying by providing tiered seller subscriptions, product management, order processing, and real-time chat—tailored for group-based commerce (e.g., ITS community networks in Hyderabad/Mumbai).

**Core Purpose:** Enable secure, role-based transactions where:
- Customers browse, cart, and purchase products.
- Sellers manage stores, fulfill orders, and scale via subscriptions.
- Admins approve content, monitor audits, and broadcast updates.

**Primary User Workflows:**
- **Customer:** Browse categories/products → Add to cart → Checkout (address/payment) → Track orders → Review products.
- **Seller:** Dashboard (orders/products) → Add/edit products → Process/ship orders → View sales reports.
- **Admin:** Approve subscribers/products → Manage users/orders → View audit logs/broadcast messages.
- **Transaction Example:** Customer buys shirt → Seller ships → Admin monitors via dashboard/audit trail.

The platform supports 1000+ products, real-time chat, PDF invoices, and auto-services (e.g., order decline after 48h). Deployed to Azure (SQL Server), it's production-ready with Gmail SMTP and Google/Facebook auth.

---

## 2. Technical Architecture

**Architectural Pattern:** ASP.NET Core MVC with Areas (Admin/Seller/SuperAdmin), layered as Presentation (Razor Views/Controllers) → Application (Services/CQRS) → Domain (Models) → Infrastructure (EF Core DataContext). Follows Clean Architecture principles with dependency injection, global filters (e.g., PopulateCategoriesFilter), and hosted background services. No full microservices; monolithic but modular via Areas.

**Core Tech Stack:**
| Layer | Technologies |
|-------|--------------|
| **Backend** | .NET 8.0, ASP.NET Core MVC/Razor Pages, MediatR (CQRS), EntityFramework Core 8.0.3 (SQL Server) |
| **Frontend** | Bootstrap, Razor Views/Components (e.g., hierarchical CategoriesViewComponent), SignalR (ChatHub), Playwright (PDF generation via Rotativa) |
| **Database** | SQL Server (Azure: db36483.public.databaseasp.net), Migrations (50+ for entities like AuditLog, BroadcastMessage), Seeding (Admins/Roles/Categories/Products) |
| **Auth/Security** | ASP.NET Identity (Roles: Admin/Subscriber/Customer), External OAuth (Google/Facebook/Microsoft/LinkedIn), Policies (e.g., [Authorize(Roles='SuperAdmin')]), Session (30min timeout) |
| **Services/Integrations** | HostedServices (SubscriptionExpiryService, OrderAutoDeclineService, UserStatusCleanupService), MailKit (SMTP Gmail), StackExchange.Redis (cache), ExcelDataReader (bulk import) |
| **Other** | SignalR (real-time chat with custom IUserIdProvider), AuditService (change tracking) |

**Component Interactions:**
```
User Request → Middleware (HTTPS/Session/Auth) → Controller (e.g., ProductsController) → Service (e.g., AuditService) → DataContext (EF Queries/Migrations) → SQL Server
↳ Views render via Filters/ViewComponents → Static Files (wwwroot)
Background: HostedServices poll/cron (e.g., expire subscriptions) → Email/SignalR notifications
```

Data flows via DbContext (e.g., Products with Store/User FKs, Category hierarchy). AuditLogs track all changes (Entity/Action/Old/New values).

---

## 3. Application Workflow

**Primary Transaction: Customer Order Fulfillment (End-to-End Lifecycle)**

1. **Browse/Search:** User hits `/Products/Index?category=shirts` → PopulateCategoriesFilter loads ViewBag.Categories → Hierarchical sidebar renders → Products query (filtered by active Stores).
2. **View Product:** `/Products/Product/{**slug}` → EF fetches Product + Category/Reviews/Store → Razor View with AddToCart form (session-based CartItem).
3. **Cart/Checkout:** `/Cart` → Session CartItems → Address selection → OrdersController POST → Create Order/OrderDetails → Persist to DB → Email receipt (SmtpEmailSender).
4. **Seller Processing:** Seller dashboard `/Areas/Seller/Orders` → Update OrderDetail.Status/ShippedDate → AuditLog entry → Customer notified (email/chat).
5. **Admin Oversight:** `/Areas/Admin/Orders` → Monitor pending → Approve/refund → Background OrderAutoDeclineService cancels stalled (>48h).
6. **Completion:** Customer reviews → PDF invoice (`/PlaywrightPdfController/Order/{id}`) → Stats update dashboard.

**Error Paths:** Invalid auth → `/Account/AccessDenied`. DB fail → ExceptionHandler logs. All actions audited.

---

## 4. Infrastructure & DevOps

**Hosting Environment:**
- **Production:** Azure App Service (weypaari.runasp.net), SQL Azure (db36483.public.databaseasp.net; connstr in appsettings.Production.json).
- **Development:** Local (launchSettings: https://localhost:7121), LocalDB (CMSECommerce.db).
- **Scaling:** Redis backplane for SignalR, distributed cache/session.

**CI/CD Pipeline:**
- **Build/Test:** `dotnet restore/build/test` (xUnit + EF InMemory).
- **Migrations:** `dotnet ef database update` (auto on startup via Program.cs scoped block).
- **Deploy:** VS Publish (WebDeploy: weypaari.runasp.net-WebDeploy.publishSettings) or Azure CLI. Seeding via DbSeeder.SeedData().
- **Logs/Debugging:** 
  - App: Kudu (Azure portal) → LogFiles → Application.
  - DB: SSMS → AuditLogs table (filter by Entity/Timestamp).
  - Queries: SSMS for health (e.g., `SELECT COUNT(*) FROM Orders WHERE Status='Pending'`).

**Access:**
```
Prod DB: Server=db36483.public.databaseasp.net; User=db36483; PW=G@z38R+b-mW6
SMTP: Gmail (weypaari@gmail.com; app PW: rouw xmpu bnzy ppzs)
Admins: weypaari@gmail.com/Pass@local110 (SuperAdmin)
```

---

## 5. Support & Troubleshooting

**Top 3 Failure Points & Resolutions:**

1. **Login Failures (30% tickets - Invalid PW/Lockout/Email Unconfirmed):**
   | Symptom | Check | Manual Fix (SSMS) |
   |---------|-------|-------------------|
   | Invalid credentials | `SELECT * FROM AspNetUsers WHERE NormalizedEmail='user@ex.com'` | Reset PW: Exec Identity `UserManager.UpdateSecurityStampAsync(user)`. Unlock: `UPDATE AspNetUsers SET LockoutEnd=NULL WHERE Id='{guid}'`. |
   | Time: 2min | Logs: AuditLogs (LoginFailed). | Email confirm: `UPDATE SET EmailConfirmed=1`. |

2. **Stuck Orders (25% - Pending >48h):**
   - Query: `SELECT * FROM Orders WHERE Status='Pending' AND CreatedAt < DATEADD(h,-48,GETDATE())`.
   - Fix: Notify seller → Auto-decline via OrderAutoDeclineService. Manual: `UPDATE Orders SET Status='Cancelled'`.
   - Logs: OrderDetails + AuditLogs. Time: 5min.

3. **Subscription/Product Approval Delays (20%):**
   - Query: `SELECT * FROM SubscriberRequests WHERE Status='Pending'`.
   - Fix: Admin approve → `UPDATE UserProfiles SET CurrentTierId={tier}`. Products: Approve in `/Areas/Admin/Products`.
   - Logs: AuditLogs (filter 'Approved'). Time: 3min.

**General:** Use docs/Support-Team-Reference.md for 50+ scripts/tables. Escalate P1 (downtime) to #devops.

---

## 6. Glossary

- **Area:** Organized folder for role-specific features (e.g., Admin Area = /Admin/Dashboard).
- **DbContext:** Database 'bridge' (DataContext.cs) handling queries/saves.
- **EF Core:** Tool mapping code classes (Models) to DB tables.
- **Hosted Service:** Background task (e.g., auto-cleanup cron jobs).
- **Identity:** User login system (handles PW hash, roles like Admin).
- **Migration:** DB schema update script (e.g., add new column).
- **Razor View:** Template file (.cshtml) mixing HTML/C# for dynamic pages.
- **SignalR:** Real-time chat (like WhatsApp web).
- **Slug:** URL-friendly name (e.g., 'blue-shirt').
- **Tier:** Seller plan (e.g., Basic=25 products).

**Next Steps:** Review TechnicalDocumenta.md for dev extensions; run `dotnet run` locally.

This document ensures seamless handover—platform is stable, audited, and scalable. Contact weypaari@gmail.com for queries.

