# Audit Logging Implementation TODO

## Step 1: Add AuditLog DbSet to DataContext
- Add `public DbSet<AuditLog> AuditLogs { get; set; }` to Infrastructure/DataContext.cs

## Step 2: Register AuditService in Program.cs
- Add `builder.Services.AddScoped<IAuditService, AuditService>();` in Program.cs services configuration

## Step 3: Create Migration for AuditLog Table
- Run `dotnet ef migrations add AddAuditLogTable` command
- Run `dotnet ef database update` to apply migration

## Step 4: Integrate Audit Logging into Controllers
- Identify key controllers: Areas/Admin/Controllers/UsersController, Areas/SuperAdmin/Controllers/SubscriptionRequestsController, Controllers/ProductsController, Controllers/OrdersController, etc.
- Add audit logging for creation, update, deletion, approval/rejection actions using IAuditService

## Step 5: Test and Verify
- Test audit logging in key workflows
- Verify no existing features are broken
