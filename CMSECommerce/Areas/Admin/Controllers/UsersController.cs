using CMSECommerce.Areas.Admin.Models; // Assumed namespace for ViewModels
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic; // Required for List
using System.Linq; // Required for Select and FirstOrDefault

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    // 🔴 RECTIFIED: Changed [Authorize("Admin")] to [Authorize(Roles = "Admin")]
    // assuming 'Admin' is a role name, which is the standard Identity authorization pattern.
    [Authorize(Roles = "Admin")]    
    public class UsersController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _context;
        private IWebHostEnvironment _webHostEnvironment;
        // private readonly DataContext _context; // Inject DataContext if needed here

        // 🔴 RECTIFIED: Optional - Use primary constructor syntax (C# 12) for cleaner injection, 
        // though the current constructor is perfectly valid.
        public UsersController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, DataContext context, IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            // 🔴 RECTIFIED: Ensure UserViewModel is referenced by 'using CMSECommerce.Areas.Admin.Models;'
            var model = new List<UserViewModel>();

            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var isLocked = await _userManager.IsLockedOutAsync(u);
                model.Add(new UserViewModel { Id = u.Id, UserName = u.UserName, Email = u.Email,PhoneNumber=u.PhoneNumber, Roles = roles, IsLockedOut = isLocked });
            }

            return View(model);
        }

        
        public async Task<IActionResult> EnableDisable(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var isLocked = await _userManager.IsLockedOutAsync(user);

            // 🔴 RECTIFIED: Check if the user is already locked out before trying to reset the lockout end date. 
            // Also, consider calling ResetAccessFailedCountAsync if the lockout was due to failed attempts.
            if (isLocked)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
                // Optionally: await _userManager.ResetAccessFailedCountAsync(user);
            }
            else
            {
                // Note: DateTimeOffset.MaxValue is correct for permanent lock out.
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Create()
        {
            // 🔴 RECTIFIED: Ensure the RoleManager has access to roles via 'using System.Linq;'
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserModel model) // Ensure CreateUserModel is referenced
        {
            if (ModelState.IsValid)
            {
                var user = new IdentityUser { UserName = model.UserName, Email = model.Email, EmailConfirmed = true };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrWhiteSpace(model.Role))
                    {
                        // 🔴 Logic for adding role is correct here
                        if (await _roleManager.RoleExistsAsync(model.Role))
                            await _userManager.AddToRoleAsync(user, model.Role);
                    }

                    return RedirectToAction(nameof(Index));
                }
                foreach (var e in result.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
            }
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            // Ensure EditUserModel is referenced
            var model = new EditUserModel { Id = user.Id, UserName = user.UserName, Email = user.Email, PhoneNumber=user.PhoneNumber, Role = roles.FirstOrDefault() };
            ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            return View(model);
        }

        // Assuming your controller has the DataContext injected:
        // private readonly DataContext _context; 

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // Store the current roles BEFORE the user object is updated
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Update user details
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            var updateResult = await _userManager.UpdateAsync(user);

            if (!updateResult.Succeeded)
            {
                foreach (var e in updateResult.Errors)
                    ModelState.AddModelError(string.Empty, e.Description);
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }

            // --- Role Update Logic ---

            // 1. Check if the user should have a new role (model.Role) 
            if (!string.IsNullOrWhiteSpace(model.Role) && !currentRoles.Contains(model.Role))
            {
                // Remove all current roles and add the new one.
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (await _roleManager.RoleExistsAsync(model.Role))
                    await _userManager.AddToRoleAsync(user, model.Role);
            }
            // 2. Check if a role was removed (model.Role is null/empty but currentRoles exist)
            else if (string.IsNullOrWhiteSpace(model.Role) && currentRoles.Count > 0)
            {
                // remove all roles if none selected
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            // 3. If model.Role is already currentRoles.FirstOrDefault(), do nothing.

            // -------------------------------------------------------------------------
            // ⭐ NEW LOGIC: Check for Subscriber Role Revocation 
            // -------------------------------------------------------------------------
            bool wasSubscriber = currentRoles.Contains("Subscriber");
            bool isSubscriberNow = await _userManager.IsInRoleAsync(user, "Subscriber");

            if (wasSubscriber && !isSubscriberNow)
            {
                // The user was a Subscriber but is no longer one (e.g., changed to Customer or Admin)
                var revokedRequest = await _context.SubscriberRequests
                    .Where(r => r.UserId == user.Id && r.Approved == true) // Find the previously approved request
                    .OrderByDescending(r => r.RequestDate)
                    .FirstOrDefaultAsync();

                if (revokedRequest != null)
                {
                    revokedRequest.Approved = false; // Set to rejected/revoked status
                    revokedRequest.ApprovalDate = DateTime.Now; // Update process date
                    revokedRequest.AdminNotes = $"Subscription revoked by Admin on {DateTime.Now.ToShortDateString()}. Role changed to {model.Role ?? "None"}.";

                    _context.Update(revokedRequest);
                    await _context.SaveChangesAsync();
                }
                TempData["warning"] = $"Subscription status for {user.UserName} has been revoked and the request updated.";
            }
            // -------------------------------------------------------------------------


            // change password if provided
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                if (!passResult.Succeeded)
                {
                    foreach (var e in passResult.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);
                    ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                    return View(model);
                }
            }

            TempData["success"] = $"User {user.UserName} updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        //[HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeleteConfirmed(string id)
        //{
        //    var user = await _userManager.FindByIdAsync(id);
        //    if (user == null) return NotFound();
        //    await _userManager.DeleteAsync(user);
        //    return RedirectToAction(nameof(Index));
        //}

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // --- 1. Find and Delete Dependent Records (CRITICAL STEP) ---

            // a. Delete UserProfile (Requires UserId for lookup)
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == id);

            if (userProfile != null)
            {
                // 1a. Delete associated physical files (images/QRCodes)
                // You should re-use your file deletion helper logic here, as defined in your previous DeleteProfileData action.

                // Example: Delete Profile Image file
                if (!string.IsNullOrEmpty(userProfile.ProfileImagePath))
                {
                    // Assuming you have access to _webHostEnvironment
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfileImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
                }

                _context.UserProfiles.Remove(userProfile);
            }

            // b. Delete Address records (Requires UserId for lookup)
            var userAddresses = await _context.Addresses
                .Where(a => a.UserId == id)
                .ToListAsync();

            _context.Addresses.RemoveRange(userAddresses);

            // c. Save changes to delete custom records from your DataContext
            // This removes the foreign key constraint preventing the IdentityUser deletion.
            await _context.SaveChangesAsync();

            // --- 2. Delete the IdentityUser ---
            IdentityResult result = await _userManager.DeleteAsync(user);

            if (result.Succeeded)
            {
                TempData["success"] = $"User '{user.UserName}' and all associated data deleted successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Handle Identity Manager errors (e.g., if roles/claims removal failed)
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            TempData["error"] = $"Error deleting core user record: {errors}";
            return RedirectToAction(nameof(Index));
        }
    }
}