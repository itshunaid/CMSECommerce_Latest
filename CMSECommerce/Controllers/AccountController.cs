using CMSECommerce.Areas.Admin.Controllers;
using CMSECommerce.Areas.Admin.Models;
using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Controllers
{
    public class AccountController(
            DataContext dataContext,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            IUserStatusService userStatusService,
            RoleManager<IdentityRole> roleManager,
            ILogger<AccountController> logger,
            IUserService userService) : Controller
    {
        private DataContext _context = dataContext;
        private UserManager<IdentityUser> _userManager = userManager;
        private SignInManager<IdentityUser> _sign_in_manager = signInManager;
        private IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly IUserStatusService _userStatusService = userStatusService;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly ILogger<AccountController> _logger = logger;
        private readonly IUserService _userService = userService;


        private async Task UpdateOrderShippedStatus(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return; // No username provided, exit early.
            }

            try
            {
                // The current 'username' variable is assumed to hold the case-insensitive username to match.
                string usernameLower = username.ToLower();

                // 1. Get ALL Order IDs for the current user (case-insensitive)
                var userOrderIds = await _context.Orders
                    .Where(o => o.UserName.ToLower() == usernameLower) // Convert both sides to lowercase for comparison
                    .Select(o => o.Id)
                    .ToListAsync();

                // 2. Find all OrderDetail items for those orders
                var allOrderDetails = await _context.OrderDetails
                    .Where(d => userOrderIds.Contains(d.OrderId))
                    .ToListAsync();

                // 3. Group the OrderDetails by OrderId
                var ordersGroupedByDetail = allOrderDetails
                    .GroupBy(d => d.OrderId)
                    .ToList();

                // 4. Find the Order IDs where ALL details are processed
                var ordersReadyToShipIds = ordersGroupedByDetail
                    .Where(group => group.All(d => d.IsProcessed == true))
                    .Select(group => group.Key)
                    .ToList();

                // 5. Retrieve the actual Order entities that need updating and haven't been shipped yet
                var ordersToUpdate = await _context.Orders
                    .Where(o => ordersReadyToShipIds.Contains(o.Id) && o.Shipped == false)
                    .ToListAsync();

                // 6. Update the 'Shipped' status
                foreach (var order in ordersToUpdate)
                {
                    order.Shipped = true;
                }

                // 7. Save changes to the database
                if (ordersToUpdate.Any())
                {
                    // The critical database save operation
                    await _context.SaveChangesAsync();
                }
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException ex)
            {
                // Log the concurrency error (e.g., another process updated the same record)
                // _logger.LogError(ex, "Concurrency error occurred while updating shipment status for user: {username}", username);
                // Depending on requirements, you might retry the operation or log and continue.
            }
            catch (Exception ex)
            {
                // Log all other database access or logic errors
                // _logger.LogError(ex, "An error occurred while updating shipment status for user: {username}", username);
                // Optionally, re-throw the exception if the calling method needs to know the operation failed.
                // throw; 
            }
        }


        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;
            List<CMSECommerce.Models.Order> orders = new();

            try
            {
                // Call the dedicated method to update statuses before displaying the list
                await UpdateOrderShippedStatus(username);

                // Now, fetch the complete, updated list of orders for the user
                orders = await _context.Orders
                    .OrderByDescending(x => x.Id)
                    // Note: Your original code used a case-sensitive check here, 
                    // but the Status update used case-insensitive. Be consistent.
                    .Where(x => x.UserName == username)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log the error during order fetching or status update
                // _logger.LogError(ex, "Error retrieving orders for user: {username}", username);
                TempData["error"] = "An error occurred while loading your orders.";
                // Fallback: return an empty list or redirect if necessary
                return View(orders);
            }

            return View(orders);
        }


        public IActionResult Register()
        {
            // Assuming a ViewModel named 'User' is used for registration.
            return View();
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
        public async Task<IActionResult> Register(User user)
        {
            // 1. Validation and Uniqueness Checks (Same as before)
            user.FirstName ??= "First Name";
            user.LastName ??= "Last Name";

            if (!ModelState.IsValid) return View(user);

            var existingEmail = await _userManager.FindByEmailAsync(user.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError("Email", "Email address is already in use.");
                return View(user);
            }

            // 2. Create the Identity User
            IdentityUser newUser = new()
            {
                UserName = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber
            };

            try
            {
                IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);

                if (result.Succeeded)
                {
                    try
                    {
                        // 3. Map Address details to the UserProfile model
                        // We combine the registration address fields into a single string for the Profile
                        string fullAddress = $"{user.StreetAddress}, {user.City}, {user.State} {user.PostalCode}, {user.Country}";

                        var newProfile = new UserProfile
                        {
                            UserId = newUser.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            // Mapping Address details to UserProfile fields
                            HomeAddress = fullAddress,
                            HomePhoneNumber = user.PhoneNumber,
                            BusinessPhoneNumber = user.PhoneNumber, // Defaulting to same as home

                            // Setting default visibility and status
                            IsProfileVisible = true,
                            IsImageApproved = false,
                            IsImagePending = false
                        };

                        _context.UserProfiles.Add(newProfile);

                        // 4. Map Address details to the separate Address table (Relational)
                        var newAddress = new CMSECommerce.Models.Address
                        {
                            UserId = newUser.Id,
                            StreetAddress = user.StreetAddress,
                            City = user.City,
                            State = user.State,
                            PostalCode = user.PostalCode,
                            Country = user.Country
                        };

                        _context.Addresses.Add(newAddress);

                        // Save all changes to the database
                        await _context.SaveChangesAsync();

                        // 5. Finalize
                        await _userManager.AddToRoleAsync(newUser, "Customer");

                        TempData["success"] = "You have registered successfully!";
                        return RedirectToAction("Login");
                    }
                    catch (Exception dbEx)
                    {
                        // Rollback Identity user if Profile/Address fails
                        await _userManager.DeleteAsync(newUser);
                        ModelState.AddModelError("", "Database error occurred while saving profile details.");
                        return View(user);
                    }
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "A critical error occurred. Please try again later.");
            }

            return View(user);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            // Fetches the IdentityUser based on the current authenticated user
            var identityUser = await _userManager.GetUserAsync(User);

            // Safety check: If user cannot be found, redirect to login
            if (identityUser == null) return RedirectToAction("Login");

            try
            {
                // Attempt to find existing profile data
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                // Map Identity data to the ViewModel
                var viewModel = new ProfileUpdateViewModel
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    PhoneNumber = identityUser.PhoneNumber,
                };

                if (userProfile != null)
                {
                    // Map existing UserProfile data to the ViewModel
                    viewModel.FirstName = userProfile.FirstName;
                    viewModel.LastName = userProfile.LastName;
                    viewModel.IsProfileVisible = userProfile.IsProfileVisible;
                    viewModel.ITSNumber = userProfile.ITSNumber;
                    viewModel.About = userProfile.About;
                    viewModel.Profession = userProfile.Profession;
                    viewModel.ServicesProvided = userProfile.ServicesProvided;
                    viewModel.LinkedInUrl = userProfile.LinkedInUrl;
                    viewModel.FacebookUrl = userProfile.FacebookUrl;
                    viewModel.InstagramUrl = userProfile.InstagramUrl;
                    viewModel.WhatsappNumber = userProfile.WhatsAppNumber;
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;

                    // --- NECESSARY CHANGES START HERE ---
                    // 1. Map existing approved image and approval status
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.IsImageApproved = userProfile.IsImageApproved;

                    // 2. Map the new pending image path and pending status
                    viewModel.PendingProfileImagePath = userProfile.PendingProfileImagePath;
                    viewModel.IsImagePending = userProfile.IsImagePending;
                    // --- NECESSARY CHANGES END HERE ---

                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                }

                // Return the View with the populated ViewModel (for the edit form)
                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error (e.g., database connection failure)
                // _logger.LogError(ex, "Error fetching profile for user {UserId}", identityUser.Id);
                TempData["error"] = "An error occurred while loading your profile data.";
                return RedirectToAction("Index", "Home"); // Redirect to a safe page on failure
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile(string id)
        {
            try
            {
                // 1. ADMIN AUTHORIZATION CHECK (Recommended for multi-user edit actions)
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    TempData["error"] = "You are not authorized to edit other user profiles.";
                    // Redirect to the user's own profile page or a restricted access page
                    return RedirectToAction("Profile");
                }

                var identityUser = await _userManager.FindByIdAsync(id);

                if (identityUser == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction("ProfileDetails");
                }

                // Attempt to find existing profile data
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == id);

                // Map data to the ViewModel
                var viewModel = new ProfileUpdateViewModel
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    PhoneNumber = identityUser.PhoneNumber
                };

                if (userProfile != null)
                {
                    // Map common properties
                    viewModel.FirstName = userProfile.FirstName;
                    viewModel.LastName = userProfile.LastName;
                    viewModel.IsProfileVisible = userProfile.IsProfileVisible;
                    viewModel.ITSNumber = userProfile.ITSNumber;
                    viewModel.About = userProfile.About;
                    viewModel.Profession = userProfile.Profession;
                    viewModel.ServicesProvided = userProfile.ServicesProvided;
                    viewModel.LinkedInUrl = userProfile.LinkedInUrl;
                    viewModel.FacebookUrl = userProfile.FacebookUrl;
                    viewModel.InstagramUrl = userProfile.InstagramUrl;
                    viewModel.WhatsappNumber = userProfile.WhatsAppNumber;
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;

                    // --- NECESSARY CHANGES START HERE: Mapping Image Approval Fields ---
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.IsImageApproved = userProfile.IsImageApproved;

                    // Map the new pending image path and pending status
                    viewModel.PendingProfileImagePath = userProfile.PendingProfileImagePath;
                    viewModel.IsImagePending = userProfile.IsImagePending;
                    // --- NECESSARY CHANGES END HERE ---

                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                }

                // Return the common profile edit view with the data
                return View("Profile", viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                // _logger.LogError(ex, "Error fetching profile for editing for ID: {id}", id);
                TempData["error"] = "An error occurred while preparing the profile for editing.";
                return RedirectToAction("ProfileDetails");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ProfileDetails(string id = "", bool ITSAvailable = false)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = (await _userManager.GetUserAsync(User))?.Id;
            }

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
                    WhatsAppNumber = userProfile?.WhatsAppNumber,
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ProfileDetailsView(string userId = "")
        {
            // 1. Determine the user ID to view (current user or specified user)
            if (string.IsNullOrEmpty(userId))
            {
                userId = (await _userManager.GetUserAsync(User))?.Id;
            }

            if (string.IsNullOrEmpty(userId))
            {
                // Should not happen under [Authorize], but a safeguard
                TempData["error"] = "User ID could not be determined. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 2. Fetch Identity User details
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return NotFound();
                }

                // 3. Fetch related data (UserProfile and Roles)
                var userProfile = await _context.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);
                var roles = await _userManager.GetRolesAsync(user);

                // 4. Map data to the ViewModel (EditUserModel is used here as it contains all necessary properties)
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

                    // Status Flags
                    IsImageApproved = userProfile?.IsImageApproved ?? false,
                    IsProfileVisible = userProfile?.IsProfileVisible ?? true,

                    // Details
                    About = userProfile?.About,
                    Profession = userProfile?.Profession,
                    ServicesProvided = userProfile?.ServicesProvided,

                    // Social Links
                    LinkedInUrl = userProfile?.LinkedInUrl,
                    FacebookUrl = userProfile?.FacebookUrl,
                    InstagramUrl = userProfile?.InstagramUrl,
                    WhatsAppNumber = userProfile?.WhatsAppNumber,

                    // Addresses
                    HomeAddress = userProfile?.HomeAddress,
                    HomePhoneNumber = userProfile?.HomePhoneNumber,
                    BusinessAddress = userProfile?.BusinessAddress,
                    BusinessPhoneNumber = userProfile?.BusinessPhoneNumber,

                    // QRCodes
                    GpayQRCodePath = userProfile?.GpayQRCodePath,
                    PhonePeQRCodePath = userProfile?.PhonePeQRCodePath
                };

                // 5. Return the model to the ProfileDetails.cshtml view (read-only)
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error loading user ID: {UserId} details.", userId);
                TempData["error"] = "An error occurred while loading the profile details.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileDetails(EditUserModel model)
        {
            // Reload roles immediately for failed path (moved up for better UX)
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
                // Keep the current paths in the model for the return view
                // The `IFormFile` properties will be null, which is fine.
                return View("ProfileDetailsView", model);
            }

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                TempData["error"] = "User not found during update.";
                return NotFound();
            }

            // --- NEW: File Upload Logic Helper (Reusable, assuming controller has IWebHostEnvironment: _webHostEnvironment) ---
            async Task<string> SaveFileAndGetPathAsync(IFormFile file, string currentPath)
            {
                if (file == null || file.Length == 0)
                {
                    // No new file uploaded, keep the current path
                    return currentPath;
                }

                // 1. Delete old file if it exists and is a relative path (starts with /)
                if (!string.IsNullOrWhiteSpace(currentPath) && currentPath.StartsWith("/"))
                {
                    string oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, currentPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldFilePath))
                    {
                        System.IO.File.Delete(oldFilePath);
                    }
                }

                // 2. Save new file
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "useruploads");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // Return the new relative path for database storage
                return $"/images/useruploads/{uniqueFileName}";
            }
            // ---------------------------------------------------------------------------------------------------------------------

            // ** Fetch and Update UserProfile **
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == model.Id);

            if (userProfile == null)
            {
                TempData["error"] = "Associated User Profile not found. Cannot save profile details.";
                // Depending on business rules, you might want to stop or attempt to create profile here.
            }
            else
            {
                try
                {
                    // 1. Handle File Uploads and update paths in the UserProfile entity
                    userProfile.ProfileImagePath = await SaveFileAndGetPathAsync(model.ProfileImageFile, model.ProfileImagePath);
                    userProfile.GpayQRCodePath = await SaveFileAndGetPathAsync(model.GpayQRCodeFile, model.GpayQRCodePath);
                    userProfile.PhonePeQRCodePath = await SaveFileAndGetPathAsync(model.PhonePeQRCodeFile, model.PhonePeQRCodePath);

                    // 2. Map the remaining updates from the model to the existing UserProfile entity
                    userProfile.FirstName = model.FirstName;
                    userProfile.LastName = model.LastName;
                    userProfile.ITSNumber = model.ITSNumber;
                    // The path fields are updated above based on uploads/current value:
                    // userProfile.ProfileImagePath = model.ProfileImagePath; // <-- No longer direct mapping
                    userProfile.IsImageApproved = model.IsImageApproved;
                    userProfile.IsProfileVisible = model.IsProfileVisible;
                    userProfile.About = model.About;
                    userProfile.Profession = model.Profession;
                    userProfile.ServicesProvided = model.ServicesProvided;
                    userProfile.LinkedInUrl = model.LinkedInUrl;
                    userProfile.FacebookUrl = model.FacebookUrl;
                    userProfile.InstagramUrl = model.InstagramUrl;
                    userProfile.WhatsAppNumber = model.WhatsAppNumber;
                    userProfile.HomeAddress = model.HomeAddress;
                    userProfile.HomePhoneNumber = model.HomePhoneNumber;
                    userProfile.BusinessAddress = model.BusinessAddress;
                    userProfile.BusinessPhoneNumber = model.BusinessPhoneNumber;
                    // userProfile.GpayQRCodePath = model.GpayQRCodePath; // <-- No longer direct mapping
                    // userProfile.PhonePeQRCodePath = model.PhonePeQRCodePath; // <-- No longer direct mapping

                    // Tell DbContext the entity has been modified
                    _context.Update(userProfile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "File operation error during user profile update for ID: {UserId}", model.Id);
                    ModelState.AddModelError(string.Empty, "An error occurred during file upload. Please try again.");
                    // Re-throw or return view here if you want to stop Identity update on file error
                    return View("ProfileDetailsView", model);
                }
            }
            // ** END: Fetch and Update UserProfile & File Logic **

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

                    // Re-map the current (unmodified) paths back to the model before returning the view
                    if (userProfile != null)
                    {
                        model.ProfileImagePath = userProfile.ProfileImagePath;
                        model.GpayQRCodePath = userProfile.GpayQRCodePath;
                        model.PhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                    }
                    return View("ProfileDetailsView", model);
                }

                // --- Role Update Logic ---
                // (Existing logic remains the same)
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
                // (Existing logic remains the same)
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
                // (Existing logic remains the same)
                if (!string.IsNullOrWhiteSpace(model.NewPassword))
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    var passResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
                    if (!passResult.Succeeded)
                    {
                        foreach (var e in passResult.Errors)
                            ModelState.AddModelError(string.Empty, e.Description);

                        // Re-map the current (unmodified) paths back to the model before returning the view
                        if (userProfile != null)
                        {
                            model.ProfileImagePath = userProfile.ProfileImagePath;
                            model.GpayQRCodePath = userProfile.GpayQRCodePath;
                            model.PhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                        }
                        return View("ProfileDetailsView", model);
                    }
                }

                // ** FINAL STEP: Save all context changes (UserProfile, SubscriberRequest) **
                // This is done once after all Identity operations (which call SaveChanges internally)
                await _context.SaveChangesAsync();

                TempData["success"] = $"User {user.UserName} and Profile updated successfully.";
                return View("ProfileDetailsView",model);
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

            // Ensure model has current paths if execution reaches here due to an exception
            if (userProfile != null)
            {
                model.ProfileImagePath = userProfile.ProfileImagePath;
                model.GpayQRCodePath = userProfile.GpayQRCodePath;
                model.PhonePeQRCodePath = userProfile.PhonePeQRCodePath;
            }
            return View("ProfileDetailsView", model);
        }

       

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken] // Recommended for security
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel viewModel)
        {
            // 1. Model Validation
            if (!ModelState.IsValid)
            {
                // If validation fails, return to the view with errors.
                // You may need to repopulate ViewBag.ITSAvailable here if it's dynamic
                ViewBag.ITSAvailable = !string.IsNullOrEmpty(viewModel.ITSNumber);
                return View("ProfileDetails", viewModel);
            }

            try
            {
                // 2. Retrieve Existing Data
                var identityUser = await _userManager.FindByIdAsync(viewModel.UserId);

                // Ensure user exists
                if (identityUser == null)
                {
                    TempData["error"] = "Error: User not found.";
                    return RedirectToAction("Login", "Account");
                }

                // Retrieve or create the UserProfile record
                var userProfile = await _context.UserProfiles
                                                .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                // If UserProfile doesn't exist, create a new one (This ensures data integrity)
                if (userProfile == null)
                {
                    userProfile = new UserProfile { UserId = identityUser.Id };
                    _context.UserProfiles.Add(userProfile);
                }

                // 3. Handle File Uploads (Profile Image)
                if (viewModel.ProfileImageUpload != null)
                {
                    // NOTE: This assumes you have a helper method (e.g., SaveFile)
                    // and a storage path injected/defined (e.g., _webHostEnvironment.WebRootPath).
                    // Image saving logic MUST be implemented securely.

                    // 3a. Save the new file to a temporary/pending location
                    string newImagePath = await SaveFile(viewModel.ProfileImageUpload, "profileimages/pending");

                    // 3b. Update the pending path and set review flags
                    userProfile.PendingProfileImagePath = newImagePath;
                    userProfile.IsImagePending = true;

                    // Optionally clear the old approved image path if a new one is pending
                    // userProfile.ProfileImagePath = null; 
                    userProfile.IsImageApproved = false;

                    TempData["message"] = "Profile image uploaded successfully and is awaiting administrator approval.";
                }

                // 3. Handle File Uploads (Gpay QR)
                if (viewModel.GpayQRCodeUpload != null)
                {
                    userProfile.GpayQRCodePath = await SaveFile(viewModel.GpayQRCodeUpload, "qrcodes/gpay");
                }

                // 3. Handle File Uploads (PhonePe QR)
                if (viewModel.PhonePeQRCodeUpload != null)
                {
                    userProfile.PhonePeQRCodePath = await SaveFile(viewModel.PhonePeQRCodeUpload, "qrcodes/phonepe");
                }

                // 4. Update IdentityUser Data
                // The IdentityUser.Email and UserName are often handled through separate, more rigorous flows.
                // We focus on the PhoneNumber here as it's directly editable in the view.
                if (identityUser.PhoneNumber != viewModel.PhoneNumber)
                {
                    identityUser.PhoneNumber = viewModel.PhoneNumber;
                    var result = await _userManager.UpdateAsync(identityUser);
                    if (!result.Succeeded)
                    {
                        TempData["error"] = "Error updating phone number.";
                        // Optionally add errors to ModelState here
                    }
                }

                // 5. Map updated properties to UserProfile Entity
                userProfile.FirstName = viewModel.FirstName;
                userProfile.LastName = viewModel.LastName;
                userProfile.Profession = viewModel.Profession;
                userProfile.About = viewModel.About;
                userProfile.ServicesProvided = viewModel.ServicesProvided;
                userProfile.IsProfileVisible = viewModel.IsProfileVisible;

                // Social/Contact Information
                userProfile.LinkedInUrl = viewModel.LinkedInUrl;
                userProfile.FacebookUrl = viewModel.FacebookUrl;
                userProfile.InstagramUrl = viewModel.InstagramUrl;
                userProfile.WhatsAppNumber = viewModel.WhatsappNumber;

                // Address Information
                userProfile.HomeAddress = viewModel.HomeAddress;
                userProfile.HomePhoneNumber = viewModel.HomePhoneNumber;
                userProfile.BusinessAddress = viewModel.BusinessAddress;
                userProfile.BusinessPhoneNumber = viewModel.BusinessPhoneNumber;

                // NOTE: ITSNumber is usually read-only/mapped in the GET action, 
                // but ensure you don't overwrite it here if the ViewModel property is empty.
                // If the database is the source of truth, it should retain its value.

                // 6. Save Changes
                await _context.SaveChangesAsync();

                TempData["success"] = TempData["message"] != null ?
                                      TempData["message"] :
                                      "Profile details updated successfully!";

                // Redirect back to the GET action to show the updated data
                return RedirectToAction("ProfileDetails", new { userId = viewModel.UserId });
            }
            catch (Exception ex)
            {
                // 7. Error Handling
                // Log the error (if using ILogger)
                // _logger.LogError(ex, "Error updating profile for user ID: {UserId}", viewModel.UserId);

                TempData["error"] = "An unexpected error occurred while saving your profile. Please try again.";
                // Return to view with error state
                ViewBag.ITSAvailable = !string.IsNullOrEmpty(viewModel.ITSNumber);
                return View("ProfileDetails", viewModel);
            }
        }

        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0) return null;

            // Get the path to the wwwroot folder
            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, subFolder);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            // Generate a unique file name to prevent collision
            var uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
            var filePath = Path.Combine(uploadPath, uniqueFileName);

            // Save the file
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            // Return the relative path for the database (e.g., /profileimages/pending/GUID_file.jpg)
            return $"/{subFolder}/{uniqueFileName}";
        }


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileUpdateViewModel model)
        {
            // Note: Assumes necessary services (_userManager, _context, _webHostEnvironment) 
            // and using statements (System.IO, etc.) are available in the Controller.

            if (!ModelState.IsValid)
            {
                // Re-display the form with validation errors
                return View(model);
            }

            // Find Identity User - wrap in try-catch for identity/db failure
            IdentityUser identityUser;
            try
            {
                identityUser = await _userManager.FindByIdAsync(model.UserId);
            }
            catch (Exception ex)
            {
                // Log Identity lookup failure
                // _logger.LogError(ex, "Identity lookup failed for user ID: {UserId}", model.UserId);
                TempData["error"] = "A database error occurred during user verification.";
                return RedirectToAction("ProfileDetails");
            }

            if (identityUser == null)
            {
                TempData["error"] = "User not found or session expired.";
                return RedirectToAction("Login");
            }

            // --- 1. Update IdentityUser Basic Info ---
            identityUser.UserName = model.UserName;
            identityUser.Email = model.Email;
            identityUser.PhoneNumber = model.PhoneNumber;

            // Save IdentityUser changes (includes checking for username/email conflicts)
            try
            {
                var identityResult = await _userManager.UpdateAsync(identityUser);
                if (!identityResult.Succeeded)
                {
                    foreach (var error in identityResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                // Log Identity update failure (e.g., database connection)
                // _logger.LogError(ex, "Error updating IdentityUser data for ID: {UserId}", model.UserId);
                ModelState.AddModelError("", "A critical error occurred while updating your login details.");
                return View(model);
            }


            // --- 2. Handle UserProfile and File Uploads ---
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

            bool isNewProfile = userProfile == null;
            if (isNewProfile)
            {
                userProfile = new UserProfile { UserId = identityUser.Id };
            }

            // FIX: Declared at this scope so it is accessible outside the I/O try block
            bool profileImageUploaded = false;

            // Separate the I/O logic for better error handling visibility
            try
            {
                // File Upload Helper Function
                async Task<string> ProcessFileUpload(IFormFile file, string subFolder)
                {
                    if (file == null) return null;

                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", subFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    // Ensure the directory exists (I/O operation)
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        // File Copy operation (I/O operation)
                        await file.CopyToAsync(fileStream);
                    }
                    return Path.Combine("images", subFolder, uniqueFileName).Replace("\\", "/");
                }

                // --- Process Profile Image for Approval ---
                if (model.ProfileImageUpload != null)
                {
                    // 1. Save the new image to the PENDING folder/path
                    string newPendingPath = await ProcessFileUpload(model.ProfileImageUpload, "profiles/pending");

                    // 2. Delete the previously pending image (if one existed)
                    if (!string.IsNullOrEmpty(userProfile.PendingProfileImagePath))
                    {
                        try
                        {
                            string oldPendingPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.PendingProfileImagePath);
                            if (System.IO.File.Exists(oldPendingPath)) System.IO.File.Delete(oldPendingPath);
                        }
                        catch (Exception deleteEx)
                        {
                            // Log but don't stop the update
                            // _logger.LogWarning(deleteEx, "Failed to delete old pending profile image: {Path}", userProfile.PendingProfileImagePath);
                        }
                    }

                    // 3. Update the UserProfile properties
                    userProfile.PendingProfileImagePath = newPendingPath;
                    userProfile.IsImageApproved = false; // Must be reset to false when a new image is pending
                    userProfile.IsImagePending = true;  // Mark as pending

                    profileImageUploaded = true; // Set the flag
                }

                // Process GPay/PhonePe QR Codes (omitting old file delete for brevity, but it should be added)
                if (model.GpayQRCodeUpload != null)
                {
                    // DELETE existing GPay QR code before saving new one
                    if (!string.IsNullOrEmpty(userProfile.GpayQRCodePath))
                    {
                        // Delete old file logic here
                    }
                    userProfile.GpayQRCodePath = await ProcessFileUpload(model.GpayQRCodeUpload, "qrcodes");
                }
                if (model.PhonePeQRCodeUpload != null)
                {
                    // DELETE existing PhonePe QR code before saving new one
                    if (!string.IsNullOrEmpty(userProfile.PhonePeQRCodePath))
                    {
                        // Delete old file logic here
                    }
                    userProfile.PhonePeQRCodePath = await ProcessFileUpload(model.PhonePeQRCodeUpload, "qrcodes");
                }
            }
            catch (IOException ioEx)
            {
                // Log File I/O failure (permission, disk full, etc.)
                // _logger.LogError(ioEx, "File I/O error during profile update for user ID: {UserId}", model.UserId);
                ModelState.AddModelError("", "Error saving file attachments. Check server disk space or permissions.");
                return View(model);
            }
            catch (Exception ex)
            {
                // Log any other file upload processing errors
                // _logger.LogError(ex, "Unexpected error during file upload for user ID: {UserId}", model.UserId);
                ModelState.AddModelError("", "An unexpected error occurred during file upload.");
                return View(model);
            }

            // --- 3. Update UserProfile Details and Save to DB ---
            // Map data fields from ViewModel to UserProfile
            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;
            userProfile.IsProfileVisible = model.IsProfileVisible;
            userProfile.ITSNumber = model.ITSNumber;
            userProfile.About = model.About;
            userProfile.Profession = model.Profession;
            userProfile.ServicesProvided = model.ServicesProvided;
            userProfile.LinkedInUrl = model.LinkedInUrl;
            userProfile.FacebookUrl = model.FacebookUrl;
            userProfile.InstagramUrl = model.InstagramUrl;
            userProfile.WhatsAppNumber = model.WhatsappNumber;
            userProfile.HomeAddress = model.HomeAddress;
            userProfile.HomePhoneNumber = model.HomePhoneNumber;
            userProfile.BusinessAddress = model.BusinessAddress;
            userProfile.BusinessPhoneNumber = model.BusinessPhoneNumber;

            try
            {
                if (isNewProfile)
                {
                    _context.UserProfiles.Add(userProfile);
                }
                else
                {
                    _context.UserProfiles.Update(userProfile);
                }

                await _context.SaveChangesAsync();
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Log the DB save error (e.g., constraint violation, data too long)
                // _logger.LogError(dbEx, "Database update failed for UserProfile ID: {UserId}", model.UserId);
                ModelState.AddModelError("", "Error saving profile details due to a database constraint.");
                return View(model);
            }
            catch (Exception ex)
            {
                // Log generic DB save error
                // _logger.LogError(ex, "Unexpected database error during UserProfile save for ID: {UserId}", model.UserId);
                ModelState.AddModelError("", "An unexpected error occurred while saving profile data.");
                return View(model);
            }

            // Set success message based on whether an image upload was submitted
            if (profileImageUploaded)
            {
                TempData["success"] = "Your profile and new image have been saved. The new profile image is now **pending review** by an administrator.";
            }
            else
            {
                TempData["success"] = "Your profile details have been updated successfully.";
            }

            // Redirect to the Profile Details page
            return RedirectToAction("ProfileDetails", new { userId = userProfile.UserId });
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProfileData()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                return Unauthorized();
            }

            try
            {
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                if (userProfile != null)
                {
                    // Helper function for file deletion to keep the main logic clean
                    void DeleteFileIfPresent(string relativePath)
                    {
                        if (string.IsNullOrEmpty(relativePath)) return;
                        try
                        {
                            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/'));
                            if (System.IO.File.Exists(fullPath))
                            {
                                System.IO.File.Delete(fullPath);
                            }
                        }
                        catch (Exception deleteEx)
                        {
                            // Log a warning if deletion fails (e.g., file lock), but continue the database operation
                            // _logger.LogWarning(deleteEx, "Failed to delete file: {Path}", relativePath);
                        }
                    }

                    // Delete associated files (images, QR codes)
                    DeleteFileIfPresent(userProfile.ProfileImagePath);
                    DeleteFileIfPresent(userProfile.GpayQRCodePath);
                    DeleteFileIfPresent(userProfile.PhonePeQRCodePath);

                    // Remove and save
                    _context.UserProfiles.Remove(userProfile);
                    await _context.SaveChangesAsync();

                    TempData["success"] = "Your custom profile data has been deleted!";
                }
                else
                {
                    TempData["info"] = "No custom profile data found to delete.";
                }

                // Redirect back to the profile page (which will now show default values)
                return RedirectToAction("ProfileDetails");
            }
            catch (Exception ex)
            {
                // Log the error during DB save or file deletion lookup
                // _logger.LogError(ex, "Error deleting profile data for user ID: {UserId}", identityUser.Id);
                TempData["error"] = "An error occurred while deleting your profile data.";
                return RedirectToAction("ProfileDetails");
            }
        }
        public IActionResult Login(string returnUrl)
        {
           
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (!ModelState.IsValid)
                return View(loginVM);

            string returnUrl = loginVM.ReturnUrl ?? "/";

            try
            {
                var user = await ResolveUserAsync(loginVM.UserName);
                if (user == null) return InvalidLoginResponse(loginVM);

                ViewBag.UserId = user.Id;

                var result = await _sign_in_manager.PasswordSignInAsync(
                    user.UserName,
                    loginVM.Password,
                    isPersistent: false,
                    lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    await UpdateUserStatusAsync(user.Id);
                    return LocalRedirect(returnUrl);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {UserName}", loginVM.UserName);
                    ViewBag.IsLockedOut = true;

                    // Fetch the most recent request to see the Admin's decision/notes
                    var lastRequest = await _context.UnlockRequests
                        .Where(ur => ur.UserId == user.Id)
                        .OrderByDescending(ur => ur.RequestDate)
                        .FirstOrDefaultAsync();

                    string statusMessage;

                    if (lastRequest != null)
                    {
                        // If the last request was denied, show the Admin Note (the reason)
                        if (lastRequest.Status == "Denied" && !string.IsNullOrEmpty(lastRequest.AdminNotes))
                        {
                            statusMessage = $"Your request was denied. Admin Note: {lastRequest.AdminNotes}";
                        }
                        else if (lastRequest.Status == "Pending")
                        {
                            statusMessage = "An unlock request is already pending with the admin team.";
                            ViewBag.RequestSubmitted = true;
                        }
                        else
                        {
                            statusMessage = "Your account is disabled. You can submit an unlock request below.";
                        }
                    }
                    else
                    {
                        statusMessage = "Your account is disabled. Please contact the administrator or submit a request below.";
                    }

                    ModelState.AddModelError(string.Empty, statusMessage);
                    return View(loginVM);
                }

                return InvalidLoginResponse(loginVM);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error");
                return View(loginVM);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestAccountUnlock(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return BadRequest();

            // Architect's Tip: Use TempData to pass feedback back to the Login view
            var result = await _userService.CreateUnlockRequestAsync(userId);

            if (result.Succeeded)
            {
                TempData["success"] = "Success! Your unlock request has been sent to the admin team.";
                ViewBag.RequestSubmitted = result.Succeeded;
            }
            else
            {
                TempData["error"] = result.Message; // e.g., "Request already pending"
            }

            return RedirectToAction(nameof(Login));
        }

        /// <summary>
        /// Architect's Approach: Encapsulated logic to find user by ITS, Email, Phone, or Username
        /// </summary>
        private async Task<IdentityUser> ResolveUserAsync(string identifier)
        {
            // Strategy: Check ITS Profile first as it's the most specific business requirement
            var profile = await _context.UserProfiles
                .AsNoTracking() // Performance optimization for read-only
                .FirstOrDefaultAsync(up => up.ITSNumber == identifier);

            if (profile != null)
            {
                return await _userManager.FindByIdAsync(profile.UserId);
            }

            // Fallback: Standard Identity Lookups
            return await _userManager.FindByNameAsync(identifier)
                   ?? await _userManager.FindByEmailAsync(identifier)
                   ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
        }

        /// <summary>
        /// Updates user online status with error resiliency
        /// </summary>
        private async Task UpdateUserStatusAsync(string userId)
        {
            try
            {
                var status = await _context.UserStatuses.FindAsync(userId)
                             ?? new UserStatusTracker { UserId = userId };

                status.IsOnline = true;
                status.LastActivity = DateTime.UtcNow;

                if (_context.Entry(status).State == EntityState.Detached)
                    _context.UserStatuses.Add(status);

                await _context.SaveChangesAsync();
            }
            catch
            {
                // Architect Note: Status tracking shouldn't break the login flow
            }
        }

        private IActionResult InvalidLoginResponse(LoginViewModel vm)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(vm);
        }
        public async Task<IActionResult> Logout()
        {
            string userId = null;

            // 1. Check if a user is currently logged in and get their ID before signing them out
            try
            {
                userId = _userManager.GetUserId(User);
            }
            catch (Exception idEx)
            {
                // Log error getting user ID, but continue to sign out
                // _logger.LogWarning(idEx, "Could not retrieve user ID during logout.");
            }

            // 2. Perform the sign-out operation
            try
            {
                await _sign_in_manager.SignOutAsync();
            }
            catch (Exception signOutEx)
            {
                // Log critical failure of the sign-out mechanism
                // _logger.LogCritical(signOutEx, "Critical error during SignOutAsync.");
                // The user session might still be active, but we continue to redirect.
            }

            if (userId != null)
            {
                // --- USER STATUS TRACKING: SET OFFLINE (Database Operations) ---
                try
                {
                    // 3. Find the status tracker using the captured ID
                    var status = await _context.UserStatuses.FindAsync(userId);

                    // 4. Update status to offline and save
                    if (status != null)
                    {
                        status.IsOnline = false;
                        status.LastActivity = DateTime.UtcNow;
                        // The critical database save operation
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception statusEx)
                {
                    // Log the error during status update but allow redirect to proceed
                    // _logger.LogError(statusEx, "Failed to update UserStatusTracker for user {UserId} during logout.", userId);
                }
            }

            return Redirect("/");
        }
        public async Task<IActionResult> OrderDetails(int id)
        {
            // The commented-out lines are ignored as they are not executed.

            try
            {
                // Fetch the specific Order
                Order order = await _context.Orders
                    .Where(x => x.Id == id)
                    .FirstOrDefaultAsync();

                // Check if the order was found before proceeding
                if (order == null)
                {
                    TempData["error"] = $"Order with ID {id} not found.";
                    return RedirectToAction("Index", "Orders"); // Assuming an Orders list view
                }

                // Fetch related Order Details
                List<OrderDetail> orderDetails = await _context.OrderDetails
                    .Where(x => x.OrderId == id)
                    .ToListAsync();

                // Return the combined ViewModel
                return View(new OrderDetailsViewModel { Order = order, OrderDetails = orderDetails });
            }
            catch (Exception ex)
            {
                // Log the detailed exception (e.g., database connection failure)
                // _logger.LogError(ex, "Error retrieving order details for Order ID: {OrderId}", id);

                // Inform the user about the failure
                TempData["error"] = "A database error occurred while trying to load the order details.";

                // Redirect to a safe page (e.g., the Orders list or Home)
                return RedirectToAction("Index", "Orders");
            }
        }

        public IActionResult AccessDenied()
        {
            try
            {
                var model = new AccessDeniedViewModel
                {
                    Message = "You do not have permission to access this page."
                };
                return View(model);
            }
            catch (Exception ex)
            {
                // Log the error if the view fails to render
                // _logger.LogError(ex, "Error rendering AccessDenied page.");

                // Fallback: return a generic 403 status code or a plain text response
                return StatusCode(403);
            }
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RequestSeller()
        {
            try
            {
                // Identity Operation
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login");

                // Database Operation: Check if a request already exists
                var isUserRequestExist = await _context.SubscriberRequests
                    .AnyAsync(r => r.UserId == currentUser.Id);

                if (!isUserRequestExist)
                {
                    // Database Operation: Add and Save New Request
                    try
                    {
                        var userProfile= await _context.UserProfiles
                            .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                        bool ITSAvailable= false;
                        if (userProfile != null) 
                        { 
                            ITSAvailable = !string.IsNullOrEmpty(userProfile.ITSNumber);
                        }

                        if(!ITSAvailable)
                        {
                            TempData["error"] = "You must have a valid ITS Number in your profile to request seller access. Please update your profile and try again.";
                            return RedirectToAction("ProfileDetails",new { ITSAvailable=ITSAvailable});
                        }
                        var newRequest = new SubscriberRequest
                        {
                            UserId = currentUser.Id,
                            RequestDate = System.DateTime.Now,
                            UserName = currentUser.UserName,
                            Approved = false
                        };
                        _context.Add(newRequest);
                        await _context.SaveChangesAsync();
                        TempData["success"] = "Your request was automatically created. Please check the status below.";
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                    {
                        // Handle concurrency or constraint violation if multiple requests tried to save simultaneously
                        // _logger.LogError(dbEx, "DbUpdateException when auto-creating SubscriberRequest for user {UserId}", currentUser.Id);
                        TempData["error"] = "A database issue prevented the creation of your request. Please try again.";
                        // Fallthrough to load existing request (which might now exist) or return null
                    }
                }

                // Database Operation: Retrieve the existing request (or the newly created one)
                var existingRequest = await _context.SubscriberRequests
                    .FirstOrDefaultAsync(r => r.UserId == currentUser.Id);

                // Pass the existing request (or null) to the view
                return View(existingRequest);
            }
            catch (Exception ex)
            {
                // Log the general error (e.g., database connection failure, Identity service error)
                // _logger.LogError(ex, "General error in GET RequestSeller for user {UserName}", User.Identity.Name);
                TempData["error"] = "An error occurred while loading the seller request page.";
                return RedirectToAction("Index", "Home"); // Redirect to a safe page
            }
        }

        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RequestSeller(string user = "")
        {
            try
            {
                // Identity Operation
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null) return RedirectToAction("Login");

                // Database Operation: Check for existing request to prevent duplicates
                var existingRequest = await _context.SubscriberRequests
                    .AsNoTracking()
                    .FirstOrDefaultAsync(r => r.UserId == currentUser.Id);

                if (existingRequest != null)
                {
                    TempData["info"] = "You have already submitted a request. Check the status page for details.";
                    return RedirectToAction("RequestStatus");
                }

                // Create and save the new request
                var newRequest = new SubscriberRequest
                {
                    UserId = currentUser.Id,
                    UserName = currentUser.UserName,
                    RequestedRole = "Subscriber",
                    Approved = false,
                    RequestDate = DateTime.Now
                };

                // Database Operations: Add and Save Changes
                _context.Add(newRequest);
                await _context.SaveChangesAsync();

                TempData["success"] = "Your seller request has been submitted successfully to the administrator for review.";
                return RedirectToAction("RequestStatus");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Log the detailed database error (e.g., constraint violation)
                // _logger.LogError(dbEx, "DbUpdateException when saving new SubscriberRequest for user {UserName}", User.Identity.Name);
                TempData["error"] = "A database error occurred while submitting your request. Please try again.";
                return RedirectToAction("RequestStatus");
            }
            catch (Exception ex)
            {
                // Log the general error (e.g., database connection failure, Identity service error)
                // _logger.LogError(ex, "General error in POST RequestSeller for user {UserName}", User.Identity.Name);
                TempData["error"] = "An unexpected error occurred while processing your request.";
                return RedirectToAction("RequestStatus");
            }
        }

        [Authorize]
        public async Task<IActionResult> RequestStatus()
        {
            try
            {
                // Identity Operation: Get the current authenticated user's details
                var current = await _userManager.GetUserAsync(User);
                if (current == null)
                {
                    // Identity lookup failed, possibly due to corrupted cookie or expired session
                    TempData["error"] = "Could not retrieve user details. Please log in again.";
                    return RedirectToAction("Login");
                }

                // Identity Operation: Get the current roles of the user
                // This is wrapped in the outer try-catch as well.
                var currentRoles = await _userManager.GetRolesAsync(current);

                // Select the first role or default to a safe string like "Guest"
                ViewBag.CurrentRole = currentRoles.FirstOrDefault() ?? "Guest";

                // Database Operation: Attempt to find an existing SubscriberRequest for this user
                var req = await _context.SubscriberRequests
                    .FirstOrDefaultAsync(r => r.UserId == current.Id);

                // If no request is found, create a placeholder model.
                if (req == null)
                {
                    var placeholderModel = new CMSECommerce.Models.SubscriberRequest
                    {
                        UserId = current.Id,
                        UserName = current.UserName,
                        // Approved = false is the default status when no request exists
                        Approved = false
                    };
                    // Pass the placeholder to the view. Note: The view name is "RequestStatus".
                    return View("RequestStatus", placeholderModel);
                }

                // If a request is found, pass the actual request details to the view.
                return View("RequestStatus", req);
            }
            catch (Exception ex)
            {
                // Log the detailed exception (e.g., database connection failure, Identity service error)
                // _logger.LogError(ex, "Error retrieving request status for user {UserName}", User.Identity.Name);

                // Inform the user about the failure
                TempData["error"] = "A system error occurred while trying to fetch your request status. Please try again later.";

                // Redirect to a safe page (e.g., the Home page)
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Subscriber")] // Restrict access to authorized users
        public async Task<IActionResult> UserList()
        {
            try
            {
                // 1. Get all IdentityUsers (Database/Identity Operation)
                var allIdentityUsers = await _userManager.Users.ToListAsync();

                // 2. Get all UserProfiles (Database Operation)
                var allUserProfiles = await _context.UserProfiles.ToListAsync();
                var profilesDictionary = allUserProfiles.ToDictionary(p => p.UserId, p => p);

                // 3. Create a list of the ProfileUpdateViewModel by combining IdentityUser and UserProfile
                // Includes asynchronous calls to GetRolesAsync
                var userListTasks = allIdentityUsers.Select(async user =>
                {
                    // The dictionary lookup itself is safe and fast
                    profilesDictionary.TryGetValue(user.Id, out var profile);

                    // Critical Identity Operation: GetRolesAsync
                    var roles = await _userManager.GetRolesAsync(user);

                    // Accessing profile properties (using null conditional operator `?` for safety)
                    var viewModel = new ProfileUpdateViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        FirstName = profile?.FirstName,
                        LastName = profile?.LastName,
                        ITSNumber = profile?.ITSNumber,
                        Profession = profile?.Profession,
                        // Handle potential null profile before accessing IsProfileVisible
                        IsProfileVisible = profile?.IsProfileVisible ?? false,
                        // Assign the user's highest role, or "None"
                        CurrentRole = roles.FirstOrDefault() ?? "None"
                    };

                    return viewModel;
                }).ToList();

                // Critical Task Management: Wait for all the role lookups to complete
                var finalUserList = await Task.WhenAll(userListTasks);

                // 4. Implementation of the Filter: Exclude users who are not visible or are Admins (for non-Admin viewers).
                List<ProfileUpdateViewModel> filteredUserList = new();
                if (User.IsInRole("Admin"))
                {
                    // Admins see all visible profiles
                    filteredUserList = finalUserList
                        .Where(u => u.IsProfileVisible)
                        .OrderBy(u => u.UserName)
                        .ToList();
                }
                else
                {
                    // Subscribers see visible profiles that are NOT Admins
                    filteredUserList = finalUserList
                        .Where(u => u.CurrentRole != "Admin" && u.IsProfileVisible)
                        .OrderBy(u => u.UserName)
                        .ToList();
                }

                return View(filteredUserList);
            }
            catch (Exception ex)
            {
                // Log the detailed exception (e.g., database connection failure, Identity service error)
                // _logger.LogError(ex, "Error occurred while generating the UserList.");

                // Inform the user about the failure
                TempData["error"] = "A system error occurred while trying to load the list of users.";

                // Redirect to a safe page or return an empty list
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Subscriber")] // Restrict access to authorized users
        public async Task<IActionResult> UserDetails(string id)
        {
            // 1. Determine the user ID to view (current user or specified user)
            if (string.IsNullOrEmpty(id))
            {
                id = (await _userManager.GetUserAsync(User))?.Id;
            }

            if (string.IsNullOrEmpty(id))
            {
                // Should not happen under [Authorize], but a safeguard
                TempData["error"] = "User ID could not be determined. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 2. Fetch Identity User details
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return NotFound();
                }

                // 3. Fetch related data (UserProfile and Roles)
                var userProfile = await _context.UserProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == id);
                var roles = await _userManager.GetRolesAsync(user);

                // 4. Map data to the ViewModel (EditUserModel is used here as it contains all necessary properties)
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

                    // Status Flags
                    IsImageApproved = userProfile?.IsImageApproved ?? false,
                    IsProfileVisible = userProfile?.IsProfileVisible ?? true,

                    // Details
                    About = userProfile?.About,
                    Profession = userProfile?.Profession,
                    ServicesProvided = userProfile?.ServicesProvided,

                    // Social Links
                    LinkedInUrl = userProfile?.LinkedInUrl,
                    FacebookUrl = userProfile?.FacebookUrl,
                    InstagramUrl = userProfile?.InstagramUrl,
                    WhatsAppNumber = userProfile?.WhatsAppNumber,

                    // Addresses
                    HomeAddress = userProfile?.HomeAddress,
                    HomePhoneNumber = userProfile?.HomePhoneNumber,
                    BusinessAddress = userProfile?.BusinessAddress,
                    BusinessPhoneNumber = userProfile?.BusinessPhoneNumber,

                    // QRCodes
                    GpayQRCodePath = userProfile?.GpayQRCodePath,
                    PhonePeQRCodePath = userProfile?.PhonePeQRCodePath
                };

                // 5. Return the model to the ProfileDetails.cshtml view (read-only)
                return View("ProfileDetailsView",model);
            }
            catch (Exception ex)
            {
                // Log the error
                _logger.LogError(ex, "Error loading user ID: {UserId} details.", id);
                TempData["error"] = "An error occurred while loading the profile details.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")] // ⬅️ IMPORTANT: Only Admins should delete other users
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Invalid user ID provided for deletion.";
                return RedirectToAction("UserList");
            }

            try
            {
                // 1. Find the IdentityUser (Identity Operation)
                var userToDelete = await _userManager.FindByIdAsync(userId);

                if (userToDelete == null)
                {
                    TempData["info"] = $"User with ID {userId} not found.";
                    return RedirectToAction("UserList");
                }

                // IMPORTANT: Prevent accidental deletion of the current Admin user's account
                if (userToDelete.Id == _userManager.GetUserId(User))
                {
                    TempData["error"] = "You cannot delete your own account via the Admin panel.";
                    return RedirectToAction("UserList");
                }

                // --- 2. Delete related custom data (UserProfile, Address, Orders, etc.) ---

                // Helper function for file path resolution
                string GetFullPath(string relativePath)
                {
                    if (string.IsNullOrEmpty(relativePath)) return null;
                    return Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/', '\\'));
                }

                // a. Find and remove UserProfile (Database Operation)
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile != null)
                {
                    // Delete Profile Image (File System Operation)
                    try
                    {
                        string profileImagePath = GetFullPath(userProfile.ProfileImagePath);
                        if (System.IO.File.Exists(profileImagePath))
                        {
                            System.IO.File.Delete(profileImagePath);
                        }
                    }
                    catch (Exception fileEx)
                    {
                        // Log file deletion error but continue with database cleanup
                        // _logger.LogWarning(fileEx, "Failed to delete profile image for user {UserId}", userId);
                    }

                    _context.UserProfiles.Remove(userProfile);
                }

                // b. Delete Address records (Database Operation)
                var userAddresses = await _context.Addresses
                    .Where(a => a.UserId == userId)
                    .ToListAsync();
                _context.Addresses.RemoveRange(userAddresses);

                // c. Delete Orders (Database Operation)
                var userOrders = await _context.Orders
                    .Where(o => o.UserName == userToDelete.UserName)
                    .ToListAsync();
                _context.Orders.RemoveRange(userOrders);

                // d. Save all custom context changes before deleting the IdentityUser (Database Operation)
                await _context.SaveChangesAsync();

                // --- 3. Delete the IdentityUser (Identity Operation) ---
                IdentityResult result = await _userManager.DeleteAsync(userToDelete);

                if (result.Succeeded)
                {
                    TempData["success"] = $"User '{userToDelete.UserName}' and all associated data deleted successfully.";
                }
                else
                {
                    // Identity operation failed (e.g., due to configuration issue)
                    string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["error"] = $"Error deleting user: {errors}";
                }

                // Redirect to the list of users
                return RedirectToAction("UserList");
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            {
                // Log the specific database update error
                // _logger.LogError(dbEx, "Database concurrency or foreign key error during user data cleanup for ID: {UserId}", userId);
                TempData["error"] = "Database error occurred while cleaning up user data. Deletion failed.";
                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                // Log the general unexpected error (e.g., connection issue, general Identity failure)
                // _logger.LogCritical(ex, "Critical unexpected error during user deletion for ID: {UserId}", userId);
                TempData["error"] = "A critical system error occurred during the deletion process.";
                return RedirectToAction("UserList");
            }
        }



    }
}