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


        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Best practice: Use the unique User ID instead of Name for database queries
            var userId = _userManager.GetUserId(User);
            List<CMSECommerce.Models.Order> orders = new();

            try
            {
                // 1. Update statuses before displaying the list
                await UpdateOrderShippedStatus(userId);

                // 2. Fetch the updated list using UserId
                orders = await _context.Orders
    .Include(o => o.OrderDetails) // Add this line!
    .Where(x => x.UserId == userId)
    .OrderByDescending(x => x.Id)
    .ToListAsync();
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error retrieving orders for user: {userId}", userId);
                TempData["error"] = "An error occurred while loading your orders.";
                return View(orders);
            }

            return View(orders);
        }

        private async Task UpdateOrderShippedStatus(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                // Optimized Query: Find orders belonging to the user where:
                // 1. The order is not yet marked as Shipped
                // 2. ALL associated OrderDetails have IsProcessed set to true
                var ordersToUpdate = await _context.Orders
                    .Where(o => o.UserId == userId && !o.Shipped)
                    .Where(o => o.OrderDetails.All(d => d.IsProcessed))
                    .ToListAsync();

                if (ordersToUpdate.Any())
                {
                    foreach (var order in ordersToUpdate)
                    {
                        order.Shipped = true;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Failed to auto-update order status for {userId}", userId);
            }
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
            // 1. Basic Validations
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
                    // Use a Database Transaction to ensure both Store and Profile are created together
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        // 3. Create the Store FIRST
                        var newStore = new Store
                        {
                            StoreName = $"{user.FirstName}'s Shop", // Fallback name
                            StreetAddress = user.StreetAddress,
                            City = user.City,
                            PostCode = user.PostalCode,
                            Country = user.Country,
                            Email = user.Email,
                            Contact = user.PhoneNumber,
                            GSTIN = string.Empty // Ensure your User model has this field
                        };

                        _context.Stores.Add(newStore);
                        await _context.SaveChangesAsync(); // This generates the newStore.Id

                        // 4. Create the UserProfile using the newStore.Id
                        string fullAddress = $"{user.StreetAddress}, {user.City}, {user.State} {user.PostalCode}, {user.Country}";

                        var newProfile = new UserProfile
                        {
                            UserId = newUser.Id,
                            FirstName = user.FirstName,
                            LastName = user.LastName,
                            HomeAddress = fullAddress,
                            HomePhoneNumber = user.PhoneNumber,
                            BusinessPhoneNumber = user.PhoneNumber,

                            // Link to the Store we just created
                            StoreId = newStore.Id,

                            IsProfileVisible = true,
                            IsImageApproved = false,
                            IsImagePending = false
                        };

                        _context.UserProfiles.Add(newProfile);

                        // 5. Create Address entry
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

                        // Save Profile and Address
                        await _context.SaveChangesAsync();

                        // Commit the transaction
                        await transaction.CommitAsync();

                        // 6. Assign Role and Finalize
                        await _userManager.AddToRoleAsync(newUser, "Seller"); // Changed to 'Seller' since they have a store

                        TempData["success"] = "Registration and Store setup successful!";
                        return RedirectToAction("Login");
                    }
                    catch (Exception dbEx)
                    {
                        await transaction.RollbackAsync();
                        await _userManager.DeleteAsync(newUser); // Clean up the Identity user
                        ModelState.AddModelError("", "Error creating store or profile. Please check your data.");
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
                ModelState.AddModelError("", "A critical error occurred.");
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
                // 1. ADMIN AUTHORIZATION CHECK
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null || !await _userManager.IsInRoleAsync(currentUser, "Admin"))
                {
                    TempData["error"] = "You are not authorized to edit other user profiles.";
                    return RedirectToAction("Profile");
                }

                var identityUser = await _userManager.FindByIdAsync(id);

                if (identityUser == null)
                {
                    TempData["error"] = "User not found.";
                    return RedirectToAction("ProfileDetails");
                }

                // 2. RETRIEVE DATA (Including Store information)
                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store)
                    .FirstOrDefaultAsync(p => p.UserId == id);

                // Map base Identity data to the ViewModel
                var viewModel = new ProfileUpdateViewModel
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    PhoneNumber = identityUser.PhoneNumber
                };

                if (userProfile != null)
                {
                    // --- PROFILE BASIC FIELDS ---
                    viewModel.FirstName = userProfile.FirstName;
                    viewModel.LastName = userProfile.LastName;
                    viewModel.ITSNumber = userProfile.ITSNumber;
                    viewModel.Profession = userProfile.Profession;
                    viewModel.About = userProfile.About;
                    viewModel.ServicesProvided = userProfile.ServicesProvided;
                    viewModel.IsProfileVisible = userProfile.IsProfileVisible;

                    // --- SOCIAL & CONTACT ---
                    viewModel.LinkedInUrl = userProfile.LinkedInUrl;
                    viewModel.FacebookUrl = userProfile.FacebookUrl;
                    viewModel.InstagramUrl = userProfile.InstagramUrl;
                    viewModel.WhatsappNumber = userProfile.WhatsAppNumber;

                    // --- USER ADDRESSES ---
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;

                    // --- IMAGES & STATUS ---
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.IsImageApproved = userProfile.IsImageApproved;
                    viewModel.PendingProfileImagePath = userProfile.PendingProfileImagePath;
                    viewModel.IsImagePending = userProfile.IsImagePending;

                    // --- PAYMENT QR CODES ---
                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;

                    // --- NEW: STORE FIELDS (All Fields Mapped) ---
                    if (userProfile.Store != null)
                    {
                        viewModel.StoreId = userProfile.Store.Id; // Hidden field in view
                        viewModel.StoreName = userProfile.Store.StoreName;
                        viewModel.StoreStreetAddress = userProfile.Store.StreetAddress;
                        viewModel.StoreCity = userProfile.Store.City;
                        viewModel.StorePostCode = userProfile.Store.PostCode;
                        viewModel.StoreCountry = userProfile.Store.Country;
                        viewModel.GSTIN = userProfile.Store.GSTIN;
                        viewModel.StoreEmail = userProfile.Store.Email;
                        viewModel.StoreContact = userProfile.Store.Contact;
                    }
                }

                return View("Profile", viewModel);
            }
            catch (Exception ex)
            {
                TempData["error"] = "An error occurred while preparing the profile.";
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

                // ** UPDATED: Fetch UserProfile with Store details included **
                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store) // Join with Stores table
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == id);

                var roles = await _userManager.GetRolesAsync(user);

                // ** UPDATED: Map all Identity, UserProfile, and Store fields **
                var model = new EditUserModel
                {
                    // IdentityUser Fields
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault(),

                    // UserProfile Fields
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
                    PhonePeQRCodePath = userProfile?.PhonePeQRCodePath,

                    // ** NEW: Store Fields Mapping **
                    StoreId = userProfile?.Store?.Id,
                    StoreName = userProfile?.Store?.StoreName,
                    StoreStreetAddress = userProfile?.Store?.StreetAddress,
                    StoreCity = userProfile?.Store?.City,
                    StorePostCode = userProfile?.Store?.PostCode,
                    StoreCountry = userProfile?.Store?.Country,
                    GSTIN = userProfile?.Store?.GSTIN,
                    StoreEmail = userProfile?.Store?.Email,
                    StoreContact = userProfile?.Store?.Contact
                };

                ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList();

                // Pass the ITSAvailable flag to ViewBag if needed for view logic
                ViewBag.ITSAvailable = ITSAvailable;

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user ID: {UserId} for details.", id);
                TempData["error"] = "Failed to load user details.";
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(ProfileUpdateViewModel viewModel)
        {
            // 1. Model Validation
            if (!ModelState.IsValid)
            {
                ViewBag.ITSAvailable = !string.IsNullOrEmpty(viewModel.ITSNumber);
                return View("ProfileDetails", viewModel);
            }

            try
            {
                // 2. Retrieve Existing Data
                var identityUser = await _userManager.FindByIdAsync(viewModel.UserId);

                if (identityUser == null)
                {
                    TempData["error"] = "Error: User not found.";
                    return RedirectToAction("Login", "Account");
                }

                // Retrieve the UserProfile including the Store data
                var userProfile = await _context.UserProfiles
                                                .Include(p => p.Store)
                                                .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                // If UserProfile doesn't exist, create a new one
                if (userProfile == null)
                {
                    userProfile = new UserProfile { UserId = identityUser.Id };
                    _context.UserProfiles.Add(userProfile);
                }

                // --- NEW: Store Management Logic ---
                // If the user is currently not linked to a store (happens after migration), 
                // we link them to a default store or create one for them.
                if (userProfile.StoreId == null)
                {
                    var defaultStore = await _context.Stores.FirstOrDefaultAsync();
                    if (defaultStore != null)
                    {
                        userProfile.StoreId = defaultStore.Id;
                    }
                    else
                    {
                        // Create a placeholder store if none exists to avoid FK errors
                        var placeholderStore = new Store
                        {
                            StoreName = $"{viewModel.FirstName}'s Store",
                            StreetAddress = viewModel.HomeAddress ?? "Pending",
                            City = "Pending",
                            PostCode = "0000",
                            Country = "Pending",
                            Email = identityUser.Email,
                            Contact = viewModel.PhoneNumber
                        };
                        _context.Stores.Add(placeholderStore);
                        await _context.SaveChangesAsync(); // Generate ID
                        userProfile.StoreId = placeholderStore.Id;
                    }
                }

                // 3. Handle File Uploads (Profile Image)
                if (viewModel.ProfileImageUpload != null)
                {
                    string newImagePath = await SaveFile(viewModel.ProfileImageUpload, "profileimages/pending");
                    userProfile.PendingProfileImagePath = newImagePath;
                    userProfile.IsImagePending = true;
                    userProfile.IsImageApproved = false;

                    TempData["message"] = "Profile image uploaded successfully and is awaiting administrator approval.";
                }

                // Handle QR Code Uploads
                if (viewModel.GpayQRCodeUpload != null)
                {
                    userProfile.GpayQRCodePath = await SaveFile(viewModel.GpayQRCodeUpload, "qrcodes/gpay");
                }

                if (viewModel.PhonePeQRCodeUpload != null)
                {
                    userProfile.PhonePeQRCodePath = await SaveFile(viewModel.PhonePeQRCodeUpload, "qrcodes/phonepe");
                }

                // 4. Update IdentityUser Data
                if (identityUser.PhoneNumber != viewModel.PhoneNumber)
                {
                    identityUser.PhoneNumber = viewModel.PhoneNumber;
                    var result = await _userManager.UpdateAsync(identityUser);
                    if (!result.Succeeded)
                    {
                        TempData["error"] = "Error updating phone number.";
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

                // 6. Save Changes
                await _context.SaveChangesAsync();

                TempData["success"] = TempData["message"] != null ?
                                      TempData["message"].ToString() :
                                      "Profile details updated successfully!";

                return RedirectToAction("ProfileDetails", new { userId = viewModel.UserId });
            }
            catch (Exception ex)
            {
                TempData["error"] = "An unexpected error occurred. Please try again.";
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
            // and using statements (System.IO, Microsoft.EntityFrameworkCore, etc.) are available.

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Find Identity User
            IdentityUser identityUser;
            try
            {
                identityUser = await _userManager.FindByIdAsync(model.UserId);
            }
            catch (Exception ex)
            {
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

            bool profileImageUploaded = false;

            try
            {
                // Internal Helper for File Uploads
                async Task<string> ProcessFileUpload(IFormFile file, string subFolder)
                {
                    if (file == null) return null;
                    string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", subFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);
                    }
                    return Path.Combine("images", subFolder, uniqueFileName).Replace("\\", "/");
                }

                // Process Profile Image for Approval
                if (model.ProfileImageUpload != null)
                {
                    string newPendingPath = await ProcessFileUpload(model.ProfileImageUpload, "profiles/pending");

                    if (!string.IsNullOrEmpty(userProfile.PendingProfileImagePath))
                    {
                        try
                        {
                            string oldPendingPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.PendingProfileImagePath);
                            if (System.IO.File.Exists(oldPendingPath)) System.IO.File.Delete(oldPendingPath);
                        }
                        catch { /* Log non-critical delete failure */ }
                    }

                    userProfile.PendingProfileImagePath = newPendingPath;
                    userProfile.IsImageApproved = false;
                    userProfile.IsImagePending = true;
                    profileImageUploaded = true;
                }

                // Process QR Codes
                if (model.GpayQRCodeUpload != null)
                    userProfile.GpayQRCodePath = await ProcessFileUpload(model.GpayQRCodeUpload, "qrcodes");

                if (model.PhonePeQRCodeUpload != null)
                    userProfile.PhonePeQRCodePath = await ProcessFileUpload(model.PhonePeQRCodeUpload, "qrcodes");
            }
            catch (IOException)
            {
                ModelState.AddModelError("", "Error saving file attachments. Check server disk space.");
                return View(model);
            }

            // --- 3. Update UserProfile & Store Details ---
            // Personal Info
            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;
            userProfile.ITSNumber = model.ITSNumber;
            userProfile.About = model.About;
            userProfile.Profession = model.Profession;
            userProfile.ServicesProvided = model.ServicesProvided;
            userProfile.IsProfileVisible = model.IsProfileVisible;

            // Contact & Social
            userProfile.LinkedInUrl = model.LinkedInUrl;
            userProfile.FacebookUrl = model.FacebookUrl;
            userProfile.InstagramUrl = model.InstagramUrl;
            userProfile.WhatsAppNumber = model.WhatsappNumber;
            userProfile.HomeAddress = model.HomeAddress;
            userProfile.HomePhoneNumber = model.HomePhoneNumber;

            // NEW: Store & Business Mapping
            // This assumes your UserProfile model now contains these specific Store fields
            userProfile.Store.StoreName = model.StoreName;
            userProfile.Store.GSTIN = model.GSTIN;
            userProfile.Store.Email = model.StoreEmail;
            userProfile.Store.Contact = model.StoreContact;
            userProfile.Store.StreetAddress = model.StoreStreetAddress;
            userProfile.Store.City = model.StoreCity;
            userProfile.Store.PostCode = model.StorePostCode;
            userProfile.Store.Country = model.StoreCountry;

            // Legacy Support: If your system still uses 'BusinessAddress', sync it with Street Address
            userProfile.BusinessAddress = model.StoreStreetAddress;

            try
            {
                if (isNewProfile) _context.UserProfiles.Add(userProfile);
                else _context.UserProfiles.Update(userProfile);

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Error saving profile details due to a database constraint.");
                return View(model);
            }

            // Success Handling
            TempData["success"] = profileImageUploaded
                ? "Profile updated. Your new image is pending administrator review."
                : "Profile details updated successfully.";

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
                    // Helper function for file deletion
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
                        catch (Exception)
                        {
                            // Log warning but don't break the flow
                        }
                    }

                    // --- 1. Delete associated files ---
                    // Existing approved image
                    DeleteFileIfPresent(userProfile.ProfileImagePath);

                    // NEW: Ensure pending profile images are also wiped
                    DeleteFileIfPresent(userProfile.PendingProfileImagePath);

                    // QR Codes
                    DeleteFileIfPresent(userProfile.GpayQRCodePath);
                    DeleteFileIfPresent(userProfile.PhonePeQRCodePath);

                    // --- 2. Remove Profile Record ---
                    // This automatically wipes StoreName, GSTIN, StoreCity, etc.
                    _context.UserProfiles.Remove(userProfile);
                    await _context.SaveChangesAsync();

                    TempData["success"] = "Your business and profile data has been completely removed!";
                }
                else
                {
                    TempData["info"] = "No custom profile data found to delete.";
                }

                return RedirectToAction("ProfileDetails", new { userId = identityUser.Id });
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error deleting profile for user {UserId}", identityUser.Id);
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
            try
            {
                // 1. Fetch Order with its Details in a single query using Include
                // This is more efficient than two separate database calls
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(x => x.Id == id);

                // 2. Safety Check
                if (order == null)
                {
                    TempData["error"] = $"Order with ID {id} not found.";
                    return RedirectToAction("Index", "Orders");
                }

                // 3. Fetch the UserProfile associated with the ORDER owner
                // We use order.UserId to ensure we see the correct profile even if an Admin is viewing
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == order.UserId);

                // 4. If no profile exists yet, initialize a blank one to prevent NullReference in the View
                if (userProfile == null)
                {
                    userProfile = new UserProfile { UserId = order.UserId };
                }

                // 5. Return the combined ViewModel
                var viewModel = new OrderDetailsViewModel
                {
                    Order = order,
                    OrderDetails = order.OrderDetails.ToList(),
                    UserProfile = userProfile
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error retrieving order details for Order ID: {OrderId}", id);
                TempData["error"] = "A database error occurred while trying to load the order details.";
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
        [Authorize(Roles = "Admin,Subscriber")]
        public async Task<IActionResult> UserList()
        {
            try
            {
                // 1. Fetch all Identity Users
                var allIdentityUsers = await _userManager.Users.ToListAsync();

                // 2. Fetch all Profiles with Stores (One database hit)
                var allUserProfiles = await _context.UserProfiles
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .ToListAsync();

                // 3. Fetch ALL User-Role mappings at once (Avoids the threading error)
                // This links UserIds to RoleNames in a flat list
                var userRoles = await (from ur in _context.UserRoles
                                       join r in _context.Roles on ur.RoleId equals r.Id
                                       select new { ur.UserId, r.Name }).ToListAsync();

                // Map for fast lookup
                var profilesDict = allUserProfiles.ToDictionary(p => p.UserId, p => p);
                var rolesDict = userRoles.GroupBy(ur => ur.UserId)
                                         .ToDictionary(g => g.Key, g => g.First().Name);

                // 4. Map to ViewModel in memory (No async calls inside this loop)
                var finalUserList = allIdentityUsers.Select(user =>
                {
                    profilesDict.TryGetValue(user.Id, out var profile);
                    rolesDict.TryGetValue(user.Id, out var roleName);

                    return new ProfileUpdateViewModel
                    {
                        UserId = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        FirstName = profile?.FirstName,
                        LastName = profile?.LastName,
                        ITSNumber = profile?.ITSNumber,
                        Profession = profile?.Profession,
                        IsProfileVisible = profile?.IsProfileVisible ?? false,
                        CurrentRole = roleName ?? "None",

                        // Safe navigation for Store
                        StoreName = profile?.Store?.StoreName ?? "Independent",
                        StoreCity = profile?.Store?.City,
                        StoreContact = profile?.Store?.Contact,
                        StoreEmail = profile?.Store?.Email,
                        ExistingProfileImagePath = profile?.ProfileImagePath
                    };
                }).ToList();

                // 5. Apply Filtering
                IEnumerable<ProfileUpdateViewModel> filteredList;
                if (User.IsInRole("Admin"))
                {
                    filteredList = finalUserList.Where(u => u.IsProfileVisible);
                }
                else
                {
                    filteredList = finalUserList.Where(u => u.IsProfileVisible && u.CurrentRole != "Admin");
                }

                var result = filteredList
                    .OrderBy(u => u.StoreName == "Independent")
                    .ThenBy(u => u.StoreName)
                    .ToList();

                return View(result);
            }
            catch (Exception ex)
            {
                TempData["error"] = "Unable to load the business directory.";
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
        [Authorize(Roles = "Admin")]
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
                var userToDelete = await _userManager.FindByIdAsync(userId);
                if (userToDelete == null)
                {
                    TempData["info"] = $"User with ID {userId} not found.";
                    return RedirectToAction("UserList");
                }

                if (userToDelete.Id == _userManager.GetUserId(User))
                {
                    TempData["error"] = "You cannot delete your own account via the Admin panel.";
                    return RedirectToAction("UserList");
                }

                // Helper function for file deletion
                void DeleteFile(string relativePath)
                {
                    if (string.IsNullOrEmpty(relativePath)) return;
                    try
                    {
                        var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/', '\\'));
                        if (System.IO.File.Exists(fullPath))
                        {
                            System.IO.File.Delete(fullPath);
                        }
                    }
                    catch { /* Log warning if needed */ }
                }

                // 1. Remove User Profile & Associated Files
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile != null)
                {
                    // Delete all associated business/profile files
                    DeleteFile(userProfile.ProfileImagePath);
                    DeleteFile(userProfile.PendingProfileImagePath);
                    DeleteFile(userProfile.GpayQRCodePath);
                    DeleteFile(userProfile.PhonePeQRCodePath);

                    _context.UserProfiles.Remove(userProfile);
                }

                // 2. Remove Addresses
                var userAddresses = await _context.Addresses.Where(a => a.UserId == userId).ToListAsync();
                _context.Addresses.RemoveRange(userAddresses);

                // 3. Remove Orders (Linked via UserId for better accuracy)
                var userOrders = await _context.Orders.Where(o => o.UserId == userId).ToListAsync();
                _context.Orders.RemoveRange(userOrders);

                // 4. Commit changes to Custom Tables
                await _context.SaveChangesAsync();

                // 5. Delete the Identity User
                IdentityResult result = await _userManager.DeleteAsync(userToDelete);

                if (result.Succeeded)
                {
                    TempData["success"] = $"User '{userToDelete.UserName}' and all associated business data deleted.";
                }
                else
                {
                    string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    TempData["error"] = $"Error deleting identity account: {errors}";
                }

                return RedirectToAction("UserList");
            }
            catch (Exception ex)
            {
                TempData["error"] = "A critical error occurred while deleting user data.";
                return RedirectToAction("UserList");
            }
        }

        public async Task<IActionResult> Invoice(int id)
        {
            // 1. Fetch the Order
            // Using AsNoTracking() as this is a read-only view for an invoice
            var order = await _context.Orders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // 2. Fetch Order Details (the items inside the order)
            var orderDetails = await _context.OrderDetails
                .AsNoTracking()
                .Where(od => od.OrderId == id)
                .ToListAsync();

            // 3. Fetch UserProfile AND the related Store
            // IMPORTANT: .Include(u => u.Store) is required to access store details in the view
            var userProfile = await _context.UserProfiles
                .AsNoTracking()
                .Include(u => u.User)  // Standard Identity User data
                .Include(u => u.Store) // The new Store model data
                .FirstOrDefaultAsync(u => u.UserId == order.UserId);

            // 4. Populate the ViewModel
            var viewModel = new OrderDetailsViewModel
            {
                Order = order,
                OrderDetails = orderDetails,
                UserProfile = userProfile
            };

            return View(viewModel);
        }

    }
}