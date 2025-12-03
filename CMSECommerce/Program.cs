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

//builder.Services.AddDbContext<DataContext>(options =>
//{
//    options.UseSqlServer(builder.Configuration["ConnectionStrings:DbConnection"]);
//});


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

// register email sender
builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();

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

// ... after app.UseAuthorization();

// 1. Area Routes (Highest Priority)
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// 2. Specific Controller Routes (MUST come before the generic/default routes)
app.MapControllerRoute(
    name: "product",
    pattern: "products/product/{slug?}",
    defaults: new { controller = "Products", action = "Product" });
// ... other specific product routes ...

app.MapControllerRoute(
    name: "cart",
    pattern: "cart/{action}/{id?}",
    defaults: new { controller = "Cart", action = "Index" });

// ⭐ CRUCIAL: The account route must be placed here.
app.MapControllerRoute(
    name: "account",
    pattern: "account/{action}",
    defaults: new { controller = "Account", action = "Index" });

app.MapControllerRoute(
    name: "orders",
    pattern: "orders/{action}",
    defaults: new { controller = "Orders", action = "Index" });

// 3. Default MVC Route (Handles / and /Home/Index)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 4. CMS Page Route (Catch-all - MUST BE LAST)
app.MapControllerRoute(
    name: "pages",
    pattern: "{slug?}",
    defaults: new { controller = "Pages", action = "Index" });

app.Run();