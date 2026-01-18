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
            var roles = new[] { "SuperAdmin", "Admin", "Customer", "Subscriber" };
            foreach (var role in roles)
            {
                if (!roleManager.RoleExistsAsync(role).GetAwaiter().GetResult())
                {
                    roleManager.CreateAsync(new IdentityRole(role)).GetAwaiter().GetResult();
                }
            }

            // 3. Seed SuperAdmin User
            var superAdminEmail = "superadmin@local.local";
            var superAdminUser = userManager.FindByEmailAsync(superAdminEmail).GetAwaiter().GetResult();

            if (superAdminUser == null)
            {
                superAdminUser = new IdentityUser
                {
                    UserName = "superadmin",
                    Email = superAdminEmail,
                    EmailConfirmed = true
                };
                var result = userManager.CreateAsync(superAdminUser, "Super@local110").GetAwaiter().GetResult();
                if (result.Succeeded)
                {
                    userManager.AddToRoleAsync(superAdminUser, "SuperAdmin").GetAwaiter().GetResult();
                }
            }
            else
            {
                // ✅ ENSURE EXISTING SUPERADMIN IS IN ROLE
                var isInRole = userManager.IsInRoleAsync(superAdminUser, "SuperAdmin").GetAwaiter().GetResult();
                if (!isInRole)
                {
                    userManager.AddToRoleAsync(superAdminUser, "SuperAdmin").GetAwaiter().GetResult();
                }
            }

            // 4. Seed Admin User
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

            // 4. Seed Categories
            if (!context.Categories.Any())
            {
                // =========================
                // LEVEL 0 – HEADINGS
                // =========================
                var headings = new List<Category>
    {
        new() { Name = "Apparel & Fashion", Slug = "apparel-fashion", Level = 0 },
        new() { Name = "Food & Refreshments", Slug = "food-refreshments", Level = 0 },
        new() { Name = "Home & Industry", Slug = "home-industry", Level = 0 },
        new() { Name = "Health & Care", Slug = "health-care", Level = 0 },
        new() { Name = "Professional Services", Slug = "professional-services", Level = 0 },
        new() { Name = "Gems & Jewelry", Slug = "gems-jewelry", Level = 0 },
        new() { Name = "Gifts & Stationery", Slug = "gifts-stationery", Level = 0 }
    };

                context.Categories.AddRange(headings);
                context.SaveChanges();

                // =========================
                // LEVEL 1 – CATEGORIES
                // =========================
                Category H(string slug) => context.Categories.First(c => c.Slug == slug);

                var categories = new List<Category>
    {
        new() { Name = "Clothing & Accessories", Slug = "clothing-accessories", Level = 1, ParentId = H("apparel-fashion").Id },
        new() { Name = "Dawoodi Bohra Specialty", Slug = "dawoodi-bohra-specialty", Level = 1, ParentId = H("apparel-fashion").Id },
        new() { Name = "Leather Products", Slug = "leather-products", Level = 1, ParentId = H("apparel-fashion").Id },
        new() { Name = "Textiles", Slug = "textiles", Level = 1, ParentId = H("apparel-fashion").Id },

        new() { Name = "Bakery & Confectionery", Slug = "bakery-confectionery", Level = 1, ParentId = H("food-refreshments").Id },
        new() { Name = "Staples", Slug = "staples", Level = 1, ParentId = H("food-refreshments").Id },
        new() { Name = "Specialty Foods", Slug = "specialty-foods", Level = 1, ParentId = H("food-refreshments").Id },

        new() { Name = "Construction & Garden", Slug = "construction-garden", Level = 1, ParentId = H("home-industry").Id },

        new() { Name = "Beauty & Skincare", Slug = "beauty-skincare", Level = 1, ParentId = H("health-care").Id },
        new() { Name = "Wellness", Slug = "wellness", Level = 1, ParentId = H("health-care").Id },

        new() { Name = "Education", Slug = "education", Level = 1, ParentId = H("professional-services").Id },
        new() { Name = "Catering", Slug = "catering", Level = 1, ParentId = H("professional-services").Id },

        new() { Name = "Precious Stones", Slug = "precious-stones", Level = 1, ParentId = H("gems-jewelry").Id },
        new() { Name = "Traditional Jewelry", Slug = "traditional-jewelry", Level = 1, ParentId = H("gems-jewelry").Id },

        new() { Name = "Office Supplies", Slug = "office-supplies", Level = 1, ParentId = H("gifts-stationery").Id },
        new() { Name = "Gifts", Slug = "gifts", Level = 1, ParentId = H("gifts-stationery").Id }
    };

                context.Categories.AddRange(categories);
                context.SaveChanges();

                // =========================
                // LEVEL 2 – SUB-CATEGORIES
                // =========================
                Category C(string slug) => context.Categories.First(c => c.Slug == slug);

                var subCategories = new List<Category>
    {
        new() { Name = "Men's Clothing", Slug = "mens-clothing", Level = 2, ParentId = C("clothing-accessories").Id },
        new() { Name = "Women's Clothing", Slug = "womens-clothing", Level = 2, ParentId = C("clothing-accessories").Id },
        new() { Name = "Kid's Clothing", Slug = "kids-clothing", Level = 2, ParentId = C("clothing-accessories").Id },

        new() { Name = "Rida", Slug = "rida", Level = 2, ParentId = C("dawoodi-bohra-specialty").Id },
        new() { Name = "Jodi", Slug = "jodi", Level = 2, ParentId = C("dawoodi-bohra-specialty").Id },
        new() { Name = "Topi", Slug = "topi", Level = 2, ParentId = C("dawoodi-bohra-specialty").Id },
        new() { Name = "Masallah", Slug = "masallah", Level = 2, ParentId = C("dawoodi-bohra-specialty").Id },

        new() { Name = "Belts", Slug = "belts", Level = 2, ParentId = C("leather-products").Id },
        new() { Name = "Wallets", Slug = "wallets", Level = 2, ParentId = C("leather-products").Id },
        new() { Name = "Handbags", Slug = "handbags", Level = 2, ParentId = C("leather-products").Id },
        new() { Name = "Laptop Bags", Slug = "laptop-bags", Level = 2, ParentId = C("leather-products").Id }
        // (remaining food / other subcategories can be added the same way)
    };

                context.Categories.AddRange(subCategories);
                context.SaveChanges();
            }


            // 5. Seed Admin Store and Profile
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