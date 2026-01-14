using CMSECommerce.Areas.Admin.Models; // Assumed namespace for ViewModels
using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
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
        ILogger<UsersController> logger,
        IUserService userService) : Controller
    {
        private readonly UserManager<IdentityUser> _userManager = userManager;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly DataContext _context = context;
        private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly ILogger<UsersController> _logger = logger;
        private readonly IUserService _userService = userService;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                // 1. Efficient Data Retrieval (Single Query with LEFT JOIN)
                // Use GroupJoin and SelectMany (DefaultIfEmpty) to perform a SQL LEFT JOIN.
                // This ensures ALL IdentityUsers are returned, with 'Profile' being null if no matching UserProfile exists.
                var usersAndProfiles = await _userManager.Users.AsNoTracking()
                    .GroupJoin(
                        _context.UserProfiles.AsNoTracking(), // Inner sequence (UserProfiles)
                        user => user.Id,                       // Outer key (IdentityUser Id)
                        profile => profile.UserId,             // Inner key (UserProfile UserId)
                        (user, profiles) => new { User = user, Profiles = profiles } // Intermediate result
                    )
                    .SelectMany(
                        temp => temp.Profiles.DefaultIfEmpty(), // Perform Left Join: returns one row for each profile or one row with a null profile
                        (temp, profile) => new { User = temp.User, Profile = profile } // Final projection
                    )
                    .ToListAsync();

                var model = new List<UserViewModel>();

                foreach (var item in usersAndProfiles)
                {
                    var u = item.User;
                    // 'p' (Profile) will be NULL if the user has no UserProfile record (due to the Left Join).
                    var p = item.Profile;

                    // 2. Identity Operations (These still require a separate trip per user)
                    var roles = await _userManager.GetRolesAsync(u);
                    var isLocked = await _userManager.IsLockedOutAsync(u);

                    // 3. Mapping to UserViewModel
                    // Use the null-conditional operator (?.) to safely access profile fields.
                    model.Add(new UserViewModel
                    {
                        // IdentityUser Fields
                        Id = u.Id,
                        UserName = u.UserName,
                        Email = u.Email,
                        PhoneNumber = u.PhoneNumber,
                        Roles = roles,
                        IsLockedOut = isLocked,

                        // UserProfile Fields (safely access properties using '?.' and '??')
                        FirstName = p?.FirstName,
                        LastName = p?.LastName,
                        ITSNumber = p?.ITSNumber,

                        // *** ProfileImagePath is now safely handled with '?.' ***
                        ProfileImagePath = p?.ProfileImagePath,

                        // Boolean fields need to be handled with ?? false, as p?.IsField returns bool? (nullable bool)
                        IsImageApproved = p?.IsImageApproved ?? false,
                        IsProfileVisible = p?.IsProfileVisible ?? false,

                        About = p?.About,
                        Profession = p?.Profession,
                        ServicesProvided = p?.ServicesProvided,
                        LinkedInUrl = p?.LinkedInUrl,
                        FacebookUrl = p?.FacebookUrl,
                        InstagramUrl = p?.InstagramUrl,
                        WhatsappNumber = p?.WhatsAppNumber,
                        HomeAddress = p?.HomeAddress,
                        HomePhoneNumber = p?.HomePhoneNumber,
                        BusinessAddress = p?.BusinessAddress,
                        BusinessPhoneNumber = p?.BusinessPhoneNumber,
                        GpayQRCodePath = p?.GpayQRCodePath,
                        PhonePeQRCodePath = p?.PhonePeQRCodePath
                    });
                }

                // --- REMOVED SECTION ---
                // The block for 'Handle users without a UserProfile' is no longer needed 
                // because the Left Join includes them in the 'usersAndProfiles' list
                // with the 'Profile' object set to null.
                // --- END REMOVED SECTION ---

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


        // GET: Admin/UnlockRequests
        public async Task<IActionResult> UnlockRequests()
        {
            // Fetch only non-approved requests (Pending/Denied) as per your logic
            var requests = await _context.UnlockRequests
                .Where(p => p.Status != "Approved")
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }

        // POST: Admin/ProcessUnlock
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessUnlock(int id, string status, string notes)
        {
            // Basic validation for the ID
            if (id <= 0)
            {
                return Json(new { succeeded = false, message = "Invalid Request ID." });
            }

            // Validate that a status was actually provided
            if (string.IsNullOrEmpty(status))
            {
                return Json(new { succeeded = false, message = "Action status is required." });
            }

            try
            {
                // 1. Execute the service logic using the 3 required parameters:
                // requestId, status ("Approved"/"Denied"), and adminNotes
                var result = await _userService.ProcessUnlockRequestAsync(id, status, notes);

                if (result.Succeeded)
                {
                    // Return success to the AJAX caller
                    return Json(new { succeeded = true, message = result.Message });
                }

                // Return failure message from the service layer
                return Json(new { succeeded = false, message = result.Message });
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., _logger.LogError(ex, "Error processing unlock"))
                return Json(new { succeeded = false, message = "An internal server error occurred." });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserField(string userId, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(fieldName))
                return BadRequest("Missing required parameters.");

            var result = await _userService.UpdateUserFieldAsync(userId, fieldName, value);
            
            
            if (result.Succeeded) return Ok(result.Message);

            return result.StatusCode switch
            {
                404 => NotFound(result.Message),
                _ => BadRequest(result.Message)
            };
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableDisable(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["error"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            // Use a transaction to ensure atomicity (All-or-Nothing)
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                bool isCurrentlyLocked = await _userManager.IsLockedOutAsync(user);

                // 1. Determine new state
                DateTimeOffset? lockoutDate = isCurrentlyLocked ? null : DateTimeOffset.MaxValue;
                ProductStatus productStatus = isCurrentlyLocked ? ProductStatus.Approved : ProductStatus.Pending;
                string actionVerb = isCurrentlyLocked ? "enabled" : "permanently locked out";

                // 2. Update User Lockout Status
                var identityResult = await _userManager.SetLockoutEndDateAsync(user, lockoutDate);
                if (!identityResult.Succeeded)
                {
                    throw new Exception($"Identity Error: {string.Join(", ", identityResult.Errors.Select(e => e.Description))}");
                }

                if (isCurrentlyLocked)
                {
                    await _userManager.ResetAccessFailedCountAsync(user);
                }

                // 3. Optimize Product Updates: Use Bulk Update logic
                // We query for both UserName and Id once to handle legacy data consistency
                var productsToUpdate = await _context.Products
                    .Where(p => p.OwnerName == user.Id || p.OwnerName == user.UserName)
                    .ToListAsync();

                if (productsToUpdate.Any())
                {
                    foreach (var product in productsToUpdate)
                    {
                        product.Status = productStatus;
                    }
                    // EF Core will batch these updates automatically on SaveChangesAsync
                    _context.Products.UpdateRange(productsToUpdate);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["success"] = $"User '{user.UserName}' has been {actionVerb}.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to toggle status for user {UserId}", id);
                TempData["error"] = "A system error occurred while updating the account status.";
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

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckITSUnique([FromQuery] string itsNumber)
        {
            if (string.IsNullOrWhiteSpace(itsNumber) || itsNumber.Length != 8 || !int.TryParse(itsNumber, out _))
            {
                return Json(new { isAvailable = false, message = "ITS Number must be exactly 8 digits." });
            }

            try
            {
                // Check if any user already has this ITS Number
                // Assuming your ApplicationUser entity has an 'ITSNumber' property
                bool isTaken = await _context.UserProfiles.AnyAsync(u => u.ITSNumber == itsNumber);

                return Json(new { isAvailable = !isTaken, message = isTaken ? "This ITS Number is already registered." : "" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ITS validation failed");
                return StatusCode(500, "Validation error");
            }
        }


        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckUserNameUnique([FromQuery] string userName)
        {
            // 1. Guard Clause: Fast-fail on empty input
            if (string.IsNullOrWhiteSpace(userName))
            {
                return Json(true);
            }

            try
            {
                // 2. Optimization: Use Normalized fields for O(1) or O(log n) DB lookups
                // Identity indexes are typically tuned for NormalizedUserName and NormalizedEmail.
                var normalizedInput = userName.ToUpperInvariant();
                var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.ITSNumber == userName);
                if (userProfile != null)
                {
                    return Json(false);
                }
                // 3. Efficiency: Use AnyAsync instead of FirstOrDefaultAsync.
                // AnyAsync generates 'IF EXISTS' in SQL, which stops searching after the first match.
                // FirstOrDefaultAsync retrieves the entire row into memory, which is wasteful.
                bool isTaken = await _userManager.Users.AnyAsync(u =>
                    u.NormalizedUserName == normalizedInput ||
                    u.NormalizedEmail == normalizedInput ||
                    u.PhoneNumber == userName); // Phone numbers are usually treated as exact strings

                // 4. Return result: True means "Available", False means "Taken"
                return Json(!isTaken);
            }
            catch (Exception ex)
            {
                // 5. Resilience: Log the error and fail safe (assume taken to prevent collisions)
                _logger.LogError(ex, "Error validating uniqueness for identifier: {Identifier}", userName);
                return StatusCode(500, "Internal validation error");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckEmailUnique([FromQuery] string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { isAvailable = true });
            }

            // 1. Format Validation
            var emailAttribute = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            if (!emailAttribute.IsValid(email))
            {
                return Json(new { isAvailable = false, message = "Please enter a valid email address." });
            }

            try
            {
                string normalizedEmail = email.ToUpperInvariant();

                // 2. Uniqueness Check
                bool isEmailTaken = await _userManager.Users
                    .AnyAsync(u => u.NormalizedEmail == normalizedEmail);

                if (isEmailTaken)
                {
                    return Json(new { isAvailable = false, message = "Email address is already in use." });
                }

                return Json(new { isAvailable = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while validating email uniqueness for: {Email}", email);
                return StatusCode(StatusCodes.Status500InternalServerError, "Validation service unavailable.");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> CheckPhoneUnique([FromQuery] string phoneNumber)
        {
            // 1. Guard Clause: Format Validation (O(1) complexity)
            // We validate length and numeric integrity to prevent unnecessary DB round-trips.
            if (string.IsNullOrWhiteSpace(phoneNumber) ||
                phoneNumber.Length != 10 ||
                !long.TryParse(phoneNumber, out _))
            {
                // Architect's Note: Return false or a specific error object if the format is invalid.
                return Json(false);
            }

            try
            {
                // 2. Normalization
                // Identity lookups for usernames should always use the Normalized field (Upper case)
                // to leverage the database index effectively.
                string normalizedInput = phoneNumber.ToUpperInvariant();

                // 3. Atomic Data Access Strategy
                // We use 'AnyAsync' instead of 'FirstOrDefaultAsync'.
                // AnyAsync generates a SQL 'IF EXISTS' which is significantly faster and 
                // avoids loading the full user entity into the web server's memory.
                bool isConflict = await _userManager.Users.AnyAsync(u =>
                    u.PhoneNumber == phoneNumber ||
                    u.NormalizedUserName == normalizedInput);

                // 4. Result: Returns true if the phone number is available and valid.
                return Json(!isConflict);
            }
            catch (Exception ex)
            {
                // 5. Resilience & Observability
                // Log the exception but do not expose internal details to the client.
                _logger.LogError(ex, "Phone uniqueness validation failed for input: {Phone}", phoneNumber);

                return StatusCode(StatusCodes.Status500InternalServerError, "Validation service error");
            }
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
                            WhatsAppNumber = model.WhatsappNumber,
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

        // GET: Admin/Users/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            // Fetch Profile (creating a shell if it doesn't exist to prevent null reference in View)
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
            var roles = await _userManager.GetRolesAsync(user);

            var model = new EditUserModel
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = roles.FirstOrDefault(),

                // Profile mapping
                FirstName = profile?.FirstName ?? "",
                LastName = profile?.LastName ?? "",
                ITSNumber = profile?.ITSNumber,
                About = profile?.About,
                Profession = profile?.Profession,
                ServicesProvided = profile?.ServicesProvided,
                WhatsAppNumber = profile?.WhatsAppNumber,
                LinkedInUrl = profile?.LinkedInUrl,
                FacebookUrl = profile?.FacebookUrl,
                InstagramUrl = profile?.InstagramUrl,
                HomeAddress = profile?.HomeAddress,
                HomePhoneNumber = profile?.HomePhoneNumber,
                BusinessAddress = profile?.BusinessAddress,
                BusinessPhoneNumber = profile?.BusinessPhoneNumber,
                ProfileImagePath = profile?.ProfileImagePath,
                GpayQRCodePath = profile?.GpayQRCodePath,
                PhonePeQRCodePath = profile?.PhonePeQRCodePath,
                IsImageApproved = profile?.IsImageApproved ?? false,
                IsProfileVisible = profile?.IsProfileVisible ?? true
            };

            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        // POST: Admin/Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditUserModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            // 1. Update IdentityUser Basic Info
            user.UserName = model.UserName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // 2. Update Role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(model.Role))
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userManager.AddToRoleAsync(user, model.Role);
                    }
                }

                // 3. Handle Password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                }

                // 4. Update or Create UserProfile
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile == null)
                {
                    profile = new UserProfile { UserId = user.Id };
                    _context.UserProfiles.Add(profile);
                }

                profile.FirstName = model.FirstName;
                profile.LastName = model.LastName;
                profile.ITSNumber = model.ITSNumber;
                profile.About = model.About;
                profile.Profession = model.Profession;
                profile.ServicesProvided = model.ServicesProvided;
                profile.WhatsAppNumber = model.WhatsAppNumber;
                profile.LinkedInUrl = model.LinkedInUrl;
                profile.FacebookUrl = model.FacebookUrl;
                profile.InstagramUrl = model.InstagramUrl;
                profile.HomeAddress = model.HomeAddress;
                profile.HomePhoneNumber = model.HomePhoneNumber;
                profile.BusinessAddress = model.BusinessAddress;
                profile.BusinessPhoneNumber = model.BusinessPhoneNumber;
                profile.IsImageApproved = model.IsImageApproved;
                profile.IsProfileVisible = model.IsProfileVisible;

                // 5. Handle File Uploads
                if (model.ProfileImageFile != null)
                    profile.ProfileImagePath = await SaveFile(model.ProfileImageFile, "profiles");

                if (model.GpayQRCodeFile != null)
                    profile.GpayQRCodePath = await SaveFile(model.GpayQRCodeFile, "qrcodes");

                if (model.PhonePeQRCodeFile != null)
                    profile.PhonePeQRCodePath = await SaveFile(model.PhonePeQRCodeFile, "qrcodes");

                await _context.SaveChangesAsync();
                TempData["success"] = "User and Profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();
            return View(model);
        }

        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            string uploadDir = Path.Combine(_webHostEnvironment.WebRootPath, "media", subFolder);
            if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

            string fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            string filePath = Path.Combine(uploadDir, fileName);

            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fs);
            }

            return $"/media/{subFolder}/{fileName}";
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
                var userProductsByUserName = await _context.Products.Where(p => p.OwnerName == user.UserName).ToListAsync();
                var userProductsByOwerId = await _context.Products.Where(p => p.OwnerName == user.Id).ToListAsync();
                // **WARNING:** Deleting products may require file system cleanup of images/gallery. 
                // This is a complex dependency that should be handled explicitly.
                _context.Products.RemoveRange(userProductsByUserName);
                _context.Products.RemoveRange(userProductsByOwerId);

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

        [HttpGet]
        public async Task<IActionResult> ExportUsersCsv()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Id,UserName,Email,PhoneNumber,Roles,HasProfile,ProfileId,ITSNumber");

                foreach (var u in users)
                {
                    var roles = await _userManager.GetRolesAsync(u);
                    var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == u.Id);
                    var its = profile?.ITSNumber ?? string.Empty;
                    var profileId = profile?.Id.ToString() ?? string.Empty;
                    sb.AppendLine($"{u.Id},\"{u.UserName}\",\"{u.Email}\",\"{u.PhoneNumber}\",\"{string.Join(";", roles)}\",{(profile!=null)},{profileId},\"{its}\"");
                }

                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                return File(bytes, "text/csv", $"users_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting users CSV");
                return StatusCode(500, "Failed to export users.");
            }
        }
    }
}