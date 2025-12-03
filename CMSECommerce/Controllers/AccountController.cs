using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Models; // Ensure this is present for UserProfile
using System.IO; // Required for Path, FileStream, Directory
using Microsoft.AspNetCore.Hosting; // Required for IWebHostEnvironment

namespace CMSECommerce.Controllers
{
    public class AccountController(
            DataContext dataContext,
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IWebHostEnvironment webHostEnvironment) : Controller
    {
        private DataContext _context = dataContext;
        private UserManager<IdentityUser> _userManager = userManager;
        private SignInManager<IdentityUser> _sign_in_manager = signInManager;
        private IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

        // ... (UpdateOrderShippedStatus method remains the same) ...

        private async Task UpdateOrderShippedStatus(string username)
        {
            // 1. Get ALL Order IDs for the current user
            var userOrderIds = await _context.Orders
                .Where(o => o.UserName == username)
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
                await _context.SaveChangesAsync();
            }
        }

        // -----------------------------------------------------------

        // Modified Index Action to call the update method first
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var username = User.Identity.Name;

            // Call the dedicated method to update statuses before displaying the list
            await UpdateOrderShippedStatus(username);

            // Now, fetch the complete, updated list of orders for the user
            // Assuming 'Order' and related models/contexts exist based on the logic below.
            // If Order model is not defined, this action will require it to be added.
            // For now, assume it exists in CMSECommerce.Models.
            List<CMSECommerce.Models.Order> orders = await _context.Orders
                .OrderByDescending(x => x.Id)
                .Where(x => x.UserName == username)
                .ToListAsync();

            return View(orders);
        }

        // ... (Register GET and POST actions remain the same, assuming User, Address models exist) ...

        // Note: The Register Post action is creating an Address model, not a UserProfile model.
        // If FirstName/LastName should be set on registration, the Address model code needs to be adapted or UserProfile created here.
        // For simplicity, I'll only focus on the Profile actions as requested.

        public IActionResult Register()
        {
            // Assuming a ViewModel named 'User' is used for registration.
            // It should be defined and include properties for FirstName, LastName, UserName, Email, Password, etc.
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if(string.IsNullOrEmpty(user.FirstName))
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

                IdentityResult result = await _userManager.CreateAsync(newUser, user.Password);

                if (result.Succeeded)
                {
                    // 1. Create and save the UserProfile record with FirstName/LastName
                    var newProfile = new UserProfile
                    {
                        UserId = newUser.Id,
                        FirstName = user.FirstName, // Assuming FirstName is in the User ViewModel
                        LastName = user.LastName    // Assuming LastName is in the User ViewModel
                        // Other properties will be null/default until the user edits their profile
                    };
                    _context.UserProfiles.Add(newProfile);


                    // 2. Create and save the Address record (keeping original logic)
                    // Assuming Address model exists.
                    var newAddress = new CMSECommerce.Models.Address
                    {
                        UserId = newUser.Id, // Link address to the new IdentityUser's ID
                        StreetAddress = user.StreetAddress,
                        City = user.City,
                        State = user.State,
                        PostalCode = user.PostalCode,
                        Country = user.Country
                    };

                    _context.Addresses.Add(newAddress);
                    await _context.SaveChangesAsync(); // Save both UserProfile and Address

                    // assign Customer role on registration
                    await _userManager.AddToRoleAsync(newUser, "Customer");
                    TempData["success"] = "You have registered successfully!";

                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }

            return View(user);
        }

        // -----------------------------------------------------------


        // Re-aligning Profile() to EditProfile() view
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var identityUser = await _userManager.GetUserAsync(User);
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
                // ADDED FirstName and LastName
                viewModel.FirstName = userProfile.FirstName;
                viewModel.LastName = userProfile.LastName;

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


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditProfile(string id)
        {
            var identityUser = await _userManager.FindByIdAsync(id);

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
                // ADDED FirstName and LastName
                viewModel.FirstName = userProfile.FirstName;
                viewModel.LastName = userProfile.LastName;

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

            // The view is named "Profile" in your original code, which is confusingly named the same as the POST action.
            // I'll keep the view name as "Profile" for consistency with your original code.
            return View("Profile", viewModel);
        }


        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ProfileDetails(string userId="")
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
                // ADDED FirstName and LastName
                viewModel.FirstName = userProfile.FirstName;
                viewModel.LastName = userProfile.LastName;

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


        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Profile(ProfileUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var identityUser = await _userManager.FindByIdAsync(model.UserId);
            //var identityUser = await _userManager.GetUserAsync(User);
            if (identityUser == null)
            {
                TempData["error"] = "User not found.";
                return RedirectToAction("Login");
            }

            // --- 1. Update IdentityUser Basic Info ---
            identityUser.UserName = model.UserName;
            identityUser.Email = model.Email;
            identityUser.PhoneNumber = model.PhoneNumber;

            // Save IdentityUser changes (includes checking for username/email conflicts)
            var identityResult = await _userManager.UpdateAsync(identityUser);
            if (!identityResult.Succeeded)
            {
                foreach (var error in identityResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            // --- 2. Handle UserProfile and File Uploads ---
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

            bool isNewProfile = false;
            if (userProfile == null)
            {
                userProfile = new UserProfile { UserId = identityUser.Id };
                isNewProfile = true;
            }

            // File Upload Helper Function (Included in your original code, re-used here)
            async Task<string> ProcessFileUpload(IFormFile file, string subFolder)
            {
                if (file == null) return null;

                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", subFolder);
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Ensure the directory exists
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }
                // Return the relative path for database storage
                return Path.Combine("images", subFolder, uniqueFileName).Replace("\\", "/");
            }

            // Process Profile Image
            if (model.ProfileImageUpload != null)
            {
                // Delete old image if it exists
                if (!string.IsNullOrEmpty(userProfile.ProfileImagePath))
                {
                    string oldPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfileImagePath);
                    if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                }
                userProfile.ProfileImagePath = await ProcessFileUpload(model.ProfileImageUpload, "profiles");
                userProfile.IsImageApproved = model.IsImageApproved;
            }

            // Process GPay QR Code
            if (model.GpayQRCodeUpload != null)
            {
                // NOTE: Add logic to delete old QR code if needed, similar to profile image.
                userProfile.GpayQRCodePath = await ProcessFileUpload(model.GpayQRCodeUpload, "qrcodes");
            }

            // Process PhonePe QR Code
            if (model.PhonePeQRCodeUpload != null)
            {
                // NOTE: Add logic to delete old QR code if needed, similar to profile image.
                userProfile.PhonePeQRCodePath = await ProcessFileUpload(model.PhonePeQRCodeUpload, "qrcodes");
            }


            // --- 3. Update UserProfile Details ---
            // ADDED FirstName and LastName
            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;

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

            if (isNewProfile)
            {
                _context.UserProfiles.Add(userProfile);
            }
            else
            {
                _context.UserProfiles.Update(userProfile);
            }

            await _context.SaveChangesAsync();

            TempData["success"] = "Your profile has been updated successfully!";
            return RedirectToAction("ProfileDetails", new {userId=userProfile.UserId}); // Redirect to display the updated profile
        }

        // ... (DeleteProfileData method remains the same) ...

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

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

            if (userProfile != null)
            {
                // Delete associated files (images, QR codes)
                if (!string.IsNullOrEmpty(userProfile.ProfileImagePath))
                {
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.ProfileImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                if (!string.IsNullOrEmpty(userProfile.GpayQRCodePath))
                {
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.GpayQRCodePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
                if (!string.IsNullOrEmpty(userProfile.PhonePeQRCodePath))
                {
                    var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, userProfile.PhonePeQRCodePath.TrimStart('/'));
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }

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

        // ... (Login, Logout, OrderDetails, AccessDenied, RequestSeller, RequestStatus remain the same) ...

        public IActionResult Login(string returnUrl)
        {
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel loginVM)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByNameAsync(loginVM.UserName);

                Microsoft.AspNetCore.Identity.SignInResult result = await _sign_in_manager.PasswordSignInAsync(loginVM.UserName, loginVM.Password, false, false);

                if (result.Succeeded)
                {
                    return Redirect(loginVM.ReturnUrl ?? "/");
                }

                ModelState.AddModelError("", "Invalid username or password");
            }

            return View(loginVM);
        }

        public async Task<IActionResult> Logout()
        {
            await _sign_in_manager?.SignOutAsync();

            return Redirect("/");
        }

        public async Task<IActionResult> OrderDetails(int id)
        {

            //var userName = _userManager.GetUserName(User);
            //var userId = _sign_in_manager.UserManager.GetUserId(User);
            //var product = await _context.Products.Where(p => p.OwnerId == userId).FirstOrDefaultAsync();

            Order order = await _context.Orders.Where(x => x.Id == id).FirstOrDefaultAsync();

            List<OrderDetail> orderDetails = await _context.OrderDetails.Where(x => x.OrderId == id).ToListAsync();

            return View(new OrderDetailsViewModel { Order = order, OrderDetails = orderDetails });
        }

        public IActionResult AccessDenied()
        {
            var model = new AccessDeniedViewModel
            {
                Message = "You do not have permission to access this page."
            };
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RequestSeller()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login");

            // Check if a request already exists
            var isUserRequestExist = await _context.SubscriberRequests
                .AnyAsync(r => r.UserId == currentUser.Id);

            if (!isUserRequestExist)
            {
                var newRequest = new SubscriberRequest
                {
                    UserId = currentUser.Id,
                    RequestDate = System.DateTime.Now,
                    // Assuming 'IsApproved' is set to false by default in the model
                    // and Status/Notes will be handled by the Admin.
                    Approved = false
                };
                _context.Add(newRequest);
                await _context.SaveChangesAsync();
            }
            // Check if a request already exists
            var existingRequest = await _context.SubscriberRequests
                .FirstOrDefaultAsync(r => r.UserId == currentUser.Id);

            // Pass the existing request (or null) to the view
            return View(existingRequest);
        }


        [HttpPost]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> RequestSeller(string user = "")
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return RedirectToAction("Login");

            // Check for existing request to prevent duplicates
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

            _context.Add(newRequest);
            await _context.SaveChangesAsync();

            TempData["success"] = "Your seller request has been submitted successfully to the administrator for review.";
            return RedirectToAction("RequestStatus");
        }

        [Authorize]
        public async Task<IActionResult> RequestStatus()
        {
            // Get the current authenticated user's details
            var current = await _userManager.GetUserAsync(User);
            if (current == null) return RedirectToAction("Login");

            // ⭐ NEW: Get the current roles of the user
            var currentRoles = await _userManager.GetRolesAsync(current);
            // Select the first role or default to a safe string like "Guest"
            ViewBag.CurrentRole = currentRoles.FirstOrDefault() ?? "Guest";

            // Attempt to find an existing SubscriberRequest for this user
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


        /// <summary>
        /// Displays a list of all registered IdentityUsers, joining with UserProfile data.
        /// Requires Admin/Staff Authorization to view all users.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Subscriber")] // Restrict access to authorized users
        public async Task<IActionResult> UserList()
        {
            // 1. Get all IdentityUsers
            // Note: We avoid ToListAsync() here to enable more efficient filtering later, but we need ToListAsync() for step 2.
            var allIdentityUsers = await _userManager.Users.ToListAsync();

            // 2. Get all UserProfiles
            var allUserProfiles = await _context.UserProfiles.ToListAsync();
            var profilesDictionary = allUserProfiles.ToDictionary(p => p.UserId, p => p);

            // 3. Create a list of the ProfileUpdateViewModel by combining IdentityUser and UserProfile
            var userListTasks = allIdentityUsers.Select(async user =>
            {
                profilesDictionary.TryGetValue(user.Id, out var profile);
                // Important: GetRolesAsync is an asynchronous operation, hence the Select(async...) structure.
                var roles = await _userManager.GetRolesAsync(user);

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
                    // Assign the user's highest role, or "None"
                    CurrentRole = roles.FirstOrDefault() ?? "None"
                };

                return viewModel;
            }).ToList();

            // Wait for all the role lookups to complete
            var finalUserList = await Task.WhenAll(userListTasks);

            // 4. ⭐ Implementation of the Filter: Exclude users who are in the "Admin" role.
            var filteredUserList = finalUserList
                                       .Where(u => u.CurrentRole != "Admin")
                                       .OrderBy(u => u.UserName) // Optional: Add sorting
                                       .ToList();

            // Note: Consider a dedicated, lighter ViewModel if ProfileUpdateViewModel is too heavy.
            return View(filteredUserList);
        }
        /// <summary>
        /// Displays the full details of a specific user by their ID.
        /// Requires Admin/Staff Authorization to view other users' details.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Subscriber")] // Restrict access to authorized users
        public async Task<IActionResult> UserDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            // 1. Get the specific IdentityUser
            var identityUser = await _userManager.FindByIdAsync(id);
            if (identityUser == null)
            {
                TempData["error"] = $"User with ID {id} not found.";
                return RedirectToAction("UserList");
            }

            // 2. Get the specific UserProfile
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


        [HttpPost]
        [Authorize(Roles = "Admin")] // ⬅️ IMPORTANT: Only Admins should delete other users
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                TempData["error"] = "Invalid user ID provided for deletion.";
                return RedirectToAction("UserList"); // Redirect to a user management list
            }

            // 1. Find the IdentityUser
            var userToDelete = await _userManager.FindByIdAsync(userId);

            if (userToDelete == null)
            {
                TempData["info"] = $"User with ID {userId} not found.";
                return RedirectToAction("UserList");
            }

            // IMPORTANT: Prevent accidental deletion of the current Admin user's account
            // You might adjust this based on your application's specific rules.
            if (userToDelete.Id == _userManager.GetUserId(User))
            {
                TempData["error"] = "You cannot delete your own account via the Admin panel.";
                return RedirectToAction("UserList");
            }

            // --- 2. Delete related custom data (UserProfile, Address, Orders, etc.) ---

            // a. Find and remove UserProfile (where the NOT NULL constraint was an issue)
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (userProfile != null)
            {
                // Add file cleanup logic here (as seen in DeleteProfileData)
                // ... (e.g., delete ProfileImagePath, GpayQRCodePath, etc. files) ...

                // Example file cleanup helper reuse:
                string GetFullPath(string relativePath)
                {
                    if (string.IsNullOrEmpty(relativePath)) return null;
                    return Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/', '\\'));
                }

                // Delete Profile Image
                string profileImagePath = GetFullPath(userProfile.ProfileImagePath);
                if (System.IO.File.Exists(profileImagePath))
                {
                    System.IO.File.Delete(profileImagePath);
                }

                _context.UserProfiles.Remove(userProfile);
            }

            // b. Delete Address records
            var userAddresses = await _context.Addresses
                .Where(a => a.UserId == userId)
                .ToListAsync();
            _context.Addresses.RemoveRange(userAddresses);

            // c. Delete Orders (and related OrderDetails if using cascade delete, or delete OrderDetails explicitly)
            var userOrders = await _context.Orders
                .Where(o => o.UserName == userToDelete.UserName)
                .ToListAsync();
            _context.Orders.RemoveRange(userOrders);

            // d. Save all custom context changes before deleting the IdentityUser
            await _context.SaveChangesAsync();

            // --- 3. Delete the IdentityUser (This automatically removes roles, claims, and tokens) ---
            IdentityResult result = await _userManager.DeleteAsync(userToDelete);

            if (result.Succeeded)
            {
                TempData["success"] = $"User '{userToDelete.UserName}' and all associated data deleted successfully.";
            }
            else
            {
                string errors = string.Join(", ", result.Errors.Select(e => e.Description));
                TempData["error"] = $"Error deleting user: {errors}";
            }

            // Redirect to the list of users
            return RedirectToAction("UserList");
        }



    }
}