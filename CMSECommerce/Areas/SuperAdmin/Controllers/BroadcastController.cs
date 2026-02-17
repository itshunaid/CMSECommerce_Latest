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

        /// <summary>
        /// Display broadcast message form
        /// </summary>
        public async Task<IActionResult> Index()
        {
            // Get all sellers (users with active subscription)
            // Users with active subscription are those who have:
            // 1. CurrentTierId set AND
            // 2. SubscriptionEndDate is null OR SubscriptionEndDate > DateTime.Now
            var sellerProfiles = await _context.UserProfiles
                .Where(p => p.CurrentTierId.HasValue && 
                           (p.SubscriptionEndDate == null || p.SubscriptionEndDate > DateTime.Now))
                .Include(p => p.User)
                .ToListAsync();

            var sellerList = sellerProfiles
                .Where(p => p.User != null)
                .Select(p => new { id = p.User.Id, email = p.User.Email, name = p.User.UserName })
                .ToList();

            ViewBag.Sellers = sellerList;
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
            [FromForm] bool sendToAllSellers,
            [FromForm] string selectedSellerIds,
            [FromForm] IFormFile attachmentFile)
        {
            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(body))
            {
                TempData["error"] = "Subject and message body are required.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Get list of sellers to send to
                List<IdentityUser> recipientSellers;
                if (sendToAllSellers)
                {
                    // Get all users with active subscriptions
                    var sellerProfiles = await _context.UserProfiles
                        .Where(p => p.CurrentTierId.HasValue && 
                                   (p.SubscriptionEndDate == null || p.SubscriptionEndDate > DateTime.Now))
                        .Include(p => p.User)
                        .ToListAsync();

                    recipientSellers = sellerProfiles
                        .Where(p => p.User != null)
                        .Select(p => p.User)
                        .ToList();
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(selectedSellerIds))
                    {
                        TempData["error"] = "Please select at least one seller.";
                        return RedirectToAction(nameof(Index));
                    }

                    var sellerIds = selectedSellerIds.Split(',').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    recipientSellers = new List<IdentityUser>();
                    foreach (var id in sellerIds)
                    {
                        var seller = await _userManager.FindByIdAsync(id);
                        if (seller != null)
                        {
                            recipientSellers.Add(seller);
                        }
                    }
                }

                if (!recipientSellers.Any())
                {
                    TempData["error"] = "No sellers found to send message to.";
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

                        // Generate unique filename to avoid conflicts
                        var uniqueFileName = $"{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid()}_{attachmentFileName}";
                        attachmentPath = Path.Combine(uploadsFolder, uniqueFileName);

                        using (var fileStream = new FileStream(attachmentPath, FileMode.Create))
                        {
                            await attachmentFile.CopyToAsync(fileStream);
                        }

                        _logger.LogInformation("Attachment uploaded: {AttachmentPath}", attachmentPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upload attachment");
                        TempData["warning"] = "Attachment upload failed, sending without attachment.";
                        attachmentPath = null;
                    }
                }

                // Create broadcast message record
                var broadcastMessage = new BroadcastMessage
                {
                    Subject = subject,
                    Body = body,
                    AttachmentFileName = attachmentFileName,
                    AttachmentPath = attachmentPath,
                    SendToAllSellers = sendToAllSellers,
                    SelectedSellerIds = sendToAllSellers ? null : selectedSellerIds,
                    SentByUserId = currentUser.Id,
                    DateSent = DateTime.UtcNow,
                    RecipientCount = recipientSellers.Count,
                    Status = "Pending"
                };

                _context.BroadcastMessages.Add(broadcastMessage);
                await _context.SaveChangesAsync();

                // Send emails asynchronously
                _ = SendBroadcastEmailsAsync(broadcastMessage, recipientSellers, attachmentPath, body);

                TempData["success"] = $"Broadcast message queued for sending to {recipientSellers.Count} seller(s).";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending broadcast message");
                TempData["error"] = "An error occurred while sending the broadcast message.";
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
            // Get all sellers (users with active subscription)
            var sellerProfiles = await _context.UserProfiles
                .Where(p => p.CurrentTierId.HasValue && 
                           (p.SubscriptionEndDate == null || p.SubscriptionEndDate > DateTime.Now))
                .Include(p => p.User)
                .ToListAsync();

            var sellerList = sellerProfiles
                .Where(p => p.User != null)
                .Select(p => new { id = p.User.Id, email = p.User.Email, name = p.User.UserName })
                .ToList();

            return Json(sellerList);
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
