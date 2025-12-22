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

// Database Configuration
var connectionString = builder.Configuration.GetConnectionString("DbConnection");
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

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

var app = builder.Build();

// --- 2. SEEDING LOGIC ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        context.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure Roles exist
        var roles = new[] { "Admin", "Customer", "Subscriber" };
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }
        }

        // Ensure Admin User exists
        var adminEmail = "admin@local.local";
        var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = "admin",
                Email = adminEmail,
                EmailConfirmed = true
            };
            var createResult = userManager.CreateAsync(adminUser, "Pass@local110").GetAwaiter().GetResult();
            if (createResult.Succeeded)
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
            }
        }

        // Ensure Admin Profile and Store exist
        if (adminUser != null)
        {
            var adminProfile = context.UserProfiles.Include(p => p.Store).FirstOrDefault(p => p.UserId == adminUser.Id);
            if (adminProfile == null)
            {
                var adminStore = new Store
                {
                    StoreName = "Admin Central Store",
                    StreetAddress = "123 Admin HQ, Tech Park",
                    City = "Mumbai",
                    PostCode = "400001",
                    Country = "India",
                    Email = adminEmail,
                    Contact = "022-7654321",
                    GSTIN = "27AAAAA0000A1Z5",
                    UserId=adminProfile.User.Id
                };
                context.Stores.Add(adminStore);
                context.SaveChanges();

                adminProfile = new UserProfile
                {
                    UserId = adminUser.Id,
                    FirstName = "System",
                    LastName = "Administrator",
                    ITSNumber = "30455623",
                    Profession = "Global Admin",
                    About = "System generated administrator profile.",
                    IsProfileVisible = true,
                    IsImageApproved = true,
                    StoreId = adminStore.Id,
                    ProfileImagePath = "/images/defaults/admin-profile.png"
                };
                context.UserProfiles.Add(adminProfile);
                context.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred during seeding.");
    }
}

// --- 3. MIDDLEWARE PIPELINE (CORRECT ORDER) ---

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// UseStaticFiles MUST come before Routing to serve images efficiently
app.UseStaticFiles();

app.UseRouting();

// Localization should be before Authentication but after Routing
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

// Area Route
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

// Custom Specialized Routes
app.MapControllerRoute(name: "product", pattern: "products/product/{slug?}", defaults: new { controller = "Products", action = "Product" });
app.MapControllerRoute(name: "cart", pattern: "cart/{action}/{id?}", defaults: new { controller = "Cart", action = "Index" });
app.MapControllerRoute(name: "account", pattern: "account/{action}", defaults: new { controller = "Account", action = "Index" });
app.MapControllerRoute(name: "orders", pattern: "orders/{action}", defaults: new { controller = "Orders", action = "Index" });
app.MapControllerRoute(name: "products", pattern: "products/{slug?}", defaults: new { controller = "Products", action = "Index" });

// Default and Slug Routes
app.MapControllerRoute(name: "default", pattern: "{controller=Products}/{action=Index}/{id?}");
app.MapControllerRoute(name: "pages", pattern: "{slug?}", defaults: new { controller = "Pages", action = "Index" });

app.Run();