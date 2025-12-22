using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Models;

namespace CMSECommerce.Infrastructure
{
    public class DataContext(DbContextOptions<DataContext> options) : IdentityDbContext<IdentityUser>(options)
    {
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. RELATIONSHIP CONFIGURATION ---

            // IdentityUser <-> UserProfile (One-to-One)
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.User)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // IdentityUser <-> Store (One-to-One)
            modelBuilder.Entity<Store>()
                .HasOne(s => s.User)
                .WithOne()
                .HasForeignKey<Store>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // UserProfile <-> Store (One-to-One)
            modelBuilder.Entity<UserProfile>()
                .HasOne(up => up.Store)
                .WithOne()
                .HasForeignKey<UserProfile>(up => up.StoreId)
                .OnDelete(DeleteBehavior.Restrict);

            // --- 2. UNIQUE INDEXES FOR MULTI-LOGIN ---

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.HasIndex(up => up.ITSNumber).IsUnique();
                entity.HasIndex(up => up.WhatsAppNumber).IsUnique();
            });

            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasIndex(s => s.Email).IsUnique();
                entity.HasIndex(s => s.Contact).IsUnique();
                entity.HasIndex(s => s.GSTIN).IsUnique();
            });

            // PhoneNumber unique for IdentityUser (only if not null)
            modelBuilder.Entity<IdentityUser>()
                .HasIndex(u => u.PhoneNumber)
                .IsUnique()
                .HasFilter("[PhoneNumber] IS NOT NULL");

            // --- 3. CHAT MESSAGES CONFIGURATION ---

            modelBuilder.Entity<ChatMessage>(b =>
            {
                b.ToTable("ChatMessages");
                b.HasKey(x => x.Id);
                b.Property(x => x.MessageContent).IsRequired();
                b.Property(x => x.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                b.Property(x => x.IsRead).HasDefaultValue(false);
                b.HasIndex(x => new { x.RecipientId, x.IsRead });
            });

            // --- 4. SEED DATA ---

            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Shirts", Slug = "shirts" },
                new Category { Id = 2, Name = "Fruit", Slug = "fruit" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Apples", Slug = "apples", Description = "Juicy apples", Price = 1.50M, CategoryId = 2, Image = "apple1.jpg", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 100 },
                new Product { Id = 2, Name = "Grapefruit", Slug = "grapefruit", Description = "Juicy grapefruit", Price = 2M, CategoryId = 2, Image = "grapefruit1.jpg", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 80 },
                new Product { Id = 3, Name = "Grapes", Slug = "grapes", Description = "Fresh grapes", Price = 1.80M, CategoryId = 2, Image = "grapes1.jpg", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 150 },
                new Product { Id = 4, Name = "Oranges", Slug = "oranges", Description = "Fresh oranges", Price = 1.50M, CategoryId = 2, Image = "orange1.jpg", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 120 },
                new Product { Id = 5, Name = "Blue shirt", Slug = "blue-shirt", Description = "Nice blue t-shirt", Price = 7.99M, CategoryId = 1, Image = "blue1.jpg", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 45 },
                new Product { Id = 6, Name = "Red shirt", Slug = "red-shirt", Description = "Nice red t-shirt", Price = 8.99M, CategoryId = 1, Image = "red1.jpg", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 30 },
                new Product { Id = 7, Name = "Green shirt", Slug = "green-shirt", Description = "Nice green t-shirt", Price = 9.99M, CategoryId = 1, Image = "green1.png", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 25 },
                new Product { Id = 8, Name = "Pink shirt", Slug = "pink-shirt", Description = "Nice pink t-shirt", Price = 10.99M, CategoryId = 1, Image = "pink1.png", OwnerId = "admin", Status = ProductStatus.Approved, StockQuantity = 10 }
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