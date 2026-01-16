using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Areas.Admin.Models;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Services;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    // It is highly recommended to also inject ILogger<DashboardController> for production logging.
    public class DashboardController : Controller
    {
        private readonly DataContext _context;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public DashboardController(DataContext context, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager,
            IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
        }

        public async Task<IActionResult> Index()
        {
            // Initialize the model with default (zero) values
            var model = new AdminDashboardViewModel
            {
                UsersCount = 0,
                ProductsRequestCount = 0,
                ProductsCount = 0,
                OrdersCount = 0,
                PendingSubscriberRequests = 0,
                Categories = 0,
                RecentOrders = new List<Order>(),
                UserProfilesCount = 0,
                DeactivatedStoresCount = 0 // New property initialized
            };

            try
            {
                // 1. Identity User Count
                model.UsersCount = await _userManager.Users.CountAsync();

                // 2. Total Products
                model.ProductsCount = await _unitOfWork.Repository<Product>().GetAll().CountAsync();

                // 3. Pending/Rejected Product Requests
                model.ProductsRequestCount = await _unitOfWork.Repository<Product>()
                    .Find(p => p.Status == ProductStatus.Pending || p.Status == ProductStatus.Rejected)
                    .CountAsync();

                // 4. Global Order Metrics
                model.OrdersCount = await _unitOfWork.Repository<Order>().GetAll().CountAsync();

                // 5. Taxonomy Metrics
                model.Categories = await _unitOfWork.Repository<Category>().GetAll().CountAsync();

                // 6. Profile Image Moderation Queue
                model.UserProfilesCount = await _unitOfWork.Repository<UserProfile>()
                    .Find(p => !p.IsImageApproved && p.ProfileImagePath != null)
                    .CountAsync();

                // 7. Seller Onboarding Queue
                model.PendingSubscriberRequests = await _unitOfWork.Repository<SubscriberRequest>()
                    .Find(r => r.Approved == false)
                    .CountAsync();

                // 8. NEW: Deactivated Stores Metric (Architecture Churn Metric)
                // We count stores where IsActive is explicitly false
                model.DeactivatedStoresCount = await _unitOfWork.Repository<Store>()
                    .Find(s => !s.IsActive)
                    .CountAsync();

                // 9. Recent Activity Feed
                model.RecentOrders = await _unitOfWork.Repository<Order>()
                    .GetAll()
                    .OrderByDescending(o => o.Id)
                    .Take(5)
                    .ToListAsync();

                // 10. Sellers with Declined Orders
                model.SellersWithDeclines = await _unitOfWork.Repository<OrderDetail>()
                    .Find(od => od.IsCancelled == true)
                    .GroupBy(od => od.ProductOwner)
                    .Select(g => new CMSECommerce.Areas.Admin.Models.SellerDeclineSummary
                    {
                        SellerName = g.Key,
                        ManualDeclines = g.Count(od => od.CancelledByRole == "Seller"),
                        AutoDeclines = g.Count(od => od.CancelledByRole == "System"),
                        TotalDeclines = g.Count()
                    })
                    .OrderByDescending(s => s.TotalDeclines)
                    .Take(10)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // ARCHITECT NOTE: Ensure you have an ILogger injected to capture 'ex' details
                // _logger.LogError(ex, "Error fetching Admin Dashboard stats");

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
