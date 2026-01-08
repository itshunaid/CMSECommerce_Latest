using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// --- 1. SERVICES CONFIGURATION ---
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddRazorPages();
builder.Services.AddSignalR();

// Database Configuration (SQL Server)
var connectionString = builder.Configuration.GetConnectionString("DbConnection")
    ?? throw new InvalidOperationException("Connection string 'DbConnection' not found.");

builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(connectionString)
);
builder.Services.AddHostedService<SubscriptionExpiryService>();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.IsEssential = true;
});

// Identity Configuration
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
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
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Subscriber", policy => policy.RequireRole("Subscriber"));
    options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
});

// Custom Services
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, CMSECommerce.Services.NameUserIdProvider>();
builder.Services.Configure<CMSECommerce.Services.UserStatusOptions>(builder.Configuration.GetSection("UserStatus"));
builder.Services.AddScoped<CMSECommerce.Services.IUserStatusService, CMSECommerce.Services.UserStatusService>();
builder.Services.AddHostedService<CMSECommerce.Services.UserStatusCleanupService>();
builder.Services.AddHostedService<CMSECommerce.Services.OrderAutoDeclineService>();

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
        if (app.Environment.IsDevelopment())
        {
            DbSeeder.SeedData(app.Services);
        }
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

// Specific Area Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

// Specialized Routes
app.MapControllerRoute(name: "product", pattern: "products/product/{slug?}", defaults: new { controller = "Products", action = "Product" });
app.MapControllerRoute(name: "cart", pattern: "cart/{action}/{id?}", defaults: new { controller = "Cart", action = "Index" });
app.MapControllerRoute(name: "account", pattern: "account/{action}", defaults: new { controller = "Account", action = "Index" });
app.MapControllerRoute(name: "orders", pattern: "orders/{action}", defaults: new { controller = "Orders", action = "Index" });
app.MapControllerRoute(name: "products", pattern: "products/{slug?}", defaults: new { controller = "Products", action = "Index" });

// --- ADDED STORE SPECIFIC ROUTE TO ENSURE OPTIONAL ID WORKS ---
app.MapControllerRoute(
    name: "storefront",
    pattern: "/storefront/{id?}",
    defaults: new { controller = "Products", action = "StoreFront" });

// Generic Default Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Slug-based Page Route
app.MapControllerRoute(name: "pages", pattern: "{slug?}", defaults: new { controller = "Pages", action = "Index" });

app.Run();