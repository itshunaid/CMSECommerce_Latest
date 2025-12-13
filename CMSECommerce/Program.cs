using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add SignalR services for real-time communication
builder.Services.AddSignalR();

//Add your DbContext with the SQLite provider
var connectionString = builder.Configuration.GetConnectionString("DbConnection");
// ✅ CORRECT: This line tells EF Core to use the SQLite provider.
builder.Services.AddDbContext<DataContext>(options =>
 options.UseSqlite(connectionString)
);
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
 options.IdleTimeout = TimeSpan.FromMinutes(30);
 options.Cookie.IsEssential = true;
});

builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<DataContext>().AddDefaultTokenProviders();

// configure cookie paths for access denied and login
builder.Services.ConfigureApplicationCookie(options =>
{
 options.LoginPath = "/Account/Login";
 options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.Configure<IdentityOptions>(options =>
{
 options.Password.RequiredLength =4;
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

// register email sender
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

// Register custom SignalR IUserIdProvider
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, CMSECommerce.Services.NameUserIdProvider>();

builder.Services.Configure<RouteOptions>(options =>
{
 options.LowercaseUrls = true;
});

var app = builder.Build();

// Set default culture to en-IN so currency formatting uses the Indian rupee symbol
var culture = new CultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var supportedCultures = new[] { culture };
var localizationOptions = new RequestLocalizationOptions
{
 DefaultRequestCulture = new RequestCulture(culture),
 SupportedCultures = supportedCultures.ToList(),
 SupportedUICultures = supportedCultures.ToList()
};
app.UseRequestLocalization(localizationOptions);

// Apply migrations and ensure Identity roles exist at startup
using (var scope = app.Services.CreateScope())
{
 var services = scope.ServiceProvider;
 try
 {
 // Apply any pending migrations (creates database if it doesn't exist)
 var context = services.GetRequiredService<DataContext>();
 context.Database.Migrate();

 // Ensure roles exist after migrations are applied
 var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
 // ⭐ CHANGE MADE HERE: Added "Subscriber" to the roles array
 var roles = new[] { "Admin", "Customer", "Subscriber" };
 foreach (var role in roles)
 {
 var exists = roleManager.RoleExistsAsync(role).GetAwaiter().GetResult();
 if (!exists)
 {
 roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
 }
 }

 // Ensure admin user exists
 var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
 var adminEmail = "admin@local.local";
 var adminUserName = "admin";
 var adminPassword = "Pass@local110";

 var admin = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
 if (admin == null)
 {
 admin = new IdentityUser
 {
 UserName = adminUserName,
 Email = adminEmail,
 EmailConfirmed = true
 };

 var createResult = userManager.CreateAsync(admin, adminPassword).GetAwaiter().GetResult();
 if (createResult.Succeeded)
 {
 userManager.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
 }
 else
 {
 var logger = services.GetRequiredService<ILogger<Program>>();
 logger.LogError("Failed to create admin user: {Errors}", string.Join('|', createResult.Errors.Select(e => e.Description)));
 }
 }
 else
 {
 // Ensure user is in Admin role
 var inRole = userManager.IsInRoleAsync(admin, "Admin").GetAwaiter().GetResult();
 if (!inRole)
 {
 userManager.AddToRoleAsync(admin, "Admin").GetAwaiter().GetResult();
 }
 }
 }
 catch (Exception ex)
 {
 var logger = services.GetRequiredService<ILogger<Program>>();
 logger.LogError(ex, "An error occurred while migrating the database or creating roles/users.");
 }
}

app.UseSession();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Home/Error");
 // The default HSTS value is30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
 app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Register SignalR hub mapping
app.MapHub<CMSECommerce.Hubs.ChatHub>("/chatHub");

// ----------------------------------------------------------------------
// ROUTE CONFIGURATION CHANGES
// ----------------------------------------------------------------------

//1. Area Routes (Highest Priority)
app.MapControllerRoute(
 name: "areas",
 pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

//2. Specific Controller Routes (MUST come before the generic/default routes)

// Route for product details (e.g., /products/product-slug-name)
app.MapControllerRoute(
 name: "product",
 pattern: "products/product/{slug?}",
 defaults: new { controller = "Products", action = "Product" });

// Route for cart actions (e.g., /cart/add)
app.MapControllerRoute(
 name: "cart",
 pattern: "cart/{action}/{id?}",
 defaults: new { controller = "Cart", action = "Index" });

// Route for account actions (e.g., /account/login)
app.MapControllerRoute(
 name: "account",
 pattern: "account/{action}",
 defaults: new { controller = "Account", action = "Index" });

// Route for orders actions (e.g., /orders/history)
app.MapControllerRoute(
 name: "orders",
 pattern: "orders/{action}",
 defaults: new { controller = "Orders", action = "Index" });

// ⭐ CHANGE1: Main Shop Route (Optional, but good practice for clarity)
// This handles /products/{slug} for categories and /products for the main shop.
app.MapControllerRoute(
 name: "products",
 pattern: "products/{slug?}",
 defaults: new { controller = "Products", action = "Index" });


//3. Default MVC Route (Lowest Priority Standard Route)
// ⭐ CHANGE2: Sets the Products controller as the default landing page.
app.MapControllerRoute(
 name: "default",
 pattern: "{controller=Products}/{action=Index}/{id?}");

//4. CMS Page Route (Catch-all - MUST BE LAST)
// This will now only catch requests that don't match Areas, Cart, Account, Orders, or Products.
app.MapControllerRoute(
 name: "pages",
 pattern: "{slug?}",
 defaults: new { controller = "Pages", action = "Index" });

app.Run();