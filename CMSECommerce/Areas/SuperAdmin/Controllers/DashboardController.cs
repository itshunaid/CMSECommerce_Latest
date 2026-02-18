using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Areas.SuperAdmin.Models;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Services;

namespace CMSECommerce.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public DashboardController(DataContext context, UserManager<IdentityUser> userManager,
            IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        private string GetDatabaseSize()
        {
            try
            {
                var dbPath = _context.Database.GetDbConnection().DataSource;
                if (string.IsNullOrEmpty(dbPath) || !System.IO.File.Exists(dbPath))
                {
                    return "Unknown";
                }
                var fileInfo = new System.IO.FileInfo(dbPath);
                double sizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                return $"{sizeInMB:F2} MB";
            }
            catch
            {
                return "Unknown";
            }
        }

        public async Task<IActionResult> Index()
        {
            // Initialize the model with default (zero) values
            var model = new SuperAdminDashboardViewModel
            {
                UsersCount = 0,
                ProductsRequestCount = 0,
                ProductsCount = 0,
                OrdersCount = 0,
                PendingSubscriberRequests = 0,
                Categories = 0,
                RecentOrders = new List<Order>(),
                UserProfilesCount = 0,
                DeactivatedStoresCount = 0,
                PendingUnlockRequests = 0,
                TotalAdmins = 0,
                TotalSubscribers = 0,
                TotalCustomers = 0,
                TotalStores = 0,
                TotalSubscriptionTiers = 0,
                TotalReviews = 0,
                TotalChatMessages = 0,
                TotalUnlockRequests = 0,
                TotalUserAgreements = 0,
                TotalUserStatuses = 0,
                TotalUserStatusSettings = 0,
                LastMigrationDate = DateTime.Now, // Placeholder, ideally query from migrations
                DatabaseSize = GetDatabaseSize(), // Get database file size
                ActiveUsersLast24Hours = 0, // Placeholder, no login tracking
                FailedLoginAttempts = 0, // Placeholder, no tracking
                RecentAdminActivities = new List<AdminActivity>(), // Placeholder, no audit table
                RecentAuditLogs = new List<AuditLog>() // Recent audit activities
            };

            try
            {
                // Base metrics from AdminDashboardViewModel
                model.UsersCount = await _userManager.Users.CountAsync();
                model.ProductsCount = await _context.Products.CountAsync();
                model.ProductsRequestCount = await _context.Products
                    .Where(p => p.Status == ProductStatus.Pending || p.Status == ProductStatus.Rejected)
                    .CountAsync();
                model.OrdersCount = await _context.Orders.CountAsync();
                model.Categories = await _context.Categories.CountAsync();
                model.UserProfilesCount = await _context.UserProfiles
                    .Where(p => !p.IsImageApproved && p.ProfileImagePath != null)
                    .CountAsync();
                model.PendingSubscriberRequests = await _context.SubscriberRequests
                    .CountAsync(r => r.Approved == false);
                model.DeactivatedStoresCount = await _context.Stores
                    .CountAsync(s => !s.IsActive);
                model.RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync();
                model.PendingUnlockRequests = await _context.UnlockRequests
                    .CountAsync(r => r.Status == "Pending");

                // Sellers with declines - assign to model property
                model.SellersWithDeclines = await _context.OrderDetails
                    .Where(od => od.IsCancelled == true)
                    .GroupBy(od => od.ProductOwner)
                    .Select(g => new CMSECommerce.Areas.Admin.Models.SellerDeclineSummary
                    {
                        SellerName = g.Key ?? "Unknown Seller",
                        ManualDeclines = g.Count(od => od.CancelledByRole == "Seller"),
                        AutoDeclines = g.Count(od => od.CancelledByRole == "System"),
                        TotalDeclines = g.Count()
                    })
                    .OrderByDescending(s => s.TotalDeclines)
                    .Take(10)
                    .ToListAsync();

                // Additional SuperAdmin metrics
                // Assuming roles: Admin, Subscriber, etc. TotalCustomers = total users - admins - subscribers
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                model.TotalAdmins = adminUsers.Count;
                var subscriberUsers = await _userManager.GetUsersInRoleAsync("Subscriber");
                model.TotalSubscribers = subscriberUsers.Count;
                model.TotalCustomers = model.UsersCount - model.TotalAdmins - model.TotalSubscribers;

                model.TotalStores = await _context.Stores.CountAsync();
                model.TotalSubscriptionTiers = await _context.SubscriptionTiers.CountAsync();
                model.TotalReviews = await _context.Reviews.CountAsync();
                model.TotalChatMessages = await _context.ChatMessages.CountAsync();
                model.TotalUnlockRequests = await _context.UnlockRequests.CountAsync();
                model.TotalUserAgreements = await _context.UserAgreements.CountAsync();
                model.TotalUserStatuses = await _context.UserStatuses.CountAsync();
                model.TotalUserStatusSettings = await _context.UserStatusSettings.CountAsync();

                // LastMigrationDate: Query from __EFMigrationsHistory if possible
                var lastMigration = await _context.Database.SqlQueryRaw<string>("SELECT TOP 1 MigrationId AS Value FROM __EFMigrationsHistory ORDER BY MigrationId DESC").FirstOrDefaultAsync();
                if (!string.IsNullOrEmpty(lastMigration))
                {
                    // Try to parse the migration ID (format: YYYYMMDDHHMMSS_Name)
                    if (lastMigration.Length >= 14)
                    {
                        var datePart = lastMigration.Substring(0, 14);
                        if (DateTime.TryParseExact(datePart, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var migrationDate))
                        {
                            model.LastMigrationDate = migrationDate;
                        }
                        else
                        {
                            model.LastMigrationDate = DateTime.Now;
                        }
                    }
                    else
                    {
                        model.LastMigrationDate = DateTime.Now;
                    }
                }

                // ActiveUsersLast24Hours: Approximate with recent orders (handle null OrderDate)
                model.ActiveUsersLast24Hours = await _context.Orders
                    .Where(o => o.OrderDate.HasValue && o.OrderDate >= DateTime.Now.AddHours(-24))
                    .Select(o => o.UserId)
                    .Distinct()
                    .CountAsync();

                // Recent audit logs
                model.RecentAuditLogs = await _context.AuditLogs
                    .Include(a => a.User)
                    .OrderByDescending(a => a.Timestamp)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading dashboard statistics. Data displayed may be incomplete.";
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> TestSmtpConnection()
        {
            try
            {
                var smtpSettings = _configuration.GetSection("EmailSettings");
                var smtpServer = smtpSettings["SmtpServer"];
                var smtpPort = smtpSettings["SmtpPort"];
                var senderEmail = smtpSettings["SenderEmail"] ?? smtpSettings["Username"];
                var senderPassword = smtpSettings["SenderPassword"];

                if (string.IsNullOrEmpty(smtpServer) || string.IsNullOrEmpty(smtpPort) ||
                    string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    return Json(new { success = false, message = "SMTP settings are not properly configured." });
                }

                // Create a test message
                var message = new MimeKit.MimeMessage();
                message.From.Add(new MimeKit.MailboxAddress("Test", senderEmail));
                message.To.Add(MimeKit.MailboxAddress.Parse(senderEmail));
                message.Subject = "SMTP Connectivity Test";

                var bodyBuilder = new MimeKit.BodyBuilder
                {
                    HtmlBody = "<p>This is a test email to verify SMTP connectivity.</p>"
                };
                message.Body = bodyBuilder.ToMessageBody();

                var host = smtpServer;
                var port = int.TryParse(smtpPort, out var p) ? p : 587;

                // Use the same retry and fallback logic for testing
                await TestSmtpConnectionAsync(message, host, port, senderEmail, senderPassword);

                return Json(new { success = true, message = "SMTP connection test successful!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"SMTP connection test failed: {ex.Message}" });
            }
        }

        private async Task TestSmtpConnectionAsync(MimeKit.MimeMessage message, string host, int port,
            string username, string password)
        {
            var maxRetries = 3;
            var baseDelayMs = 1000; // 1 second

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    using var client = new MailKit.Net.Smtp.SmtpClient();
                    client.Timeout = 30000; // 30s

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    // Try StartTLS first (port 587)
                    try
                    {
                        await client.ConnectAsync(host, port, MailKit.Security.SecureSocketOptions.StartTls, cts.Token);
                    }
                    catch (Exception ex) when (attempt == 0) // Only try fallback on first attempt
                    {
                        // Fallback to SSL on port 465
                        try
                        {
                            await client.ConnectAsync(host, 465, MailKit.Security.SecureSocketOptions.SslOnConnect, cts.Token);
                        }
                        catch (Exception sslEx)
                        {
                            throw new Exception("Failed to connect to SMTP server using both StartTLS and SSL", sslEx);
                        }
                    }

                    await client.AuthenticateAsync(username, password, cts.Token);

                    // For testing, we don't actually send the email, just verify connection and auth
                    await client.DisconnectAsync(true);

                    return; // Success, exit retry loop
                }
                catch (TimeoutException)
                {
                    throw; // Don't retry timeout exceptions
                }
                catch (MailKit.Security.AuthenticationException)
                {
                    throw; // Don't retry authentication failures
                }
                catch (Exception ex) when (attempt < maxRetries - 1)
                {
                    // Exponential backoff for transient errors
                    var delayMs = baseDelayMs * Math.Pow(2, attempt);
                    await Task.Delay((int)delayMs);
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
    }
}
