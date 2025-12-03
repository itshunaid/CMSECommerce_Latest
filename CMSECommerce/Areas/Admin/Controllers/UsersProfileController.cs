using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Areas.Admin.Models; // Assumed namespace for ViewModels
using System.Linq; // Required for Select and FirstOrDefault
using System.Collections.Generic; // Required for List

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    // 🔴 RECTIFIED: Changed [Authorize("Admin")] to [Authorize(Roles = "Admin")]
    // assuming 'Admin' is a role name, which is the standard Identity authorization pattern.
    [Authorize(Roles = "Admin")]    
    public class UsersProfileController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _context;
        // private readonly DataContext _context; // Inject DataContext if needed here

        // 🔴 RECTIFIED: Optional - Use primary constructor syntax (C# 12) for cleaner injection, 
        // though the current constructor is perfectly valid.
        public UsersProfileController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, DataContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        //[HttpGet]
        //public async Task<IActionResult> Index()
        //{
        //    var users = await _userManager.Users.ToListAsync();
        //    // 🔴 RECTIFIED: Ensure UserViewModel is referenced by 'using CMSECommerce.Areas.Admin.Models;'
        //    var model = new List<UserViewModel>();

        //    foreach (var u in users)
        //    {
        //        var roles = await _userManager.GetRolesAsync(u);
        //        var isLocked = await _userManager.IsLockedOutAsync(u);
        //        model.Add(new UserViewModel { Id = u.Id, UserName = u.UserName, Email = u.Email,PhoneNumber=u.PhoneNumber, Roles = roles, IsLockedOut = isLocked });
        //    }

        //    return View(model);
        //}

        // GET: /admin/user/pendingimageapprovals
        [HttpGet("pendingimageapprovals")]
        public async Task<IActionResult> PendingImageApprovals()
        {
            // Fetch all UserProfiles where ProfileImagePath is set but IsImageApproved is false
            var pendingProfiles = await _context.UserProfiles
                .Where(p => !p.IsImageApproved && p.ProfileImagePath != null)
                .ToListAsync();

            return View(pendingProfiles); // Pass the list to a view for the Admin to review
        }

        // POST: /admin/user/ApproveProfileImage
        [HttpPost("ApproveProfileImage")] // 🟢 ADDED: Route to match action name, using base route
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveProfileImage(int id)
        {
            // 1. Find the UserProfile by its ID
            var userProfile = await _context.UserProfiles.FindAsync(id);

            if (userProfile == null)
            {
                TempData["ErrorMessage"] = $"User profile with ID {id} not found.";
                return RedirectToAction(nameof(PendingImageApprovals));
            }

            // 2. Check if it already has a path and is not already approved
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

            // 3. Set the approval status to true and save
            userProfile.IsImageApproved = true;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Profile image for user ID {userProfile.UserId} has been **approved** and is now live.";
            }
            catch (DbUpdateConcurrencyException)
            {
                // Handle concurrency issues if needed
                TempData["ErrorMessage"] = "A database error occurred during approval.";
            }

            // 4. Redirect back to the list of pending approvals
            return RedirectToAction(nameof(PendingImageApprovals));
        }

        // POST: /admin/user/DisapproveProfileImage
        [HttpPost("DisapproveProfileImage")] // 🟢 ADDED: Route to match action name, using base route
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisapproveProfileImage(int id)
        {
            // 1. Find the UserProfile by its ID
            var userProfile = await _context.UserProfiles.FindAsync(id);

            if (userProfile == null)
            {
                TempData["ErrorMessage"] = $"User profile with ID {id} not found.";
                return RedirectToAction(nameof(PendingImageApprovals));
            }

            // 2. Clear the image path and explicitly set approval to false

            // **Crucial Step:** Clear the path so the image no longer resolves.
            // You might also want to delete the file from your storage (disk/cloud) here.
            string oldPath = userProfile.ProfileImagePath;
            userProfile.ProfileImagePath = null;

            // Ensure approval status is false
            userProfile.IsImageApproved = false;

            try
            {
                await _context.SaveChangesAsync();

                // 3. Optional: Delete the physical file (recommended for cleaning up storage)
                // You would need to inject or access your file management service here.
                /* if (!string.IsNullOrEmpty(oldPath))
                {
                    // File.Delete(Path.Combine(_webHostEnvironment.WebRootPath, oldPath));
                } 
                */

                TempData["SuccessMessage"] = $"Profile image for user ID {userProfile.UserId} has been **rejected**. The user must upload a new image.";
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "A database error occurred during rejection.";
            }

            // 4. Redirect back to the list of pending approvals
            return RedirectToAction(nameof(PendingImageApprovals));
        }

        
    }
}