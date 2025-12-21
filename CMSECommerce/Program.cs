using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models; // Added to access UserProfile
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IUserService, UserService>();

// Razor Pages (for Admin area settings page)
builder.Services.AddRazorPages();

// Add SignalR services for real-time communication
builder.Services.AddSignalR();

// Add DbContext with the SQL Server provider
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

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<DataContext>()
    .AddDefaultTokenProviders();

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

// Register custom SignalR IUserIdProvider
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, CMSECommerce.Services.NameUserIdProvider>();

// Bind UserStatus options
builder.Services.Configure<CMSECommerce.Services.UserStatusOptions>(builder.Configuration.GetSection("UserStatus"));

// Register the user status service used by multiple pages
builder.Services.AddScoped<CMSECommerce.Services.IUserStatusService, CMSECommerce.Services.UserStatusService>();

// Background cleanup service to mark stale users offline
builder.Services.AddHostedService<CMSECommerce.Services.UserStatusCleanupService>();

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

// --- SEEDING LOGIC ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<DataContext>();
        context.Database.Migrate();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Ensure roles exist
        var roles = new[] { "Admin", "Customer", "Subscriber" };
        foreach (var role in roles)
        {
            if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
            {
                roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
            }
        }

        // Ensure admin user exists
        var adminEmail = "admin@local.local";
        var adminUserName = "admin";
        var adminPassword = "Pass@local110";

        var adminUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
        if (adminUser == null)
        {
            adminUser = new IdentityUser
            {
                UserName = adminUserName,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var createResult = userManager.CreateAsync(adminUser, adminPassword).GetAwaiter().GetResult();
            if (createResult.Succeeded)
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
            }
        }
        else
        {
            if (!userManager.IsInRoleAsync(adminUser, "Admin").GetAwaiter().GetResult())
            {
                userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
            }
        }

        // --- NEW: Seed UserProfile for Admin ---
        if (adminUser != null)
        {
            var adminProfile = context.UserProfiles.FirstOrDefault(p => p.UserId == adminUser.Id);
            if (adminProfile == null)
            {
                adminProfile = new UserProfile
                {
                    UserId = adminUser.Id,
                    FirstName = "System",
                    LastName = "Administrator",
                    ITSNumber = "ADMIN-001",
                    Profession = "Global Admin",
                    About = "System generated administrator profile.",
                    IsProfileVisible = true,
                    IsImageApproved = true,

                    // Address Mapping
                    HomeAddress = "123 Admin HQ, Tech Park",
                    HomePhoneNumber = "022-1234567",
                    BusinessAddress = "Main Office Tower, Floor 10",
                    BusinessPhoneNumber = "022-7654321",
                    WhatsAppNumber = "910000000000",

                    // Defaults
                    ProfileImagePath = "/images/defaults/admin-profile.png",
                    ServicesProvided = "System Management, Support"
                };
                context.UserProfiles.Add(adminProfile);
                context.SaveChanges();
            }
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database or seeding data.");
    }
}

app.UseSession();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Hub and Razor Pages mapping
app.MapHub<CMSECommerce.Hubs.ChatHub>("/chatHub");
app.MapRazorPages();

// --- ROUTE CONFIGURATION ---
app.MapControllerRoute(
 name: "areas",
 pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
 name: "product",
 pattern: "products/product/{slug?}",
 defaults: new { controller = "Products", action = "Product" });

app.MapControllerRoute(
 name: "cart",
 pattern: "cart/{action}/{id?}",
 defaults: new { controller = "Cart", action = "Index" });

app.MapControllerRoute(
 name: "account",
 pattern: "account/{action}",
 defaults: new { controller = "Account", action = "Index" });

app.MapControllerRoute(
 name: "orders",
 pattern: "orders/{action}",
 defaults: new { controller = "Orders", action = "Index" });

app.MapControllerRoute(
 name: "products",
 pattern: "products/{slug?}",
 defaults: new { controller = "Products", action = "Index" });

app.MapControllerRoute(
 name: "default",
 pattern: "{controller=Products}/{action=Index}/{id?}");

app.MapControllerRoute(
 name: "pages",
 pattern: "{slug?}",
 defaults: new { controller = "Pages", action = "Index" });

app.Run();