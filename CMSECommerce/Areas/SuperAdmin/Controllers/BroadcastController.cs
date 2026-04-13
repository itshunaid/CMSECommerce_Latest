using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Areas.SuperAdmin.Controllers
{
    [Area("SuperAdmin")]
    [Authorize(Roles = "SuperAdmin")]
    public class BroadcastController : Controller
    {
        private readonly DataContext _context;
        private readonly IEmailService _emailService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<BroadcastController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        private readonly IConfiguration _configuration;

        public BroadcastController(
            DataContext context,
            IEmailService emailService,
            UserManager<IdentityUser> userManager,
            ILogger<BroadcastController> logger,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }

        private async Task<List<IdentityUser>> GetActiveSellersAsync()
        {
            // Sellers are users who have an active subscription according to UserProfile
            var now = DateTime.UtcNow;
            var profiles = await _context.UserProfiles
                .Include(p => p.User)
                .Where(p => p.SubscriptionStartDate != null && p.SubscriptionEndDate != null && p.SubscriptionEndDate >= now)
                .AsNoTracking()
                .ToListAsync();

            var users = profiles
                .Where(p => p.User != null)
                .Select(p => p.User)
                .Distinct()
                .ToList();

            return users;
        }

        private async Task<List<IdentityUser>> GetCustomersAsync()
        {
            var now = DateTime.UtcNow;

            // Customers: users who do NOT have an active subscription
            // We'll select user IDs from UserProfiles that either have no subscription or expired subscriptions
            var profiles = await _context.UserProfiles
                .Include(p => p.User)
                .Where(p => p.User != null)
                .AsNoTracking()
                .ToListAsync();

            var customers = profiles
                .Where(p => !(p.SubscriptionStartDate != null && p.SubscriptionEndDate != null && p.SubscriptionEndDate >= now))
                .Where(p => p.User != null)
                .Select(p => p.User)
                .Distinct()
                .ToList();

            return customers;
        }

        /// <summary>
        /// Display broadcast message form
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Get active sellers (users with active subscription)
            var sellers = await GetActiveSellersAsync();
            var sellerList = sellers
                .Select(s => new { id = s.Id, email = s.Email, name = s.UserName })
                .ToList();

            var customers = await GetCustomersAsync();
            var customerList = customers
                .Select(c => new { id = c.Id, email = c.Email, name = c.UserName })
                .ToList();

            ViewBag.Sellers = sellerList;
            ViewBag.Customers = customerList;
            return View();
        }

        /// <summary>
        /// Send broadcast message to sellers
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(
            string subject,
            string body,
            string audience,
            bool sendToAll,
            string selectedIds,
            IFormFile attachmentFile)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                TempData["error"] = "Subject and body are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                    return RedirectToAction(nameof(Index));

                var recipients = new List<IdentityUser>();

                if (audience == "sellers" || audience == "both")
                    recipients.AddRange(await GetActiveSellersAsync());

                if (audience == "customers" || audience == "both")
                    recipients.AddRange(await GetCustomersAsync());

                recipients = recipients.GroupBy(x => x.Id).Select(g => g.First()).ToList();

                if (!sendToAll && !string.IsNullOrWhiteSpace(selectedIds))
                {
                    var ids = selectedIds.Split(',');
                    recipients = recipients.Where(r => ids.Contains(r.Id)).ToList();
                }

                recipients = recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)).ToList();

                if (!recipients.Any())
                {
                    TempData["error"] = "No valid recipients found.";
                    return RedirectToAction(nameof(Index));
                }

                // File Upload
                string attachmentPath = null;
                string attachmentFileName = null;

                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    var folder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "broadcast");
                    Directory.CreateDirectory(folder);

                    attachmentFileName = $"{Guid.NewGuid()}_{attachmentFile.FileName}";
                    attachmentPath = Path.Combine(folder, attachmentFileName);

                    using var stream = new FileStream(attachmentPath, FileMode.Create);
                    await attachmentFile.CopyToAsync(stream);
                }

                // Validate SMTP config before sending
                var smtpSection = _configuration.GetSection("EmailSettings"); // Add IConfiguration to constructor if needed
                if (!smtpSection.Exists() || string.IsNullOrEmpty(smtpSection["SmtpServer"]) || string.IsNullOrEmpty(smtpSection["SenderEmail"]))
                {
                    TempData["error"] = "Email configuration is missing. Check appsettings.json EmailSettings.";
                    return RedirectToAction(nameof(Index));
                }

                // Save Broadcast
                var broadcast = new BroadcastMessage
                {
                    Subject = subject,
                    Body = body,
                    SentByUserId = currentUser.Id,
                    DateSent = DateTime.UtcNow,
                    RecipientCount = recipients.Count,
                    Status = "Sending",
                    AttachmentFileName = attachmentFileName,
                    AttachmentPath = attachmentPath
                };

                _context.BroadcastMessages.Add(broadcast);
                await _context.SaveChangesAsync();

                // Save recipients
                var recipientEntities = recipients.Select(r => new BroadcastRecipient
                {
                    BroadcastMessageId = broadcast.Id,
                    UserId = r.Id,
                    Email = r.Email,
                    Status = "Pending"
                }).ToList();

                _context.BroadcastRecipients.AddRange(recipientEntities);
                await _context.SaveChangesAsync();

                // 🆕 SYNCHRONOUS EMAIL SENDING WITH PROGRESS TRACKING
                await SendBroadcastEmailsAsync(broadcast.Id);

                TempData["success"] = $"Broadcast sent to {recipients.Count} users. Status: {broadcast.Status}.";
                return RedirectToAction(nameof(History));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Broadcast sending failed: {Message}", ex.Message);
                TempData["error"] = $"Failed to send broadcast: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Send emails SYNCHRONOUSLY with retries and detailed tracking
        /// </summary>
        private async Task SendBroadcastEmailsAsync(int broadcastId)
        {
            var broadcast = await _context.BroadcastMessages
                .Include(b => b.Recipients)
                .FirstOrDefaultAsync(b => b.Id == broadcastId);

            if (broadcast == null) 
            {
                _logger.LogWarning("Broadcast {Id} not found", broadcastId);
                return;
            }

            int success = 0;
            int failure = 0;
            var total = broadcast.Recipients.Count;

            _logger.LogInformation("Starting broadcast {Id} to {Total} recipients", broadcastId, total);

            foreach (var r in broadcast.Recipients)
            {
                var attempt = 0;
                const int maxAttempts = 3;
                bool sent = false;

                while (attempt < maxAttempts && !sent)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(broadcast.AttachmentPath))
                        {
                            await _emailService.SendEmailWithAttachmentAsync(r.Email, broadcast.Subject, broadcast.Body, broadcast.AttachmentPath);
                        }
                        else
                        {
                            await _emailService.SendEmailAsync(r.Email, broadcast.Subject, broadcast.Body);
                        }

                        r.Status = "Sent";
                        r.SentAt = DateTime.UtcNow;
                        r.ErrorMessage = null;
                        success++;
                        sent = true;
                        _logger.LogInformation("Email sent to {Email} (attempt {Attempt})", r.Email, attempt + 1);
                    }
                    catch (Exception ex)
                    {
                        attempt++;
                        if (attempt >= maxAttempts)
                        {
                            r.Status = "Failed";
                            r.ErrorMessage = $"{ex.Message} (after {maxAttempts} attempts)";
                            failure++;
                            _logger.LogError(ex, "Email FAILED to {Email} after {Attempts} attempts: {Message}", r.Email, maxAttempts, ex.Message);
                        }
                        else
                        {
                            r.Status = "Retrying";
                            _logger.LogWarning(ex, "Email retry {Attempt}/{Max} for {Email}: {Message}", attempt + 1, maxAttempts, r.Email, ex.Message);
                            await Task.Delay(1000 * attempt); // Backoff
                        }
                    }
                }
            }

            // Update broadcast status with better granularity
            if (failure == 0)
                broadcast.Status = "Sent";
            else if (success == 0)
                broadcast.Status = "Failed";
            else
                broadcast.Status = $"Partial ({success}/{total})";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Broadcast {Id} completed: {Success}/{Total} sent, {Failure} failed", broadcastId, success, total, failure);
        }
        /// <summary>
        /// Get sellers as JSON for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSellers()
        {
            var sellers = await GetActiveSellersAsync();
            var sellerList = sellers.Select(s => new { id = s.Id, email = s.Email, name = s.UserName }).ToList();
            return Json(sellerList);
        }

        /// <summary>
        /// Get customers as JSON for dropdown
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCustomers()
        {
            var customers = await GetCustomersAsync();
            var customerList = customers.Select(c => new { id = c.Id, email = c.Email, name = c.UserName }).ToList();
            return Json(customerList);
        }

        /// <summary>
        /// View broadcast history
        /// </summary>
        public async Task<IActionResult> History()
        {
            var broadcasts = await _context.BroadcastMessages
                .Include(b => b.SentByUser)
                .OrderByDescending(b => b.DateSent)
                .ToListAsync();

            return View(broadcasts);
        }

        /// <summary>
        /// View details of a specific broadcast
        /// </summary>
        public async Task<IActionResult> Details(int id)
        {
            var broadcast = await _context.BroadcastMessages
                .Include(b => b.SentByUser)
                .Include(b => b.Recipients) // ✅ NEW
                .FirstOrDefaultAsync(b => b.Id == id);

            if (broadcast == null)
            {
                return NotFound();
            }

            return View(broadcast);
        }
    }
}
