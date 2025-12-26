using CMSECommerce.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Infrastructure
{
    public static class DbSeeder
    {
        public static void SeedData(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DataContext>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

            // 1. Run Migrations
            context.Database.Migrate();

            // 2. Seed Roles
            var roles = new[] { "Admin", "Customer", "Subscriber" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                }
            }

            // 3. Seed Admin User
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
                var result = userManager.CreateAsync(adminUser, "Pass@local110").GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
                }
            }
            else
            {
                // ✅ ENSURE EXISTING ADMIN IS IN ROLE
                var isInRole = userManager.IsInRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
                if (!isInRole)
                {
                    userManager.AddToRoleAsync(adminUser, "Admin").GetAwaiter().GetResult();
                }
            }

            // 4. Seed Admin Store and Profile
            if (adminUser != null)
            {
                // Refresh adminUser check after possible creation/role update
                var adminProfile = context.UserProfiles.FirstOrDefault(p => p.UserId == adminUser.Id);

                if (adminProfile == null)
                {
                    var adminStore = context.Stores.FirstOrDefault(s => s.UserId == adminUser.Id);

                    if (adminStore == null)
                    {
                        adminStore = new Store
                        {
                            StoreName = "Admin Central Store",
                            StreetAddress = "123 Admin HQ, Tech Park",
                            City = "Mumbai",
                            PostCode = "400001",
                            Country = "India",
                            Email = adminEmail,
                            Contact = "022-7654321",
                            GSTIN = "27AAAAA0000A1Z5",
                            UserId = adminUser.Id
                        };
                        context.Stores.Add(adminStore);
                        context.SaveChanges();
                    }

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
    }
}