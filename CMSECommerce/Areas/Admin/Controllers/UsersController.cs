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
                // 1. Efficient Data Retrieval (Single Query)
                // Join IdentityUsers with their UserProfiles and project into an anonymous object.
                // This avoids the N+1 problem for fetching profiles.
                var usersAndProfiles = await _userManager.Users.AsNoTracking()
                    .Join(_context.UserProfiles.AsNoTracking(), // Join with UserProfiles table
                        user => user.Id,                        // Key for IdentityUser
                        profile => profile.UserId,              // Key for UserProfile
                        (user, profile) => new { User = user, Profile = profile }) // Projection
                    .ToListAsync();
                var model = new List<UserViewModel>();

                foreach (var item in usersAndProfiles)
                {
                    var u = item.User;
                    var p = item.Profile;

                    // 2. Identity Operations (These still require a separate trip per user)
                    var roles = await _userManager.GetRolesAsync(u);
                    var isLocked = await _userManager.IsLockedOutAsync(u);

                    // 3. Mapping to UserViewModel
                    model.Add(new UserViewModel
                    {
                        // IdentityUser Fields
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Roles = roles,
                        IsLockedOut = isLocked,

                        // UserProfile Fields
                        FirstName = p.FirstName,
                        LastName = p.LastName,
                        ITSNumber = p.ITSNumber,
                        ProfileImagePath = p.ProfileImagePath,
                        IsImageApproved = p.IsImageApproved,
                        IsProfileVisible = p.IsProfileVisible,
                        About = p.About,
                        Profession = p.Profession,
                        ServicesProvided = p.ServicesProvided,
                        LinkedInUrl = p.LinkedInUrl,
                        FacebookUrl = p.FacebookUrl,
                        InstagramUrl = p.InstagramUrl,
                        WhatsappNumber = p.WhatsappNumber,
                        HomeAddress = p.HomeAddress,
                        HomePhoneNumber = p.HomePhoneNumber,
                        BusinessAddress = p.BusinessAddress,
                        BusinessPhoneNumber = p.BusinessPhoneNumber,
                        GpayQRCodePath = p.GpayQRCodePath,
                        PhonePeQRCodePath = p.PhonePeQRCodePath
                    });
                }

                // Handle users without a UserProfile (Optional, but recommended for robustness)
                // Get users that are in IdentityUser but NOT in UserProfiles
                var identityUserIds = usersAndProfiles.Select(i => i.User.Id).ToList();
                var usersWithoutProfiles = await _userManager.Users
                    .Where(u => !identityUserIds.Contains(u.Id))
                    .ToListAsync();

                foreach (var u in usersWithoutProfiles)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    var isLocked = await _userManager.IsLockedOutAsync(u);

                    model.Add(new UserViewModel
                    {
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Roles = roles,
                        IsLockedOut = isLocked,
                        // Profile fields will be null/default (e.g., empty string)
                    });
                }
                var userRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                ViewBag.AllRoles = userRoles;
                return View(model.OrderBy(u => u.UserName).ToList());
            }
            catch (Exception ex) when (ex is DbUpdateException || ex is InvalidOperationException)
            {
                _logger.LogError(ex, "Database error retrieving user list in Index.");
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

        // Inside your UsersController.cs

        // Inside your UsersController.cs

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserField(string userId, string fieldName, string value)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(fieldName))
            {
                return BadRequest("Missing user ID or field name.");
            }

            try
            {
                // --- 1. Handle Role Update (IdentityUser) ---
                if (fieldName == "Role")
                {
                    // ... (Role logic remains the same as before) ...
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null) return NotFound("User not found.");

                    var currentRoles = await _userManager.GetRolesAsync(user);

                    // Remove all current roles
                    if (currentRoles.Any())
                    {
                        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                        if (!removeResult.Succeeded) return StatusCode(500, $"Failed to remove old roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");
                    }

                    // Add new role if one is provided (value is the new role name)
                    if (!string.IsNullOrWhiteSpace(value) && await _roleManager.RoleExistsAsync(value))
                    {
                        var addResult = await _userManager.AddToRoleAsync(user, value);
                        if (!addResult.Succeeded) return StatusCode(500, $"Failed to add new role: {string.Join(", ", addResult.Errors.Select(e => e.Description))}");
                    }

                    return Ok($"Role updated to {value ?? "None"}");
                }

                // --- 2. Handle Status Update (IdentityUser - IsLockedOut) ---
                else if (fieldName == "IsLockedOut")
                {
                    if (!bool.TryParse(value, out bool isLockedOut))
                    {
                        return BadRequest("Invalid value for IsLockedOut.");
                    }

                    var user = await _userManager.FindByIdAsync(userId);
                    if (user == null) return NotFound("User not found.");

                    // Set the lock status in the Identity system
                    // LockoutEnd = null means they are NOT locked out.
                    user.LockoutEnd = isLockedOut ? (DateTimeOffset?)DateTimeOffset.MaxValue : null;

                    var updateResult = await _userManager.UpdateAsync(user);

                    if (!updateResult.Succeeded)
                    {
                        return StatusCode(500, $"Failed to update user status: {string.Join(", ", updateResult.Errors.Select(e => e.Description))}");
                    }
                    return Ok($"IsLockedOut status updated to {isLockedOut}");
                }

                // --- 3. Handle Profile Field Updates (UserProfile) ---
                else if (fieldName == "Profession" || fieldName == "IsProfileVisible")
                {
                    var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
                    if (profile == null) return NotFound("User Profile not found.");

                    switch (fieldName)
                    {
                        case "Profession":
                            profile.Profession = value;
                            break;
                        case "IsProfileVisible":
                            if (!bool.TryParse(value, out bool isVisible))
                            {
                                return BadRequest("Invalid value for IsProfileVisible.");
                            }
                            profile.IsProfileVisible = isVisible;
                            break;
                        default:
                            return BadRequest("Invalid field name for profile update.");
                    }

                    await _context.SaveChangesAsync();
                    return Ok($"{fieldName} updated successfully.");
                }

                else
                {
                    return BadRequest($"Field '{fieldName}' is not supported for inline update.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AJAX update failed for user {UserId} field {FieldName}", userId, fieldName);
                return StatusCode(500, "Internal server error during update.");
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
            // Reload roles for potential return to view (moved up for cleaner structure)
            try
            {
                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching roles.");
                ViewBag.Roles = new List<string>();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    // --- 1. File Upload Processing ---
                    // These variables will hold the paths to be saved in the database
                    string profileImagePath = null;
                    string gpayQrCodePath = null;
                    string phonePeQrCodePath = null;

                    // Define the path where files will be saved (e.g., wwwroot/images)
                    string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "useruploads");
                    if (!Directory.Exists(uploadFolder))
                    {
                        Directory.CreateDirectory(uploadFolder);
                    }

                    // Helper function to save a file and return its path
                    async Task<string> SaveFileAsync(IFormFile file)
                    {
                        if (file == null || file.Length == 0) return null;

                        // Create a unique file name
                        string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        string filePath = Path.Combine(uploadFolder, uniqueFileName);

                        using (var fileStream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(fileStream);
                        }
                        // Return the relative path for database storage (e.g., /images/useruploads/guid_file.jpg)
                        return $"/images/useruploads/{uniqueFileName}";
                    }

                    // Process uploads
                    profileImagePath = await SaveFileAsync(model.ProfileImageFile);
                    gpayQrCodePath = await SaveFileAsync(model.GpayQRCodeFile);
                    phonePeQrCodePath = await SaveFileAsync(model.PhonePeQRCodeFile);

                    // --- 2. Create IdentityUser object ---
                    var user = new IdentityUser
                    {
                        UserName = model.UserName,
                        Email = model.Email,
                        EmailConfirmed = true,
                        PhoneNumber = model.PhoneNumber
                    };

                    // --- 3. Attempt to create IdentityUser in the database ---
                    var result = await _userManager.CreateAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        // ** START: UserProfile Creation **

                        // The user.Id is now set by Identity after a successful creation.
                        var userProfile = new UserProfile
                        {
                            UserId = user.Id, // Link profile to the new IdentityUser
                            FirstName = model.FirstName,
                            LastName = model.LastName,

                            // Map fields, using the new paths for the image fields
                            ITSNumber = model.ITSNumber,
                            ProfileImagePath = profileImagePath, // <-- Mapped the generated path
                            IsImageApproved = model.IsImageApproved,
                            IsProfileVisible = model.IsProfileVisible,
                            About = model.About,
                            Profession = model.Profession,
                            ServicesProvided = model.ServicesProvided,
                            LinkedInUrl = model.LinkedInUrl,
                            FacebookUrl = model.FacebookUrl,
                            InstagramUrl = model.InstagramUrl,
                            WhatsappNumber = model.WhatsappNumber,
                            HomeAddress = model.HomeAddress,
                            HomePhoneNumber = model.HomePhoneNumber,
                            BusinessAddress = model.BusinessAddress,
                            BusinessPhoneNumber = model.BusinessPhoneNumber,
                            GpayQRCodePath = gpayQrCodePath,      // <-- Mapped the generated path
                            PhonePeQRCodePath = phonePeQrCodePath // <-- Mapped the generated path
                        };

                        // Add and save the UserProfile to the database
                        _context.UserProfiles.Add(userProfile);
                        await _context.SaveChangesAsync();

                        // ** END: UserProfile Creation **

                        // --- 4. Assign Role (existing logic) ---
                        if (!string.IsNullOrWhiteSpace(model.Role))
                        {
                            if (await _roleManager.RoleExistsAsync(model.Role))
                            {
                                var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
                                if (!roleResult.Succeeded)
                                {
                                    _logger.LogError("Identity error adding role {Role} to user {UserName}. Errors: {Errors}",
                                        model.Role, model.UserName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));

                                    TempData["warning"] = $"User and Profile created, but failed to assign role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}";
                                    return RedirectToAction(nameof(Index));
                                }
                            }
                        }

                        // --- 5. Success ---
                        TempData["success"] = $"User '{model.UserName}' and Profile created successfully.";
                        return RedirectToAction(nameof(Index));
                    }

                    // --- 6. Handle Identity creation errors ---
                    foreach (var e in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, e.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                // General error handling for Identity, File System, and DbContext operations
                _logger.LogError(ex, "Unexpected error during user and profile creation for user: {UserName}", model.UserName);
                ModelState.AddModelError(string.Empty, "An unexpected error occurred during user or profile creation. Check logs for details.");
            }

            // Return the view with the model if ModelState is invalid or an error occurred
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

                // ** NEW: Fetch UserProfile **
                var userProfile = await _context.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == id);
                // If profile doesn't exist, create a new one (optional: depends on business rules)
                // For simplicity, we assume profile exists or we handle nulls gracefully.

                var roles = await _userManager.GetRolesAsync(user);

                // ** NEW: Map all Identity and UserProfile fields to EditUserModel **
                var model = new EditUserModel
                {
                    // IdentityUser Fields
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault(),

                    // UserProfile Fields (using null-conditional operator '?.' for safety)
                    FirstName = userProfile?.FirstName,
                    LastName = userProfile?.LastName,
                    ITSNumber = userProfile?.ITSNumber,
                    ProfileImagePath = userProfile?.ProfileImagePath,
                    IsImageApproved = userProfile?.IsImageApproved ?? false,
                    IsProfileVisible = userProfile?.IsProfileVisible ?? true,
                    About = userProfile?.About,
                    Profession = userProfile?.Profession,
                    ServicesProvided = userProfile?.ServicesProvided,
                    LinkedInUrl = userProfile?.LinkedInUrl,
                    FacebookUrl = userProfile?.FacebookUrl,
                    InstagramUrl = userProfile?.InstagramUrl,
                    WhatsappNumber = userProfile?.WhatsappNumber,
                    HomeAddress = userProfile?.HomeAddress,
                    HomePhoneNumber = userProfile?.HomePhoneNumber,
                    BusinessAddress = userProfile?.BusinessAddress,
                    BusinessPhoneNumber = userProfile?.BusinessPhoneNumber,
                    GpayQRCodePath = userProfile?.GpayQRCodePath,
                    PhonePeQRCodePath = userProfile?.PhonePeQRCodePath
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
            if (user == null)
            {
                TempData["error"] = "User not found during update.";
                return NotFound();
            }

            // ** NEW: Fetch and Update UserProfile **
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == model.Id);

            if (userProfile == null)
            {
                // Handle case where UserProfile might not exist (optional)
                TempData["error"] = "Associated User Profile not found. Cannot save profile details.";
                // Depending on your rules, you might redirect or attempt to create the profile here.
            }
            else
            {
                // Map updates from the model to the existing UserProfile entity
                userProfile.FirstName = model.FirstName;
                userProfile.LastName = model.LastName;
                userProfile.ITSNumber = model.ITSNumber;
                userProfile.ProfileImagePath = model.ProfileImagePath;
                userProfile.IsImageApproved = model.IsImageApproved;
                userProfile.IsProfileVisible = model.IsProfileVisible;
                userProfile.About = model.About;
                userProfile.Profession = model.Profession;
                userProfile.ServicesProvided = model.ServicesProvided;
                userProfile.LinkedInUrl = model.LinkedInUrl;
                userProfile.FacebookUrl = model.FacebookUrl;
                userProfile.InstagramUrl = model.InstagramUrl;
                userProfile.WhatsappNumber = model.WhatsappNumber;
                userProfile.HomeAddress = model.HomeAddress;
                userProfile.HomePhoneNumber = model.HomePhoneNumber;
                userProfile.BusinessAddress = model.BusinessAddress;
                userProfile.BusinessPhoneNumber = model.BusinessPhoneNumber;
                userProfile.GpayQRCodePath = model.GpayQRCodePath;
                userProfile.PhonePeQRCodePath = model.PhonePeQRCodePath;

                // Tell DbContext the entity has been modified
                _context.Update(userProfile);
            }
            // ** END: Fetch and Update UserProfile **

            try
            {
                // Store the current roles BEFORE the user object is updated
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Update Identity user details
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
                // Check the role AFTER the role update logic above has run
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

                // ** FINAL STEP: Save all context changes (UserProfile, SubscriberRequest) **
                // This is done once after all Identity operations (which call SaveChanges internally)
                await _context.SaveChangesAsync();

                TempData["success"] = $"User {user.UserName} and Profile updated successfully.";
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