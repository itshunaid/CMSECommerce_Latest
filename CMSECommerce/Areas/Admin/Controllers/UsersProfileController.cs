using CMSECommerce.Infrastructure;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // 1. ADDED
using Microsoft.Extensions.Logging;
using System.IO; // 1. ADDED

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]    
    public class UsersProfileController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        ILogger<UsersProfileController> logger) : Controller
    {
        // Fields are automatically initialized by the primary constructor in modern C# (>= 12).
        // While defining the fields explicitly below is redundant, it is kept here for clarity
        // since the original code defined them.
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly ILogger<UsersProfileController> _logger = logger;

        // GET: /admin/user/pendingimageapprovals
        [HttpGet("pendingimageapprovals")]
        public async Task<IActionResult> PendingImageApprovals()
        {
            try
            {
                // Fetch all UserProfiles where ProfileImagePath is set but IsImageApproved is false
                var pendingProfiles = await _context.UserProfiles
                    .AsNoTracking()
                    // Checking for IsImageApproved == false and the existence of an image path
                    .Where(p => !p.IsImageApproved && p.ProfileImagePath != null && p.ProfileImagePath != "")
                    .ToListAsync();

                return View(pendingProfiles); // Pass the list to a view for the Admin to review
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving pending image approvals list.");
                TempData["ErrorMessage"] = "A database error occurred while loading the profiles list.";
                return View(new List<UserProfile>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving pending image approvals list.");
                TempData["ErrorMessage"] = "An unexpected error occurred while loading the approvals.";
                return View(new List<UserProfile>());
            }
        }

        // POST: /admin/user/ApproveProfileImage
        [HttpPost("ApproveProfileImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProfileImage(int id)
        {
            var userProfile = await _context.UserProfiles.FindAsync(id);
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = $"User profile with ID {id} not found.";
                return RedirectToAction(nameof(PendingImageApprovals));
            }

            // Simple checks
            if (string.IsNullOrEmpty(userProfile.ProfileImagePath))
            {
                TempData["WarningMessage"] = "No profile image path found for this user.";
                return RedirectToAction(nameof(PendingImageApprovals));
            }

            if (userProfile.IsImageApproved)
            {
                TempData["WarningMessage"] = "Profile image is already approved.";
                return RedirectToAction(nameof(PendingImageApprovals));
            }

            // Set the approval status to true and save
            userProfile.IsImageApproved = true;
            userProfile.IsImagePending = false;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Profile image for user ID {userProfile.UserId} has been **approved** and is now live.";
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Concurrency error approving image for profile ID: {ProfileId}", id);
                TempData["ErrorMessage"] = "A database concurrency error occurred during approval. Please retry.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error approving image for profile ID: {ProfileId}", id);
                TempData["ErrorMessage"] = "A database error occurred during approval.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error approving image for profile ID: {ProfileId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred during approval.";
            }

            // Redirect back to the list of pending approvals
            return RedirectToAction(nameof(PendingImageApprovals));
        }

        // POST: /admin/user/DisapproveProfileImage
        [HttpPost("DisapproveProfileImage")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisapproveProfileImage(int id)
        {
            var userProfile = await _context.UserProfiles.FindAsync(id);
            if (userProfile == null)
            {
                TempData["ErrorMessage"] = $"User profile with ID {id} not found.";
                return RedirectToAction(nameof(PendingImageApprovals));
            }

            string oldPath = userProfile.ProfileImagePath;
            string userId = userProfile.UserId;

            // Clear the image path and explicitly set approval to false
            userProfile.ProfileImagePath = null;
            userProfile.IsImageApproved = false;

            try
            {
                // Database save
                await _context.SaveChangesAsync();

                // Delete the physical file (RECOMMENDED)
                if (!string.IsNullOrEmpty(oldPath))
                {
                    // Trimming both '~' and '/' ensures the path is correctly combined with WebRootPath
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, oldPath.TrimStart('~', '/'));

                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                        _logger.LogInformation("Deleted rejected profile image at path: {Path}", fullPath);
                    }
                    else
                    {
                        _logger.LogWarning("Attempted to delete rejected profile image but file not found at path: {Path}", fullPath);
                    }
                }

                TempData["SuccessMessage"] = $"Profile image for user ID {userId} has been **rejected** and removed. The user must upload a new image.";
            }
            catch (DbUpdateConcurrencyException dbEx)
            {
                _logger.LogError(dbEx, "Concurrency error rejecting image for profile ID: {ProfileId}", id);
                TempData["ErrorMessage"] = "A database concurrency error occurred during rejection. Please retry.";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error rejecting image for profile ID: {ProfileId}", id);
                TempData["ErrorMessage"] = "A database error occurred while saving the rejection status.";
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "File system error deleting rejected image for profile ID: {ProfileId}", id);
                // The database change was already saved, so the file system error is logged as a warning, but the user is still informed.
                TempData["WarningMessage"] = $"Profile image rejected, but failed to delete the physical file. The database record was updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error rejecting image for profile ID: {ProfileId}", id);
                TempData["ErrorMessage"] = "An unexpected error occurred during rejection.";
            }

            // Redirect back to the list of pending approvals
            return RedirectToAction(nameof(PendingImageApprovals));
        }
    }
}