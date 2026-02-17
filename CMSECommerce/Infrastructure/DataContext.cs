using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace CMSECommerce.Infrastructure
{
    public class DataContext : IdentityDbContext<IdentityUser>
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

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
        public DbSet<BroadcastMessage> BroadcastMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // --- 1. CONFIGURATIONS ---
            modelBuilder.Entity<UnlockRequest>().ToTable("UnlockRequests");
            modelBuilder.Entity<Store>().HasQueryFilter(s => s.IsActive);
            modelBuilder.Entity<UserProfile>().HasIndex(u => u.ITSNumber).IsUnique();

            modelBuilder.Entity<Category>()
                .HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserAgreement>()
                .HasOne(ua => ua.User)
                .WithMany()
                .HasForeignKey(ua => ua.UserId);

            modelBuilder.Entity<UserProfile>(entity =>
            {
                entity.Property(u => u.StoreId).IsRequired(false);
                entity.HasOne(u => u.Store).WithMany().HasForeignKey(u => u.StoreId).OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(u => u.User).WithOne().HasForeignKey<UserProfile>(u => u.UserId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasOne(p => p.Store).WithMany(s => s.Products).HasForeignKey(p => p.StoreId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.User).WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ChatMessage>(b =>
            {
                b.ToTable("ChatMessages");
                b.HasKey(x => x.Id);
                b.Property(x => x.MessageContent).IsRequired();
                b.Property(x => x.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");
                b.Property(x => x.IsRead).HasDefaultValue(false);
                b.HasIndex(x => new { x.RecipientId, x.IsRead });
            });

            // --- 2. IDENTITY SEEDING ---
            string adminRoleId = "5f90378b-3001-443b-8736-411a91341c2c";
            string customerRoleId = "6f90378b-3001-443b-8736-411a91341c2d";
            string subscriberRoleId = "7f90378b-3001-443b-8736-411a91341c2e";

            string idHunaid = "h07-8265d3-05b8-4766-adcc-ca43d3960197";
            string adminUserId = "a18265d3-05b8-4766-adcc-ca43d3960199";
            string hussainaUserId = "8e448304-2185-442e-a342-6e210168d87d";
            string idMurtaza = "m01-8265d3-05b8-4766-adcc-ca43d3960191";
            string idAbbas = "a02-8265d3-05b8-4766-adcc-ca43d3960192";
            string idTaherB = "t03-8265d3-05b8-4766-adcc-ca43d3960193";
            string idTaherH = "t04-8265d3-05b8-4766-adcc-ca43d3960194";
            string idYahya = "b72c9184-e4d2-4e5a-9391-7241065162a0";

            // NEW USER IDs
            string idAbdulNew = "ab05-8265d3-05b8-4766-adcc-ca43d3960101";
            string idAlAqmar = "al08-8265d3-05b8-4766-adcc-ca43d3960102";
            string idKhader = "ak09-8265d3-05b8-4766-adcc-ca43d3960103";
            string idKhuzaima = "kh10-8265d3-05b8-4766-adcc-ca43d3960104";



            var hasher = new PasswordHasher<IdentityUser>();

            IdentityUser CreateUser(string id, string user, string email, string phone = null)
            {
                var inputBytes = Encoding.UTF8.GetBytes(id);
                var hashBytes = SHA1.HashData(inputBytes);
                var guidBytes = new byte[16];
                Array.Copy(hashBytes, guidBytes, 16);
                string validGuidStamp = new Guid(guidBytes).ToString();

                var newUser = new IdentityUser
                {
                    Id = id,
                    UserName = user,
                    NormalizedUserName = user.ToUpper(),
                    Email = email,
                    NormalizedEmail = email.ToUpper(),
                    EmailConfirmed = true,
                    PhoneNumber = phone,
                    SecurityStamp = validGuidStamp,
                    ConcurrencyStamp = id
                };
                newUser.PasswordHash = hasher.HashPassword(newUser, "Pass@local110");
                return newUser;
            }

            modelBuilder.Entity<IdentityRole>().HasData(
                new IdentityRole { Id = adminRoleId, Name = "Admin", NormalizedName = "ADMIN" },
                new IdentityRole { Id = customerRoleId, Name = "Customer", NormalizedName = "CUSTOMER" },
                new IdentityRole { Id = subscriberRoleId, Name = "Subscriber", NormalizedName = "SUBSCRIBER" }
            );

            modelBuilder.Entity<IdentityUser>().HasData(
                CreateUser(idHunaid, "weypaari@gmail.com", "weypaari@gmail.com", "9603302152"),
                CreateUser(adminUserId, "admin", "admin@local.local"),
                CreateUser(hussainaUserId, "hussaina", "hussaina@local.local"),
                CreateUser(idMurtaza, "murtazahussain166@gmail.com", "murtazahussain166@gmail.com", "9700081831"),
                CreateUser(idAbbas, "bharmalprojects@gmail.com", "bharmalprojects@gmail.com", "9963107763"),
                CreateUser(idTaherB, "mailbox.taher@gmail.com", "mailbox.taher@gmail.com", "8885216302"),
                CreateUser(idTaherH, "thussain98490@gmail.com", "thussain98490@gmail.com", "9849217820"),
                CreateUser(idYahya, "yaliasger@yahoo.co.in", "yaliasger@yahoo.co.in", "9130211052"),
                    // NEW USERS
                CreateUser(idAbdulNew, "abdulqadirlokhandwalaandwala@gmail.com", "abdulqadirlokhandwalaandwala@gmail.com", "9121835054"),
                CreateUser(idAlAqmar, "alaqmarak0810@gmail.com", "alaqmarak0810@gmail.com", "9618443558"),
                CreateUser(idKhader, "akframes@gmail.com", "akframes@gmail.com", "9949521090"),
                CreateUser(idKhuzaima, "Mohdkhuzaima@gmail.com", "Mohdkhuzaima@gmail.com", "9989664052")
            );

            modelBuilder.Entity<IdentityUserRole<string>>().HasData(
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idHunaid },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = adminUserId },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = hussainaUserId },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idMurtaza },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idAbbas },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idTaherB },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idTaherH },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idYahya },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idAbdulNew },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idAlAqmar },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idKhader },
                new IdentityUserRole<string> { RoleId = adminRoleId, UserId = idKhuzaima }
            );

            // --- 3. DOMAIN DATA ---
            modelBuilder.Entity<Category>().HasData(
                new Category { Id = 1, Name = "Shirts", Slug = "shirts", Level = 0, ParentId = null },
                new Category { Id = 2, Name = "Fruit", Slug = "fruit", Level = 0, ParentId = null },
                new Category { Id = 3, Name = "T-Shirts", Slug = "t-shirts", Level = 1, ParentId = 1 },
                new Category { Id = 4, Name = "Formal Shirts", Slug = "formal-shirts", Level = 1, ParentId = 1 },
                new Category { Id = 5, Name = "Apples", Slug = "apples", Level = 1, ParentId = 2 },
                new Category { Id = 6, Name = "Oranges", Slug = "oranges", Level = 1, ParentId = 2 }
            );

            modelBuilder.Entity<Store>().HasData(
                new Store { Id = 1, UserId = adminUserId, StoreName = "Admin Central Store", Email = "admin@local.local", Contact = "0000000000", City = "Mumbai", Country = "India", PostCode = "400001" }
            );

            modelBuilder.Entity<UserProfile>().HasData(
                new UserProfile { Id = 107, UserId = idHunaid, FirstName = "Weypaari", LastName = "Admin", ITSNumber = "100010", WhatsAppNumber = "9603302152", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 1, UserId = adminUserId, StoreId = 1, FirstName = "System", LastName = "Admin", ITSNumber = "000000", IsProfileVisible = true, CurrentProductLimit = 1000, SubscriptionStartDate = DateTime.Parse("2026-01-01"), WhatsAppNumber = "0000000000", BusinessAddress = "Main Admin Office, Mumbai", HomeAddress = "Default Admin Home", About = "Default System Administrator", Profession = "Administrator" },
                new UserProfile { Id = 100, UserId = idMurtaza, FirstName = "Murtaza", LastName = "Sagarwala", ITSNumber = "100001", WhatsAppNumber = "9700081831", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 101, UserId = idAbbas, FirstName = "Abbas", LastName = "Shajapurwala", ITSNumber = "100002", WhatsAppNumber = "9963107763", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 102, UserId = idTaherB, FirstName = "Taher", LastName = "Bensabwala", ITSNumber = "100003", WhatsAppNumber = "8885216302", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 103, UserId = idTaherH, FirstName = "Taher", LastName = "Hyderabadwala", ITSNumber = "100004", WhatsAppNumber = "9849217820", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 105, UserId = idYahya, FirstName = "Yahya", LastName = "Aliasger", ITSNumber = "100009", WhatsAppNumber = "9130211052", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 110, UserId = idAbdulNew, FirstName = "Abdulqadir", LastName = "Lokhandwala", ITSNumber = "100011", WhatsAppNumber = "9121835054", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 111, UserId = idAlAqmar, FirstName = "AL AQMAR", LastName = "KANCHWALA", ITSNumber = "100012", WhatsAppNumber = "9618443558", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 112, UserId = idKhader, FirstName = "Abdul", LastName = "Khader Patanwala", ITSNumber = "100013", WhatsAppNumber = "9949521090", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 },
                new UserProfile { Id = 113, UserId = idKhuzaima, FirstName = "Khuzaima", LastName = "Saeed", ITSNumber = "100014", WhatsAppNumber = "9989664052", BusinessAddress = "Hyderabad", HomeAddress = "Hyderabad", About = "Admin", Profession = "Admin", CurrentProductLimit = 1000 }
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