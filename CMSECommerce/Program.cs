using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Infrastructure.Filters;
using CMSECommerce.Models;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICES CONFIGURATION ---
// Register the PopulateCategoriesFilter so it can be injected as a service-based filter
builder.Services.AddScoped<PopulateCategoriesFilter>();

builder.Services.AddControllersWithViews(options =>
{
    // Add the categories filter as a service filter so ViewBag.Categories is populated for controller views
    options.Filters.AddService<PopulateCategoriesFilter>();
});
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Database Configuration (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DbConnection")
    ?? throw new InvalidOperationException("Connection string 'DbConnection' not found.");

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString)
);

// Identity Configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    // This tells Identity to validate the user against the database 
    // on every single request. Use a small value like 1 second if 
    // you want "immediate" effect.
    options.ValidationInterval = TimeSpan.FromSeconds(0);
});



builder.Services.AddHostedService<SubscriptionExpiryService>();

builder.Services.AddDistributedMemoryCache();



builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});



builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// External Authentication Providers
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["Authentication:Facebook:AppId"];
        options.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    })
    .AddMicrosoftAccount(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Microsoft:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Microsoft:ClientSecret"];
    })
    .AddLinkedIn(options =>
    {
        options.ClientId = builder.Configuration["Authentication:LinkedIn:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:LinkedIn:ClientSecret"];
    });

builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireDigit = false;
    options.User.RequireUniqueEmail = true;
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperAdmin", policy => policy.RequireRole("SuperAdmin"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Subscriber", policy => policy.RequireRole("Subscriber"));
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
});

ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

// Custom Services
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, CMSECommerce.Services.NameUserIdProvider>();
builder.Services.Configure<CMSECommerce.Services.UserStatusOptions>(builder.Configuration.GetSection("UserStatus"));
builder.Services.AddScoped<CMSECommerce.Services.IUserStatusService, CMSECommerce.Services.UserStatusService>();
builder.Services.AddHostedService<CMSECommerce.Services.UserStatusCleanupService>();
builder.Services.AddHostedService<CMSECommerce.Services.OrderAutoDeclineService>();
builder.Services.AddScoped<IAuditService, AuditService>();

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

var app = builder.Build();

// --- 2. DATABASE INITIALIZATION ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();

    try
    {
        context.Database.Migrate();
        DbSeeder.SeedData(app.Services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

// --- 3. MIDDLEWARE PIPELINE ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

var culture = new CultureInfo("en-IN");
var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(culture),
    SupportedCultures = new[] { culture },
    SupportedUICultures = new[] { culture }
};
app.UseRequestLocalization(localizationOptions);

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// --- 4. ROUTE MAPPING ---
app.MapHub<CMSECommerce.Hubs.ChatHub>("/chatHub");
app.MapRazorPages();

// Specific route for SuperAdmin to map /SuperAdmin to Dashboard/Index
app.MapControllerRoute(
    name: "SuperAdmin_default",
    pattern: "SuperAdmin",
    defaults: new { area = "SuperAdmin", controller = "Dashboard", action = "Index" });

// Area Registration
app.MapAreaControllerRoute(
    name: "SuperAdmin",
    areaName: "SuperAdmin",
    pattern: "SuperAdmin/{controller=Dashboard}/{action=Index}/{id?}");

app.MapAreaControllerRoute(
    name: "Admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

// Specific Area Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area}/{controller=Home}/{action=Index}/{id?}");

// Specialized Routes
// Using {**slug} (catch-all) to allow slashes in the product slug (e.g., chocolate-cake-1/2-kg)
app.MapControllerRoute(name: "product", pattern: "products/product/{**slug}", defaults: new { controller = "Products", action = "Product" });
app.MapControllerRoute(name: "cart", pattern: "cart/{action}/{id?}", defaults: new { controller = "Cart", action = "Index" });
app.MapControllerRoute(name: "account", pattern: "account/{action}", defaults: new { controller = "Account", action = "Index" });
app.MapControllerRoute(name: "orders", pattern: "orders/{action}", defaults: new { controller = "Orders", action = "Index" });
app.MapControllerRoute(name: "products", pattern: "products/{slug?}", defaults: new { controller = "Products", action = "Index" });

// --- ADDED STORE SPECIFIC ROUTE TO ENSURE OPTIONAL ID WORKS ---
app.MapControllerRoute(
    name: "storefront",
    pattern: "storefront/{id?}",
    defaults: new { controller = "Products", action = "StoreFront" });

// Generic Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Slug-based Page Route
app.MapControllerRoute(name: "pages", pattern: "{slug?}", defaults: new { controller = "Pages", action = "Index" });

app.Run();