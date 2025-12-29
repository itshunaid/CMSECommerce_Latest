using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
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

builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

var app = builder.Build();

// --- 2. SEEDING LOGIC (ENVIRONMENT SENSITIVE) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<DataContext>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    try
    {
        context.Database.Migrate();

        // A. Seed Roles
        var roles = new[] { "Admin", "Customer", "Subscriber" };
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }
        }

        // B. Seed Admin User, Store, and Profile
        var adminEmail = "admin@local.local";
        var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();

        if (adminUser != null)
        {
            // 1. Ensure Role is assigned even if user exists
            if (!userManager.IsInRoleAsync(adminUser, "Admin").GetAwaiter().GetResult())
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
            }

            // 2. Check if profile exists to decide if we need to reset password/seed profile
            var existingProfile = context.UserProfiles.FirstOrDefault(p => p.UserId == adminUser.Id);
            if (existingProfile == null)
            {
                // Profile missing: Force password reset to the standard seeding password
                var resetToken = userManager.GeneratePasswordResetTokenAsync(adminUser).GetAwaiter().GetResult();
                userManager.ResetPasswordAsync(adminUser, resetToken, "Pass@local110").GetAwaiter().GetResult();

                // Allow the logic to fall through or handle profile creation here
            }
        }
        else
        {
            // User does not exist at all: Create new
            adminUser = new IdentityUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = userManager.CreateAsync(adminUser, "Pass@local110").GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();

                // Create the Store
                var adminStore = new Store
                {
                    UserId = adminUser.Id,
                    StoreName = "Admin Central Store",
                    Email = adminEmail,
                    City = "Mumbai",
                    Country = "India"
                };
                context.Stores.Add(adminStore);
                context.SaveChanges();

                // Create the UserProfile
                var adminProfile = new UserProfile
                {
                    UserId = adminUser.Id,
                    StoreId = adminStore.Id,
                    FirstName = "System",
                    LastName = "Admin",
                    ITSNumber = "000000",
                    IsProfileVisible = true,
                    CurrentProductLimit = 1000,
                    SubscriptionStartDate = DateTime.Now
                };
                context.UserProfiles.Add(adminProfile);
                context.SaveChanges();
            }
        }

        // C. Seed Subscription Tiers
        if (!context.SubscriptionTiers.Any())
        {
            context.SubscriptionTiers.AddRange(
                new SubscriptionTier { Name = "Basic", Price = 500, DurationMonths = 6, ProductLimit = 25 },
                new SubscriptionTier { Name = "Intermediate", Price = 900, DurationMonths = 12, ProductLimit = 50 },
                new SubscriptionTier { Name = "Premium", Price = 1500, DurationMonths = 12, ProductLimit = 120 }
            );
            context.SaveChanges();
        }

        // D. Developer Seeder
        if (app.Environment.IsDevelopment())
        {
            DbSeeder.SeedData(app.Services);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during database seeding.");
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
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(name: "areas", pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");
app.MapControllerRoute(name: "product", pattern: "products/product/{slug?}", defaults: new { controller = "Products", action = "Product" });
app.MapControllerRoute(name: "cart", pattern: "cart/{action}/{id?}", defaults: new { controller = "Cart", action = "Index" });
app.MapControllerRoute(name: "account", pattern: "account/{action}", defaults: new { controller = "Account", action = "Index" });
app.MapControllerRoute(name: "orders", pattern: "orders/{action}", defaults: new { controller = "Orders", action = "Index" });
app.MapControllerRoute(name: "products", pattern: "products/{slug?}", defaults: new { controller = "Products", action = "Index" });
app.MapControllerRoute(name: "default", pattern: "{controller=Products}/{action=Index}/{id?}");
app.MapControllerRoute(name: "pages", pattern: "{slug?}", defaults: new { controller = "Pages", action = "Index" });

app.Run();