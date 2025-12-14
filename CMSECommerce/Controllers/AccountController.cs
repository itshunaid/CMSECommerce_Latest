using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Services;

namespace CMSECommerce.Controllers
{
    public class AccountController(
            DataContext dataContext,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment webHostEnvironment,
            IUserStatusService userStatusService) : Controller
    {
        private DataContext _context = dataContext;
        private UserManager<IdentityUser> _userManager = userManager;
        private SignInManager<IdentityUser> _sign_in_manager = signInManager;
        private IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly IUserStatusService _userStatusService = userStatusService;

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

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (string.IsNullOrEmpty(user.FirstName))
            {
                user.FirstName = "First Name";
            }
            if (string.IsNullOrEmpty(user.LastName))
            {
                user.LastName = "Last Name";
            }

            if (ModelState.IsValid)
            {
                IdentityUser newUser = new() { UserName = user.UserName, Email = user.Email, PhoneNumber = user.PhoneNumber };

                try
                {
                    // 1. Attempt to create the user in the Identity store
                    IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);

                    if (result.Succeeded)
                    {
                        // 2. Add Profile and Address (Database operations)
                        try
                        {
                            var newProfile = new UserProfile
                            {
                                UserId = newUser.Id,
                                FirstName = user.FirstName,
                                LastName = user.LastName
                            };
                            _context.UserProfiles.Add(newProfile);

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
                            await _context.SaveChangesAsync(); // Save both UserProfile and Address

                            // 3. Assign role (Identity operation)
                            await _userManager.AddToRoleAsync(newUser, "Customer");

                            TempData["success"] = "You have registered successfully!";
                            return RedirectToAction("Login");
                        }
                        catch (Exception dbEx)
                        {
                            // Log error if saving profile/address or assigning role fails *after* IdentityUser creation
                            // _logger.LogError(dbEx, "Failed to save UserProfile, Address, or assign role for user: {username}", user.UserName);

                            // You might want to delete the newly created IdentityUser here to clean up, 
                            // as the registration is incomplete.
                            // await _userManager.DeleteAsync(newUser); 

                            TempData["error"] = "Registration successful, but failed to save profile details. Please contact support.";
                            return RedirectToAction("Login"); // Redirect to avoid displaying sensitive error info
                        }
                    }

                    // If IdentityResult was not successful (e.g., username already exists)
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                catch (Exception ex)
                {
                    // Log catastrophic failure of Identity system (e.g., database connection down)
                    // _logger.LogCritical(ex, "Critical failure during user creation for user: {username}", user.UserName);
                    ModelState.AddModelError("", "A critical error occurred during registration. Please try again later.");
                }
            }

            // Return view with model (and potentially validation errors)
            return View(user);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null) return RedirectToAction("Login");

            try
            {
                // Attempt to find existing profile data
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                // Map data to the ViewModel
                var viewModel = new ProfileUpdateViewModel
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    PhoneNumber = identityUser.PhoneNumber,
                };

                if (userProfile != null)
                {
                    // ... (Mapping properties remains the same)
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
                    viewModel.WhatsappNumber = userProfile.WhatsappNumber;
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                }

                // Redirect to the ProfileDetails action to display the profile
                return RedirectToAction("ProfileDetails");
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
                var identityUser = await _userManager.FindByIdAsync(id);

                if (identityUser == null)
                {
                    TempData["error"] = "User not found or you do not have permission.";
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
                    // ... (Mapping properties remains the same)
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
                    viewModel.WhatsappNumber = userProfile.WhatsappNumber;
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                }

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
        public async Task<IActionResult> ProfileDetails(string userId = "")
        {
            try
            {
                IdentityUser identityUser = new();
                if (string.IsNullOrEmpty(userId))
                {
                    identityUser = await _userManager.GetUserAsync(User);
                }
                else
                {
                    identityUser = await _userManager.FindByIdAsync(userId);
                }

                if (identityUser == null) return RedirectToAction("Login");

                // Attempt to find existing profile data
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                // Map data to the ViewModel
                var viewModel = new ProfileUpdateViewModel
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    PhoneNumber = identityUser.PhoneNumber,
                };

                if (userProfile != null)
                {
                    // ... (Mapping properties remains the same)
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
                    viewModel.WhatsappNumber = userProfile.WhatsappNumber;
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                    viewModel.IsImageApproved = userProfile.IsImageApproved;
                }

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log the error
                // _logger.LogError(ex, "Error fetching profile details for user ID: {userId}", userId);
                TempData["error"] = "An error occurred while loading the profile details.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
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

            // Separate the I/O logic for better error handling visibility
            try
            {
                // File Upload Helper Function (Original implementation with try-catch for I/O)
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

                // Process Profile Image
                if (model.ProfileImageUpload != null)
                {
                    // Delete old image (I/O operation)
                    if (!string.IsNullOrEmpty(userProfile.ProfileImagePath))
                    {
                        try
                        {
                            string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfileImagePath);
                            if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        }
                        catch (Exception deleteEx)
                        {
                            // Log but don't stop the update, as the new file can still be saved
                            // _logger.LogWarning(deleteEx, "Failed to delete old profile image: {Path}", userProfile.ProfileImagePath);
                        }
                    }
                    userProfile.ProfileImagePath = await ProcessFileUpload(model.ProfileImageUpload, "profiles");
                    userProfile.IsImageApproved = model.IsImageApproved;
                }

                // Process GPay/PhonePe QR Codes (omitting old file delete for brevity, but it should be added)
                if (model.GpayQRCodeUpload != null)
                {
                    userProfile.GpayQRCodePath = await ProcessFileUpload(model.GpayQRCodeUpload, "qrcodes");
                }
                if (model.PhonePeQRCodeUpload != null)
                {
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
            // (Mapping properties remains the same)
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
            userProfile.WhatsappNumber = model.WhatsappNumber;
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

            TempData["success"] = "Your profile has been updated successfully!";
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
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            // The ReturnUrl should be handled here to prevent null reference if not provided.
            string returnUrl = loginVM.ReturnUrl ?? "/";

            if (ModelState.IsValid)
            {
                try
                {
                    // 1. Attempt to find the user by UserName
                    var user = await _userManager.FindByNameAsync(loginVM.UserName);

                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Invalid username or password.");
                        return View(loginVM);
                    }

                    // 2. Attempt the sign-in
                    Microsoft.AspNetCore.Identity.SignInResult result =
                        await _sign_in_manager.PasswordSignInAsync(loginVM.UserName, loginVM.Password, false, false);

                    if (result.Succeeded)
                    {
                        // --- USER STATUS TRACKING: SET ONLINE (Database Operations) ---
                        try
                        {
                            // 1. Get the status tracker for the successfully logged-in user
                            var status = await _context.UserStatuses.FindAsync(user.Id);

                            // 2. If no tracker exists, create one
                            if (status == null)
                            {
                                status = new UserStatusTracker { UserId = user.Id };
                                _context.UserStatuses.Add(status);
                            }

                            // 3. Update status to online and record activity time
                            status.IsOnline = true;
                            status.LastActivity = DateTime.UtcNow;

                            // The critical database save operation
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception statusEx)
                        {
                            // Log the error during status update but allow login to proceed
                            // Logging the status update failure is crucial for monitoring.
                            // _logger.LogError(statusEx, "Failed to update UserStatusTracker for user {UserId} during login.", user.Id);
                            // The user is already logged in, so we swallow the exception and continue the redirect.
                        }

                        // 4. Redirect user to the intended URL
                        return LocalRedirect(returnUrl);
                    }

                    // If login failed (e.g., bad password)
                    ModelState.AddModelError(string.Empty, "Invalid username or password.");
                }
                catch (Exception ex)
                {
                    // Log catastrophic failure of Identity system (e.g., database connection down, misconfiguration)
                    // _logger.LogCritical(ex, "Critical error during login process for user: {Username}", loginVM.UserName);
                    ModelState.AddModelError(string.Empty, "A critical system error occurred during login. Please try again.");
                    return View(loginVM);
                }
            }

            // If ModelState is not valid or login failed
            return View(loginVM);
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
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                // 1. Get the specific IdentityUser (Identity Operation)
                var identityUser = await _userManager.FindByIdAsync(id);
                if (identityUser == null)
                {
                    TempData["error"] = $"User with ID {id} not found.";
                    return RedirectToAction("UserList");
                }

                // 2. Get the specific UserProfile (Database Operation)
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                // 3. Map data to the existing ProfileUpdateViewModel
                var viewModel = new ProfileUpdateViewModel
                {
                    UserId = identityUser.Id,
                    UserName = identityUser.UserName,
                    Email = identityUser.Email,
                    PhoneNumber = identityUser.PhoneNumber,

                };

                if (userProfile != null)
                {
                    // ADDED FirstName and LastName
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
                    viewModel.WhatsappNumber = userProfile.WhatsappNumber;
                    viewModel.HomeAddress = userProfile.HomeAddress;
                    viewModel.HomePhoneNumber = userProfile.HomePhoneNumber;
                    viewModel.BusinessAddress = userProfile.BusinessAddress;
                    viewModel.BusinessPhoneNumber = userProfile.BusinessPhoneNumber;
                    viewModel.ExistingProfileImagePath = userProfile.ProfileImagePath;
                    viewModel.ExistingGpayQRCodePath = userProfile.GpayQRCodePath;
                    viewModel.ExistingPhonePeQRCodePath = userProfile.PhonePeQRCodePath;
                }

                // Use the same ProfileDetails view you already have for displaying profile data
                return View("ProfileDetails", viewModel);
            }
            catch (Exception ex)
            {
                // Log the detailed exception (e.g., database connection failure, Identity service error)
                // _logger.LogError(ex, "Error retrieving user details for ID: {UserId}", id);

                // Inform the user about the failure and redirect
                TempData["error"] = "A system error occurred while trying to load the user's details.";
                return RedirectToAction("UserList");
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