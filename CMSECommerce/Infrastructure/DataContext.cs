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

        public DbSet<SubscriptionRequest> SubscriptionRequests { get; set; }
        public DbSet<SubscriptionTier> SubscriptionTiers { get; set; }
        



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
     new Product { Id = 1, Name = "Apples", Slug = "apples", Description = "Juicy apples", Price = 1.50M, CategoryId = 2, Image = "apple1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 100, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 2, Name = "Grapefruit", Slug = "grapefruit", Description = "Juicy grapefruit", Price = 2M, CategoryId = 2, Image = "grapefruit1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 80, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 3, Name = "Grapes", Slug = "grapes", Description = "Fresh grapes", Price = 1.80M, CategoryId = 2, Image = "grapes1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 150, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 4, Name = "Oranges", Slug = "oranges", Description = "Fresh oranges", Price = 1.50M, CategoryId = 2, Image = "orange1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 120, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 5, Name = "Blue shirt", Slug = "blue-shirt", Description = "Nice blue t-shirt", Price = 7.99M, CategoryId = 1, Image = "blue1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 45, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 6, Name = "Red shirt", Slug = "red-shirt", Description = "Nice red t-shirt", Price = 8.99M, CategoryId = 1, Image = "red1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 30, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 7, Name = "Green shirt", Slug = "green-shirt", Description = "Nice green t-shirt", Price = 9.99M, CategoryId = 1, Image = "green1.png", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 25, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
     new Product { Id = 8, Name = "Pink shirt", Slug = "pink-shirt", Description = "Nice pink t-shirt", Price = 10.99M, CategoryId = 1, Image = "pink1.png", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 10, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" }
 );


            //            modelBuilder.Entity<Product>().HasData(
            //    // 1-8 Original set updated with your credentials
            //    new Product { Id = 1, Name = "Apples", Slug = "apples", Description = "Juicy apples", Price = 1.50M, CategoryId = 2, Image = "apple1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 100, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 2, Name = "Grapefruit", Slug = "grapefruit", Description = "Juicy grapefruit", Price = 2M, CategoryId = 2, Image = "grapefruit1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 80, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 3, Name = "Grapes", Slug = "grapes", Description = "Fresh grapes", Price = 1.80M, CategoryId = 2, Image = "grapes1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 150, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 4, Name = "Oranges", Slug = "oranges", Description = "Fresh oranges", Price = 1.50M, CategoryId = 2, Image = "orange1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 120, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 5, Name = "Blue shirt", Slug = "blue-shirt", Description = "Nice blue t-shirt", Price = 7.99M, CategoryId = 1, Image = "blue1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 45, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 6, Name = "Red shirt", Slug = "red-shirt", Description = "Nice red t-shirt", Price = 8.99M, CategoryId = 1, Image = "red1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 30, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 7, Name = "Green shirt", Slug = "green-shirt", Description = "Nice green t-shirt", Price = 9.99M, CategoryId = 1, Image = "green1.png", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 25, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 8, Name = "Pink shirt", Slug = "pink-shirt", Description = "Nice pink t-shirt", Price = 10.99M, CategoryId = 1, Image = "pink1.png", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 10, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },

            //    // 9-20 Food & Groceries
            //    new Product { Id = 9, Name = "Banana", Slug = "banana", Description = "Organic bananas", Price = 0.99M, CategoryId = 2, Image = "banana.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 200, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 10, Name = "Mango", Slug = "mango", Description = "Sweet Alphonso mango", Price = 3.50M, CategoryId = 2, Image = "mango.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 50, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 11, Name = "Pineapple", Slug = "pineapple", Description = "Tropical pineapple", Price = 2.50M, CategoryId = 2, Image = "pine.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 60, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 12, Name = "Strawberry", Slug = "strawberry", Description = "Fresh strawberries", Price = 4.00M, CategoryId = 2, Image = "straw.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 40, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 13, Name = "Watermelon", Slug = "watermelon", Description = "Big juicy watermelon", Price = 5.00M, CategoryId = 2, Image = "water.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 20, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 14, Name = "Kiwi", Slug = "kiwi", Description = "Green kiwi fruit", Price = 1.20M, CategoryId = 2, Image = "kiwi.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 100, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 15, Name = "Pear", Slug = "pear", Description = "Sweet pear", Price = 1.40M, CategoryId = 2, Image = "pear.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 90, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 16, Name = "Cherry", Slug = "cherry", Description = "Red cherries", Price = 6.00M, CategoryId = 2, Image = "cherry.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 30, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 17, Name = "Avocado", Slug = "avocado", Description = "Ripe avocado", Price = 2.20M, CategoryId = 2, Image = "avo.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 55, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 18, Name = "Lemon", Slug = "lemon", Description = "Zesty lemon", Price = 0.50M, CategoryId = 2, Image = "lemon.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 300, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 19, Name = "Peach", Slug = "peach", Description = "Soft peach", Price = 1.90M, CategoryId = 2, Image = "peach.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 75, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 20, Name = "Plum", Slug = "plum", Description = "Sweet plum", Price = 1.10M, CategoryId = 2, Image = "plum.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 85, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },

            //    // 21-35 Clothing & Accessories
            //    new Product { Id = 21, Name = "Black Hoodie", Slug = "black-hoodie", Description = "Warm black hoodie", Price = 25.00M, CategoryId = 1, Image = "hoodie1.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 50, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 22, Name = "Jeans", Slug = "jeans", Description = "Blue denim jeans", Price = 40.00M, CategoryId = 1, Image = "jeans.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 40, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 23, Name = "Leather Jacket", Slug = "leather-jacket", Description = "Cool leather jacket", Price = 99.00M, CategoryId = 1, Image = "leather.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 15, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 24, Name = "Sneakers", Slug = "sneakers", Description = "White sneakers", Price = 55.00M, CategoryId = 1, Image = "sneaker.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 25, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 25, Name = "Sun Hat", Slug = "sun-hat", Description = "Beach sun hat", Price = 15.00M, CategoryId = 1, Image = "hat.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 100, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 26, Name = "Wool Scarf", Slug = "wool-scarf", Description = "Winter scarf", Price = 12.00M, CategoryId = 1, Image = "scarf.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 60, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 27, Name = "Dress Shoes", Slug = "dress-shoes", Description = "Formal shoes", Price = 80.00M, CategoryId = 1, Image = "shoes.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 20, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 28, Name = "Silk Tie", Slug = "silk-tie", Description = "Red silk tie", Price = 18.00M, CategoryId = 1, Image = "tie.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 45, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 29, Name = "Yoga Pants", Slug = "yoga-pants", Description = "Stretchable pants", Price = 30.00M, CategoryId = 1, Image = "yoga.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 70, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 30, Name = "Cotton Socks", Slug = "cotton-socks", Description = "Pack of 5 socks", Price = 10.00M, CategoryId = 1, Image = "socks.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 150, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 31, Name = "Winter Coat", Slug = "winter-coat", Description = "Heavy coat", Price = 120.00M, CategoryId = 1, Image = "coat.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 10, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 32, Name = "Baseball Cap", Slug = "baseball-cap", Description = "Team cap", Price = 20.00M, CategoryId = 1, Image = "cap.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 80, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 33, Name = "Belt", Slug = "belt", Description = "Brown leather belt", Price = 22.00M, CategoryId = 1, Image = "belt.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 40, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 34, Name = "Shorts", Slug = "shorts", Description = "Summer cargo shorts", Price = 28.00M, CategoryId = 1, Image = "shorts.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 55, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 35, Name = "Gloves", Slug = "gloves", Description = "Warm leather gloves", Price = 35.00M, CategoryId = 1, Image = "gloves.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 30, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },

            //    // 36-49 Electronics & Home Office
            //    new Product { Id = 36, Name = "USB Cable", Slug = "usb-cable", Description = "Fast charging cable", Price = 5.00M, CategoryId = 3, Image = "usb.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 500, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 37, Name = "Headphones", Slug = "headphones", Description = "Noise cancelling", Price = 150.00M, CategoryId = 3, Image = "head.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 25, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 38, Name = "Wireless Mouse", Slug = "wireless-mouse", Description = "Ergonomic mouse", Price = 25.00M, CategoryId = 3, Image = "mouse.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 100, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 39, Name = "Keyboard", Slug = "keyboard", Description = "Mechanical keyboard", Price = 75.00M, CategoryId = 3, Image = "kb.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 45, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 40, Name = "Laptop Stand", Slug = "laptop-stand", Description = "Aluminum stand", Price = 35.00M, CategoryId = 3, Image = "stand.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 60, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 41, Name = "Desk Lamp", Slug = "desk-lamp", Description = "LED desk lamp", Price = 20.00M, CategoryId = 4, Image = "lamp.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 80, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 42, Name = "Water Bottle", Slug = "water-bottle", Description = "Steel bottle", Price = 15.00M, CategoryId = 4, Image = "bottle.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 200, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 43, Name = "Notebook", Slug = "notebook", Description = "A5 lined notebook", Price = 8.00M, CategoryId = 4, Image = "note.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 300, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 44, Name = "Coffee Mug", Slug = "coffee-mug", Description = "Ceramic mug", Price = 12.00M, CategoryId = 4, Image = "mug.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 120, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 45, Name = "Power Bank", Slug = "power-bank", Description = "10000mAh bank", Price = 45.00M, CategoryId = 3, Image = "pb.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 50, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 46, Name = "Phone Case", Slug = "phone-case", Description = "Silicone case", Price = 10.00M, CategoryId = 3, Image = "case.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 250, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 47, Name = "Backpack", Slug = "backpack", Description = "Travel backpack", Price = 65.00M, CategoryId = 1, Image = "pack.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 40, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 48, Name = "Sunglasses", Slug = "sunglasses", Description = "Polarized lens", Price = 50.00M, CategoryId = 1, Image = "sun.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 35, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" },
            //    new Product { Id = 49, Name = "Webcam", Slug = "webcam", Description = "1080p HD Webcam", Price = 40.00M, CategoryId = 3, Image = "webcam.jpg", OwnerName = "CUSTOMER", Status = ProductStatus.Approved, StockQuantity = 30, UserId = "10cad2a7-b708-4ab3-b0f0-79e8e628f932" }
            //);



            modelBuilder.Entity<Page>().HasData(
                new Page { Id = 1, Title = "Home", Slug = "home", Body = "This is the home page" },
                new Page { Id = 2, Title = "About", Slug = "about", Body = "This is the about page" },
                new Page { Id = 3, Title = "Services", Slug = "services", Body = "This is the services page" },
                new Page { Id = 4, Title = "Contact", Slug = "contact", Body = "This is the contact page" }
            );
        }
    }
}