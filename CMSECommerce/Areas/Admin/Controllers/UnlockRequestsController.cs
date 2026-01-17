using CMSECommerce.Areas.Admin.Models;
using CMSECommerce.Controllers;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UnlockRequestsController : BaseController
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<UnlockRequestsController> _logger;
        private readonly IEmailService _emailService;

        public UnlockRequestsController(
            DataContext context,
            UserManager<IdentityUser> userManager,
            ILogger<UnlockRequestsController> logger,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
            _emailService = emailService;
        }

        // GET: Admin/UnlockRequests
        public async Task<IActionResult> Index()
        {
            var requests = await _context.UnlockRequests
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        // GET: Admin/UnlockRequests/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var unlockRequest = await _context.UnlockRequests
                .FirstOrDefaultAsync(m => m.Id == id);

            if (unlockRequest == null)
            {
                return NotFound();
            }

            return View(unlockRequest);
        }

        // POST: Admin/UnlockRequests/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var unlockRequest = await _context.UnlockRequests.FindAsync(id);
            if (unlockRequest == null)
            {
                TempData["error"] = "Unlock request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (unlockRequest.Status != "Pending")
            {
                TempData["error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Find the user
                var user = await _userManager.FindByIdAsync(unlockRequest.UserId);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Reset lockout
                var resetLockoutResult = await _userManager.ResetAccessFailedCountAsync(user);
                var setLockoutEndDateResult = await _userManager.SetLockoutEndDateAsync(user, null);

                if (resetLockoutResult.Succeeded && setLockoutEndDateResult.Succeeded)
                {
                    // Update request status
                    unlockRequest.Status = "Approved";
                    unlockRequest.AdminNotes = $"Approved by {User.Identity.Name} on {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}";
                    await _context.SaveChangesAsync();

                    // Send notification email
                    try
                    {
                        var subject = "Account Unlock Approved";
                        var body = $@"
                        <h2>Account Unlock Approved</h2>
                        <p>Dear User,</p>
                        <p>Your account unlock request has been approved. You can now sign in to your account.</p>
                        <p>If you continue to experience issues, please contact support.</p>
                        <br>
                        <p>Best regards,<br>Weypaari Team</p>";

                        await _emailService.SendEmailAsync(unlockRequest.Email, subject, body);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send unlock approval email to {Email}", unlockRequest.Email);
                    }

                    TempData["success"] = $"Unlock request for {unlockRequest.UserName} has been approved.";
                }
                else
                {
                    var errors = string.Join(", ", resetLockoutResult.Errors.Concat(setLockoutEndDateResult.Errors).Select(e => e.Description));
                    TempData["error"] = $"Failed to unlock account: {errors}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving unlock request {RequestId}", id);
                TempData["error"] = "An error occurred while processing the request.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/UnlockRequests/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string adminNotes)
        {
            var unlockRequest = await _context.UnlockRequests.FindAsync(id);
            if (unlockRequest == null)
            {
                TempData["error"] = "Unlock request not found.";
                return RedirectToAction(nameof(Index));
            }

            if (unlockRequest.Status != "Pending")
            {
                TempData["error"] = "This request has already been processed.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                // Update request status
                unlockRequest.Status = "Rejected";
                unlockRequest.AdminNotes = $"Rejected by {User.Identity.Name} on {DateTime.UtcNow:yyyy-MM-dd HH:mm UTC}. Notes: {adminNotes}";
                await _context.SaveChangesAsync();

                // Send notification email
                try
                {
                    var subject = "Account Unlock Request Rejected";
                    var body = $@"
                    <h2>Account Unlock Request Rejected</h2>
                    <p>Dear User,</p>
                    <p>Your account unlock request has been rejected.</p>
                    <p>Reason: {adminNotes}</p>
                    <p>If you believe this is an error, please contact support.</p>
                    <br>
                    <p>Best regards,<br>Weypaari Team</p>";

                    await _emailService.SendEmailAsync(unlockRequest.Email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send unlock rejection email to {Email}", unlockRequest.Email);
                }

                TempData["success"] = $"Unlock request for {unlockRequest.UserName} has been rejected.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting unlock request {RequestId}", id);
                TempData["error"] = "An error occurred while processing the request.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
