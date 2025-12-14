using CMSECommerce.Areas.Admin.Models; // Assumed namespace for ViewModels
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController(
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        DataContext context,
        IWebHostEnvironment webHostEnvironment,
        ILogger<UsersController> logger) : Controller
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly ILogger<UsersController> _logger = logger;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var users = await _userManager.Users.AsNoTracking().ToListAsync();
                var model = new List<UserViewModel>();

                foreach (var u in users)
                {
                    // Identity operation
                    var roles = await _userManager.GetRolesAsync(u);
                    var isLocked = await _userManager.IsLockedOutAsync(u);

                    model.Add(new UserViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Roles = roles,
                        IsLockedOut = isLocked
                    });
                }
                return View(model);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error retrieving user list in Index.");
                TempData["error"] = "A database error occurred while loading the user list.";
                return View(new List<UserViewModel>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving user list in Index.");
                TempData["error"] = "An unexpected error occurred while loading users.";
                return View(new List<UserViewModel>());
            }
        }

        public async Task<IActionResult> EnableDisable(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = $"User with ID {id} not found.";
                return NotFound();
            }

            try
            {
                var isLocked = await _userManager.IsLockedOutAsync(user);

                if (isLocked)
                {
                    // Enable user
                    await _userManager.SetLockoutEndDateAsync(user, null);
                    await _userManager.ResetAccessFailedCountAsync(user);
                    TempData["success"] = $"User '{user.UserName}' has been enabled.";
                }
                else
                {
                    // Disable (Lock out) user permanently
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                    TempData["warning"] = $"User '{user.UserName}' has been permanently locked out.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling/disabling user ID: {UserId}", id);
                TempData["error"] = "Error updating user's lock status. Please check logs.";
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Create()
        {
            try
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving roles for Create view.");
                TempData["error"] = "Error loading roles for the creation form.";
                ViewBag.Roles = new List<string>(); // Provide empty list to prevent crash
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var user = new IdentityUser { UserName = model.UserName, Email = model.Email, EmailConfirmed = true, PhoneNumber = model.PhoneNumber };
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        if (!string.IsNullOrWhiteSpace(model.Role))
                        {
                            if (await _roleManager.RoleExistsAsync(model.Role))
                            {
                                var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                                if (!roleResult.Succeeded)
                                {
                                    _logger.LogError("Identity error adding role {Role} to user {UserName}. Errors: {Errors}",
                                        model.Role, model.UserName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                                    TempData["warning"] = $"User created, but failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                                    return RedirectToAction(nameof(Index)); // Continue to Index but with warning
                                }
                            }
                        }
                        TempData["success"] = $"User '{model.UserName}' created successfully.";
                        return RedirectToAction(nameof(Index));
                    }

                    // Handle Identity creation errors
                    foreach (var e in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, e.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user creation for user: {UserName}", model.UserName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during user creation.");
            }

            // Reload roles if the view is returned due to validation/error
            try
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading roles after failed user creation.");
                ViewBag.Roles = new List<string>();
            }

            return View(model);
        }

        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return NotFound();
                }

                var roles = await _userManager.GetRolesAsync(user);

                var model = new EditUserModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault()
                };

                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user ID: {UserId} for editing.", id);
                TempData["error"] = "Failed to load user details for editing.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserModel model)
        {
            // Reload roles immediately for failed path
            try
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reloading roles during user edit postback.");
                ViewBag.Roles = new List<string>();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            try
            {
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
                    return View(model);
                }

                // --- Role Update Logic ---
                if (!string.IsNullOrWhiteSpace(model.Role) && !currentRoles.Contains(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (await _roleManager.RoleExistsAsync(model.Role))
                        await _userManager.AddToRoleAsync(user, model.Role);
                }
                else if (string.IsNullOrWhiteSpace(model.Role) && currentRoles.Count > 0)
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                // --- End Role Update Logic ---

                // ⭐ NEW LOGIC: Check for Subscriber Role Revocation 
                bool wasSubscriber = currentRoles.Contains("Subscriber");
                bool isSubscriberNow = await _userManager.IsInRoleAsync(user, "Subscriber");

                if (wasSubscriber && !isSubscriberNow)
                {
                    var revokedRequest = await _context.SubscriberRequests
                        .Where(r => r.UserId == user.Id && r.Approved == true)
                        .OrderByDescending(r => r.RequestDate)
                        .FirstOrDefaultAsync();

                    if (revokedRequest != null)
                    {
                        revokedRequest.Approved = false;
                        revokedRequest.ApprovalDate = DateTime.Now;
                        revokedRequest.AdminNotes = $"Subscription revoked by Admin on {DateTime.Now.ToShortDateString()}. Role changed to {model.Role ?? "None"}.";

                        _context.Update(revokedRequest);
                        await _context.SaveChangesAsync(); // Database operation
                        TempData["warning"] = $"Subscription status for {user.UserName} has been revoked and the request updated.";
                    }
                }
                // --- End Subscriber Role Revocation ---

                // Change password if provided
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                    if (!passResult.Succeeded)
                    {
                        foreach (var e in passResult.Errors)
                            ModelState.AddModelError(string.Empty, e.Description);
                        return View(model);
                    }
                }

                TempData["success"] = $"User {user.UserName} updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error updating user ID: {UserId}", model.Id);
                ModelState.AddModelError(string.Empty, "A database error occurred while updating the user record.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error updating user ID: {UserId}", model.Id);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during user update.");
            }

            return View(model);
        }

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

            string userName = user.UserName;

            try
            {
                // --- 1. Find and Delete Dependent Records (CRITICAL STEP) ---

                // a. Delete UserProfile
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);

                if (userProfile != null)
                {
                    // Delete associated physical files (Image/QRCode)
                    if (!string.IsNullOrEmpty(userProfile.ProfileImagePath))
                    {
                        string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfileImagePath.TrimStart('/'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath); // File System Operation
                        }
                    }

                    _context.UserProfiles.Remove(userProfile);
                }

                // b. Delete Address records (Assuming they link directly to UserId)
                var userAddresses = await _context.Addresses.Where(a => a.UserId == id).ToListAsync();
                _context.Addresses.RemoveRange(userAddresses);

                // c. Delete SubscriberRequests
                var subscriberRequests = await _context.SubscriberRequests.Where(r => r.UserId == id).ToListAsync();
                _context.SubscriberRequests.RemoveRange(subscriberRequests);

                // d. Delete Product records (Assuming products have an OwnerId/UserId field)
                // Depending on your model, you might need to handle product files/gallery too.
                // Assuming Product.OwnerId is the UserId (string)
                var userProducts = await _context.Products.Where(p => p.OwnerId == user.UserName).ToListAsync();
                // **WARNING:** Deleting products may require file system cleanup of images/gallery. 
                // This is a complex dependency that should be handled explicitly.
                _context.Products.RemoveRange(userProducts);

                // Save changes to delete custom records from your DataContext
                await _context.SaveChangesAsync(); // Database operation

                // --- 2. Delete the IdentityUser ---
                IdentityResult result = await _userManager.DeleteAsync(user); // Identity operation

                if (result.Succeeded)
                {
                    TempData["success"] = $"User '{userName}' and all associated data deleted successfully.";
                    return RedirectToAction(nameof(Index));
                }

                // Handle Identity Manager errors (e.g., if roles/claims removal failed)
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Identity error deleting user {UserName}. Errors: {Errors}", userName, errors);
                TempData["error"] = $"Error deleting core user record: {errors}";
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error deleting user ID: {UserId} and associated records.", id);
                TempData["error"] = "A database error occurred while deleting the user. Ensure all foreign key constraints are handled.";
            }
            catch (IOException ioEx)
            {
                _logger.LogError(ioEx, "File system error during user deletion for ID: {UserId}", id);
                TempData["error"] = "File error occurred while deleting user profile data. The user was not deleted from the database.";
                // Re-throw or return if you want to stop the Identity deletion
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error deleting user ID: {UserId}", id);
                TempData["error"] = "An unexpected error occurred during user deletion.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}