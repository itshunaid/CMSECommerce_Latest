using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Infrastructure
{
    public class DataContext : IdentityDbContext<IdentityUser>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        public DbSet<UserAgreement> UserAgreements { get; set; }
        public DbSet<Page> Pages { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<UnlockRequest> UnlockRequests { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<SubscriberRequest> SubscriberRequests { get; set; }
        public DbSet<UserStatusTracker> UserStatuses { get; set; }
        public DbSet<UserStatusSetting> UserStatusSettings { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<SubscriptionRequest> SubscriptionRequests { get; set; }
        public DbSet<SubscriptionTier> SubscriptionTiers { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<UnlockRequest>().ToTable("UnlockRequests");

            // ARCHITECTURE: Automatically filter out deactivated stores globally
            modelBuilder.Entity<Store>()
                .HasQueryFilter(s => s.IsActive);

            modelBuilder.Entity<UserProfile>()
        .HasIndex(u => u.ITSNumber)
        .IsUnique();

            modelBuilder.Entity<Category>()
        .HasOne(c => c.Parent)
        .WithMany(c => c.Children)
        .HasForeignKey(c => c.ParentId)
        .OnDelete(DeleteBehavior.Restrict);


            // Ensure a user can have multiple agreement records (as versions change over time)
            modelBuilder.Entity<UserAgreement>()
                .HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId);

            // 1. UserProfile Configuration
            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.Property(u => u.StoreId).IsRequired(false);

                entity.HasOne(u => u.Store)
                      .WithMany()
                      .HasForeignKey(u => u.StoreId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(u => u.User)
                      .WithOne()
                      .HasForeignKey<UserProfile>(u => u.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 2. Product Relationships
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Store)
                      .WithMany(s => s.Products)
                      .HasForeignKey(p => p.StoreId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.NoAction);
            });

            // 3. Chat Message Configuration
            modelBuilder.Entity<ChatMessage>(b =>
            {
                b.ToTable("ChatMessages");
                b.HasKey(x => x.Id);
                b.Property(x => x.MessageContent).IsRequired();
                b.Property(x => x.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                b.Property(x => x.IsRead).HasDefaultValue(false);
                b.HasIndex(x => new { x.RecipientId, x.IsRead });
            });

            // --- 4. IDENTITY SEEDING (Roles & Admin User) ---

            string adminRoleId = "5f90378b-3001-443b-8736-411a91341c2c";
            string customerRoleId = "6f90378b-3001-443b-8736-411a91341c2d";
            string subscriberRoleId = "7f90378b-3001-443b-8736-411a91341c2e";
            string adminUserId = "a18265d3-05b8-4766-adcc-ca43d3960199";

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = customerRoleId, Name = "Customer", NormalizedName = "CUSTOMER" },
                new IdentityRole { Id = subscriberRoleId, Name = "Subscriber", NormalizedName = "SUBSCRIBER" }
            );

            var hasher = new PasswordHasher<IdentityUser>();
            modelBuilder.Entity<IdentityUser>().HasData(new IdentityUser
            {
                Id = adminUserId,
                UserName = "admin",
                NormalizedUserName = "ADMIN",
                Email = "admin@local.local",
                NormalizedEmail = "ADMIN@LOCAL.LOCAL",
                EmailConfirmed = true,
                PasswordHash = hasher.HashPassword(null, "Pass@local110"),
                SecurityStamp = string.Empty
            });

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
            {
                RoleId = adminRoleId,
                UserId = adminUserId
            });

            // --- 5. DOMAIN SEED DATA ---

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Shirts", Slug = "shirts", Level = 0, ParentId = null },
                new Category { Id = 2, Name = "Fruit", Slug = "fruit", Level = 0, ParentId = null },

                // Additional seeded child categories for richer demo data
                new Category { Id = 3, Name = "T-Shirts", Slug = "t-shirts", Level = 1, ParentId = 1 },
                new Category { Id = 4, Name = "Formal Shirts", Slug = "formal-shirts", Level = 1, ParentId = 1 },
                new Category { Id = 5, Name = "Apples", Slug = "apples", Level = 1, ParentId = 2 },
                new Category { Id = 6, Name = "Oranges", Slug = "oranges", Level = 1, ParentId = 2 }
            );

            modelBuilder.Entity<Store>().HasData(
                new Store
                {
                    Id = 1,
                    UserId = adminUserId,
                    StoreName = "Admin Central Store",
                    Email = "admin@local.local",
                    Contact = "0000000000",
                    City = "Mumbai",
                    Country = "India",
                    PostCode = "400001"
                }
            );

           
            modelBuilder.Entity<UserProfile>().HasData(
                new UserProfile
                {
                    Id = 1,
                    UserId = adminUserId,
                    StoreId = 1,
                    FirstName = "System",
                    LastName = "Admin",
                    ITSNumber = "000000",
                    IsProfileVisible = true,
                    CurrentProductLimit = 1000,
                    SubscriptionStartDate = DateTime.Parse("2026-01-01"),
                    WhatsAppNumber = "0000000000",
                    // ADD THIS LINE TO FIX THE ERROR:
                    BusinessAddress = "Main Admin Office, Mumbai",
                    HomeAddress = "Default Admin Home",
                    // Ensure other required fields like 'About' or 'Profession' are also filled if they are not nullable
                    About = "Default System Administrator",
                    Profession = "Administrator"
                }
            );

            modelBuilder.Entity<SubscriptionTier>().HasData(
                new SubscriptionTier { Id = 1, Name = "Trial", Price = 99, DurationMonths = 1, ProductLimit = 5 },
                new SubscriptionTier { Id = 2, Name = "Basic", Price = 499, DurationMonths = 6, ProductLimit = 25 },
                new SubscriptionTier { Id = 3, Name = "Intermediate", Price = 899, DurationMonths = 12, ProductLimit = 50 },
                new SubscriptionTier { Id = 4, Name = "Premium", Price = 1499, DurationMonths = 12, ProductLimit = 120 }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Apples", Slug = "apples", Description = "Juicy apples", Price = 1.50M, CategoryId = 2, Image = "apple1.jpg", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 2, Name = "Grapefruit", Slug = "grapefruit", Description = "Juicy grapefruit", Price = 2M, CategoryId = 2, Image = "grapefruit1.jpg", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 3, Name = "Grapes", Slug = "grapes", Description = "Fresh grapes", Price = 1.80M, CategoryId = 2, Image = "grapes1.jpg", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 4, Name = "Oranges", Slug = "oranges", Description = "Fresh oranges", Price = 1.50M, CategoryId = 2, Image = "orange1.jpg", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 5, Name = "Blue shirt", Slug = "blue-shirt", Description = "Nice blue t-shirt", Price = 7.99M, CategoryId = 1, Image = "blue1.jpg", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 6, Name = "Red shirt", Slug = "red-shirt", Description = "Nice red t-shirt", Price = 8.99M, CategoryId = 1, Image = "red1.jpg", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 7, Name = "Green shirt", Slug = "green-shirt", Description = "Nice green t-shirt", Price = 9.99M, CategoryId = 1, Image = "green1.png", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" },
                new Product { Id = 8, Name = "Pink shirt", Slug = "pink-shirt", Description = "Nice pink t-shirt", Price = 10.99M, CategoryId = 1, Image = "pink1.png", StoreId = 1, Status = ProductStatus.Approved, StockQuantity = 100, UserId = adminUserId, OwnerName = "Admin" }
            );

            modelBuilder.Entity<Page>().HasData(
                new Page { Id = 1, Title = "Home", Slug = "home", Body = "This is the home page" },
                new Page { Id = 2, Title = "About", Slug = "about", Body = "This is the about page" },
                new Page { Id = 3, Title = "Services", Slug = "services", Body = "This is the services page" },
                new Page { Id = 4, Title = "Contact", Slug = "contact", Body = "This is the contact page" }
            );
        }
    }
}