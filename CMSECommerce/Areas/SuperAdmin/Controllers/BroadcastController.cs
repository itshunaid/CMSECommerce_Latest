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

        public BroadcastController(
            DataContext context,
            IEmailService emailService,
            UserManager<IdentityUser> userManager,
            ILogger<BroadcastController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
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
            [FromForm] string subject,
            [FromForm] string body,
            [FromForm] string audience, // "sellers" or "customers" or "both"
            [FromForm] bool sendToAll,
            [FromForm] string selectedIds,
            [FromForm] IFormFile attachmentFile)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                TempData["error"] = "Subject and message body are required.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(audience) || (audience != "sellers" && audience != "customers" && audience != "both"))
            {
                TempData["error"] = "Invalid audience selected.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Robustly interpret checkbox value: HTML checkbox often posts "on" when checked
                var sendToAllRaw = (Request.Form["sendToAll"].FirstOrDefault() ?? string.Empty).ToLowerInvariant();
                bool sendToAllFlag = sendToAll || sendToAllRaw == "on" || sendToAllRaw == "true" || sendToAllRaw == "1";

                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Verify the user exists in the database to ensure foreign key validity
                var dbUser = await _context.Users.FindAsync(currentUser.Id);
                if (dbUser == null)
                {
                    _logger.LogError("Current user {UserId} not found in database context", currentUser.Id);
                    TempData["error"] = "User validation failed. Please log in again.";
                    return RedirectToAction(nameof(Index));
                }


                List<IdentityUser> recipients = new();
                var selectedSellerIds = new List<string>();
                var selectedCustomerIds = new List<string>();

                if (audience == "sellers" || audience == "both")
                {
                    if (sendToAllFlag && audience == "sellers")
                    {
                        recipients = (await GetActiveSellersAsync()).ToList();
                    }
                    else if (sendToAllFlag && audience == "both")
                    {
                        // we'll populate later by combining both lists
                    }
                    else if (!sendToAllFlag && (audience == "sellers" || audience == "both"))
                    {
                        if (string.IsNullOrWhiteSpace(selectedIds))
                        {
                            TempData["error"] = "Please select at least one seller.";
                            return RedirectToAction(nameof(Index));
                        }

                        var ids = selectedIds.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                        foreach (var id in ids)
                        {
                            var user = await _userManager.FindByIdAsync(id);
                            if (user == null) continue;
                            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                            var now = DateTime.UtcNow;
                            // classify
                            if (profile != null && profile.SubscriptionStartDate != null && profile.SubscriptionEndDate != null && profile.SubscriptionEndDate >= now)
                            {
                                if (!selectedSellerIds.Contains(user.Id)) selectedSellerIds.Add(user.Id);
                                if (!recipients.Any(r => r.Id == user.Id)) recipients.Add(user);
                            }
                            else
                            {
                                if (!selectedCustomerIds.Contains(user.Id)) selectedCustomerIds.Add(user.Id);
                                if (!recipients.Any(r => r.Id == user.Id)) recipients.Add(user);
                            }
                        }
                    }
                }

                if (audience == "customers" || audience == "both")
                {
                    if (sendToAllFlag && audience == "customers")
                    {
                        recipients = (await GetCustomersAsync()).ToList();
                    }
                    else if (sendToAllFlag && audience == "both")
                    {
                        // we'll populate later by combining both lists
                    }
                    else if (!sendToAllFlag && audience == "customers")
                    {
                        if (string.IsNullOrWhiteSpace(selectedIds))
                        {
                            TempData["error"] = "Please select at least one customer.";
                            return RedirectToAction(nameof(Index));
                        }

                        var ids = selectedIds.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                        foreach (var id in ids)
                        {
                            var user = await _userManager.FindByIdAsync(id);
                            if (user == null) continue;
                            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                            var now = DateTime.UtcNow;
                            if (profile == null || !(profile.SubscriptionStartDate != null && profile.SubscriptionEndDate != null && profile.SubscriptionEndDate >= now))
                            {
                                if (!selectedCustomerIds.Contains(user.Id)) selectedCustomerIds.Add(user.Id);
                                if (!recipients.Any(r => r.Id == user.Id)) recipients.Add(user);
                            }
                        }
                    }
                }

                // Handle sendToAll for 'both' audience by combining both lists
                if (sendToAllFlag && audience == "both")
                {
                    var sellers = await GetActiveSellersAsync();
                    var customers = await GetCustomersAsync();
                    // combine distinct by Id
                    var combined = sellers.Concat(customers).GroupBy(u => u.Id).Select(g => g.First()).ToList();
                    recipients = combined;
                }

                // Remove recipients without a valid email and log them
                var initialCount = recipients.Count;
                var invalidEmails = new List<string>();
                recipients = recipients.Where(u =>
                {
                    if (string.IsNullOrWhiteSpace(u?.Email))
                    {
                        invalidEmails.Add(u?.Id ?? "<unknown>");
                        return false;
                    }
                    return true;
                }).ToList();

                if (invalidEmails.Any())
                {
                    _logger.LogWarning("Broadcast recipients removed due to missing email addresses: {Ids}", string.Join(',', invalidEmails));
                }

                if (!recipients.Any())
                {
                    TempData["error"] = "No recipients with valid email addresses found for the selected audience.";
                    return RedirectToAction(nameof(Index));
                }

                // Handle attachment upload
                string attachmentPath = null;
                string attachmentFileName = null;

                if (attachmentFile != null && attachmentFile.Length > 0)
                {
                    // Validate file size (max 10 MB)
                    if (attachmentFile.Length > 10 * 1024 * 1024)
                    {
                        TempData["error"] = "File size cannot exceed 10 MB.";
                        return RedirectToAction(nameof(Index));
                    }

                    try
                    {
                        attachmentFileName = Path.GetFileName(attachmentFile.FileName);
                        var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "broadcast-attachments");
                        Directory.CreateDirectory(uploadsFolder);

                        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}_{attachmentFileName}";
                        attachmentPath = Path.Combine(uploadsFolder, uniqueFileName);

                        using var fileStream = new FileStream(attachmentPath, FileMode.Create);
                        await attachmentFile.CopyToAsync(fileStream);

                        _logger.LogInformation("Attachment uploaded: {AttachmentPath}", attachmentPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload attachment");
                        TempData["warning"] = "Attachment upload failed, sending without attachment.";
                        attachmentPath = null;
                        attachmentFileName = null;
                    }
                }

                var broadcast = new BroadcastMessage
                {
                    Subject = subject,
                    Body = body,
                    AttachmentFileName = attachmentFileName,
                    AttachmentPath = attachmentPath,
                    SendToAllSellers = (audience == "sellers" && sendToAllFlag) || (audience == "both" && sendToAllFlag),
                    SelectedSellerIds = selectedSellerIds.Any() ? string.Join(',', selectedSellerIds) : null,
                    SendToAllCustomers = (audience == "customers" && sendToAllFlag) || (audience == "both" && sendToAllFlag),
                    SelectedCustomerIds = selectedCustomerIds.Any() ? string.Join(',', selectedCustomerIds) : null,
                    SentByUserId = currentUser.Id,
                    DateSent = DateTime.UtcNow,
                    RecipientCount = recipients.Count,
                    Status = "Pending"
                };

                _context.BroadcastMessages.Add(broadcast);
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Log detailed information about the failure
                    _logger.LogError(ex, "Failed to save BroadcastMessage entity to database. " +
                        "Subject: {Subject}, Body Length: {BodyLength}, SentByUserId: {SentByUserId}, " +
                        "RecipientCount: {RecipientCount}, Status: {Status}, Audience: {Audience}, " +
                        "SendToAllSellers: {SendToAllSellers}, SendToAllCustomers: {SendToAllCustomers}, " +
                        "SelectedSellerIds: {SelectedSellerIds}, SelectedCustomerIds: {SelectedCustomerIds}",
                        broadcast.Subject,
                        broadcast.Body?.Length ?? 0,
                        broadcast.SentByUserId,
                        broadcast.RecipientCount,
                        broadcast.Status,
                        audience,
                        broadcast.SendToAllSellers,
                        broadcast.SendToAllCustomers,
                        broadcast.SelectedSellerIds,
                        broadcast.SelectedCustomerIds);
                    
                    // Log inner exception if available
                    if (ex.InnerException != null)
                    {
                        _logger.LogError(ex.InnerException, "Inner exception: {InnerMessage}", ex.InnerException.Message);
                    }
                    
                    TempData["error"] = "Failed to save broadcast record. See logs for details.";

                    // Clean up uploaded attachment file if present to avoid orphan files
                    if (!string.IsNullOrEmpty(attachmentPath) && System.IO.File.Exists(attachmentPath))
                    {
                        try { System.IO.File.Delete(attachmentPath); } catch { }
                    }

                    return RedirectToAction(nameof(Index));
                }

                // Send emails asynchronously
                _ = SendBroadcastEmailsAsync(broadcast, recipients, attachmentPath, body);

                TempData["success"] = $"Broadcast queued for sending to {recipients.Count} recipient(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast message");
                TempData["error"] = "An error occurred while sending the broadcast message. See logs for details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Send emails in background (fire and forget with error logging)
        /// </summary>
        private async Task SendBroadcastEmailsAsync(BroadcastMessage broadcastMessage, List<IdentityUser> recipients, string attachmentPath, string htmlBody)
        {
            var successCount = 0;
            var failureCount = 0;

            foreach (var recipient in recipients)
            {
                try
                {
                    if (!string.IsNullOrEmpty(attachmentPath))
                    {
                        await _emailService.SendEmailWithAttachmentAsync(recipient.Email, broadcastMessage.Subject, htmlBody, attachmentPath);
                    }
                    else
                    {
                        await _emailService.SendEmailAsync(recipient.Email, broadcastMessage.Subject, htmlBody);
                    }
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(ex, "Failed to send broadcast message {MessageId} to {Email}", broadcastMessage.Id, recipient.Email);
                }
            }

            // Update broadcast message status
            broadcastMessage.Status = failureCount == 0 ? "Sent" : (failureCount == recipients.Count ? "Failed" : "PartialSent");
            _context.BroadcastMessages.Update(broadcastMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Broadcast {MessageId} completed: {SuccessCount} sent, {FailureCount} failed", 
                broadcastMessage.Id, successCount, failureCount);
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
                .FirstOrDefaultAsync(b => b.Id == id);

            if (broadcast == null)
            {
                return NotFound();
            }

            return View(broadcast);
        }
    }
}
