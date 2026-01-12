using CMSECommerce.Areas.Admin.Models;
using CMSECommerce.Areas.Admin.Services;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using System.Text.Encodings.Web;


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
            IUserService userService,
            IEmailService emailService) : BaseController
    {
        private DataContext _context = dataContext;
        private UserManager<IdentityUser> _userManager = userManager;
        private SignInManager<IdentityUser> _sign_in_manager = signInManager;
        private IWebHostEnvironment _webHostEnvironment = webHostEnvironment;
        private readonly IUserStatusService _userStatusService = userStatusService;
        private readonly RoleManager<IdentityRole> _roleManager = roleManager;
        private readonly ILogger<AccountController> _logger = logger;
        private readonly IUserService _userService = userService;
        private readonly IEmailService _emailService = emailService;


        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            List<CMSECommerce.Models.Order> orders = new();

            try
            {
                // 1. Efficiently update statuses at the DB level first
                await UpdateOrderShippedStatus(userId);

                // 2. Fetch the updated list
                // .AsNoTracking() improves performance for read-only views
                orders = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.Id)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error retrieving orders for user: {userId}", userId);
                TempData["error"] = "An error occurred while loading your orders.";
            }

            return View(orders);
        }

        private async Task UpdateOrderShippedStatus(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                // Optimization: Only fetch IDs and the Shipped property for orders 
                // that actually meet the criteria to be updated.
                var ordersToUpdate = await _context.Orders
                    .Where(o => o.UserId == userId && !o.Shipped)
                    // An order is ready if it has items AND all items are processed
                    .Where(o => o.OrderDetails.Any() && o.OrderDetails.All(d => d.IsProcessed))
                    .ToListAsync();

                if (ordersToUpdate.Any())
                {
                    foreach (var order in ordersToUpdate)
                    {
                        order.Shipped = true;
                        _context.Entry(order).Property(x => x.Shipped).IsModified = true;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception)
            {
                // Log error but do not break the UI flow
            }
        }

        // --- 3. REGISTER (GET) ---
        // Updated to accept the 'username' parameter from the Login redirect
        [HttpGet]
        public IActionResult Register(string username = null)
        {
            // If the user was redirected from Login, pre-populate the ViewModel
            var model = new RegisterViewModel
            {
                Username = username
            };

            return View(model);
        }

        // --- LOGIN (GET) ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string username = null, string returnUrl = null)
        {
            var vm = new LoginViewModel
            {
                UserName = username,
                ReturnUrl = returnUrl
            };
            return View(vm);
        }

        // --- LOGIN (POST) ---
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            IdentityUser user = null;
            var identifier = model.UserName?.Trim();

            if (string.IsNullOrEmpty(identifier))
            {
                ModelState.AddModelError(string.Empty, "Please enter your username, email, ITS or mobile.");
                return View(model);
            }

            // Try email
            if (identifier.Contains("@"))
            {
                user = await _userManager.FindByEmailAsync(identifier);
            }

            // Try ITS (8 digits) via UserProfile
            if (user == null && identifier.All(char.IsDigit) && identifier.Length == 8)
            {
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.ITSNumber == identifier);
                if (profile != null)
                {
                    user = await _userManager.FindByIdAsync(profile.UserId);
                }
            }

            // Try username
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(identifier);
            }

            // Try phone number
            if (user == null)
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier);
            }

            if (user == null)
            {
                return InvalidLoginResponse(model);
            }

            // Check lockout
            if (await _userManager.IsLockedOutAsync(user))
            {
                ViewBag.IsLockedOut = true;
                return View(model);
            }

            var signInResult = await _sign_in_manager.PasswordSignInAsync(user.UserName, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (signInResult.Succeeded)
            {
                try
                {
                    var status = await _context.UserStatuses.FindAsync(user.Id);
                    if (status != null)
                    {
                        status.IsOnline = true;
                        status.LastActivity = DateTime.UtcNow;
                        await _context.SaveChangesAsync();
                    }
                }
                catch { }

                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            if (signInResult.IsLockedOut)
            {
                ViewBag.IsLockedOut = true;
                return View(model);
            }

            return InvalidLoginResponse(model);
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
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!model.HasAcceptedTerms)
            {
                ModelState.AddModelError("HasAcceptedTerms", "You must agree to the terms.");
            }

            if (!ModelState.IsValid) return View(model);

            // Check if user already exists
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null)
            {
                return RedirectToAction("Login", new { username = model.Username });
            }




            // 1. Simplified Uniqueness Check 
            // We only check UserManager for existing credentials to prevent crashes.
            bool isDuplicate = await _userManager.Users.AnyAsync(u =>
                u.UserName == model.Username ||
                u.Email == model.Email ||
                u.PhoneNumber == model.PhoneNumber);



            if (isDuplicate)
            {
                ModelState.AddModelError("", "Username, Email, or Phone Number is already in use.");
                return View(model);
            }

            try
            {
                // 2. Create Identity User (Core Identity functionality)
                var newUser = new IdentityUser
                {
                    UserName = model.Username,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    EmailConfirmed = true // Mimicking Amazon's pre-verified or direct access flow
                };

                var result = await _userManager.CreateAsync(newUser, model.Password);

                if (result.Succeeded)
                {
                    // Define the specific content the user just saw in the modal
                    string agreementText = @"
                <h6>1. Acceptance of Terms</h6><p>By accessing or using the Weypaari platform...</p>
                <h6>2. Eligibility & Registration</h6><p>To use Weypaari, you must provide a valid 8-digit ITS...</p>
                <h6>3. Privacy & Data Security</h6><p>Your privacy is important to us...</p>
                <h6>4. User Conduct</h6><p>Users shall not engage in any activity...</p>
                <h6>5. Transactions & Listings</h6><p>Weypaari provides a marketplace platform...</p>
                <h6>6. Termination of Use</h6><p>We reserve the right to terminate...</p>
                <h6>7. Modifications to Service</h6><p>Weypaari reserves the right to modify...</p>";
                    var agreement = new UserAgreement
                    {
                        UserId = newUser.Id,
                        AgreementType = "Registration_Terms_Privacy",
                        FullContent = agreementText, // Mapping the full snapshot
                        Version = "v1.0-2026-01",
                        AcceptedAt = DateTime.UtcNow,
                        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
                    };

                    _context.UserAgreements.Add(agreement);
                    await _context.SaveChangesAsync();

                    // 3. Role Assignment
                    // We keep this to ensure your [Authorize(Roles="Customer")] attributes don't break.
                    await _userManager.AddToRoleAsync(newUser, "Customer");

                    // 4. Immediate Sign-In (The Amazon Experience)
                    await _sign_in_manager.SignInAsync(newUser, isPersistent: false);

                    _logger.LogInformation("User {Username} registered successfully.", model.Username);
                    return RedirectToAction("Index", "Home");
                }

                // Add Identity errors (like password complexity issues) to the UI
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            catch (Exception ex)
            {
                // Maintain existing error logging for troubleshooting
                _logger.LogError(ex, "Registration failed for user {Username}", model.Username);
                ModelState.AddModelError("", "An internal error occurred. Please try again.");
            }

            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ValidateIdentifier(string value, string type)
        {
            if (string.IsNullOrWhiteSpace(value)) return Json(new { isAvailable = true });

            // 1. Check Identity Tables (Username, Email, Phone)
            bool existsInIdentity = await _userManager.Users.AnyAsync(u =>
                u.UserName == value || u.Email == value || u.PhoneNumber == value);

            // 2. Check Profile Tables (ITS, WhatsApp)
            bool existsInProfile = await _context.UserProfiles.AnyAsync(up =>
                up.ITSNumber == value || up.WhatsAppNumber == value);

            // 3. Check Store Tables (Store Email, GSTIN, etc)
            bool existsInStore = await _context.Stores.AnyAsync(s =>
                s.Email == value || s.Contact == value || s.GSTIN == value);

            if (existsInIdentity || existsInProfile || existsInStore)
            {
                // We determine if it's a username match specifically for the frontend redirect
                bool isUsernameTaken = (type == "Username" && await _userManager.FindByNameAsync(value) != null);

                return Json(new
                {
                    isAvailable = false,
                    // We pass a specific flag if it's a username so the JS knows to redirect
                    isUsernameType = type == "Username",
                    message = $"This {type} is already associated with an account. Please verify your details or sign in."
                });
            }

            return Json(new { isAvailable = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelItem(int orderDetailId, string reason)
        {
            // Include OrderDetails to check the sibling items
            var detail = await _context.OrderDetails
                .Include(od => od.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail == null) return NotFound();

            // 1. Time Validation (24-hour rule)
            var timeLimit = detail.Order.OrderDate.Value.AddHours(24);
            if (DateTime.Now > timeLimit)
            {
                TempData["Error"] = "Cancellation period (24 hours) has expired.";
                return RedirectToAction("OrderDetails", new { id = detail.OrderId });
            }

            // 2. Role-Based Permission Logic
            bool canCancel = false;
            string roleTag = "";

            if (User.IsInRole("Admin"))
            {
                canCancel = true;
                roleTag = "Admin";
            }
            else if (User.IsInRole("Seller") && detail.ProductOwner == User.Identity.Name)
            {
                canCancel = true;
                roleTag = "Seller";
            }
            else if (detail.Customer == User.Identity.Name) // Matches user who placed order
            {
                canCancel = true;
                roleTag = "User";
            }

            if (!canCancel) return Forbid();

            // 3. Apply Cancellation
            detail.IsCancelled = true;
            detail.CancellationReason = reason;
            detail.CancelledByRole = roleTag;

            // Optional: Update GrandTotal of the Order
            detail.Order.GrandTotal -= (detail.Price * detail.Quantity);

            // --- NEW LOGIC: Check if all items in this order are now cancelled ---
            if (detail.Order.OrderDetails.All(od => od.IsCancelled))
            {
                detail.Order.IsCancelled = true;
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Item cancelled successfully.";

            return RedirectToAction("orderdetails", new { id = detail.OrderId });
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReturnItem(int orderDetailId, int orderId, string reason)
        {
            // 1. Fetch both the specific item and the parent order
            var detail = await _context.OrderDetails.FindAsync(orderDetailId);
            var order = await _context.Orders.FindAsync(orderId);
            

            if (detail == null || order == null)
            {
                TempData["Error"] = "Order information not found.";
                return RedirectToAction("MyOrders");
            }

            // 2. Update OrderDetail fields based on your View's logic
            // We mark it as Returned so the View shows the "Return Pending" badge
            detail.IsReturned = true;
            detail.ReturnReason = reason;
            detail.ReturnDate = DateTime.Now;

            // Optional: Also mark as cancelled if your logic requires it for stock/accounting
            detail.IsCancelled = false;
            detail.CancellationReason = "Returned: " + reason;
            detail.IsProcessed = false;

            // 3. Update the Parent Order status
            // Setting Shipped to false moves it back to "Preparing for Shipment/Pending" status in your UI
            order.Shipped = false;
            order.IsCancelled = false;
            order.ShippedDate = null; // Clear the shipped date since it's no longer considered "Completed"

           

            // 4. Save both changes in a single transaction
            await _context.SaveChangesAsync();

            TempData["Success"] = "Return request submitted. Your order status has been updated to Pending.";

            return RedirectToAction("OrderDetails", new { id = orderId });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string reason)
        {
            var userId = _userManager.GetUserId(User);

            // Fetch the order, including items, ensuring it belongs to the logged-in user
            var order = await _context.Orders
                .Include(o => o.OrderDetails)          
                
               .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders");
            }

            // 1. Time Validation (24-hour rule)
            var timeLimit = order.OrderDate.Value.AddHours(24);
            if (DateTime.Now > timeLimit)
            {
                TempData["Error"] = "Cancellation period (24 hours) has expired.";
                return RedirectToAction("OrderDetails", new { id = order.Id });
            }

            // Business Logic: Prevent cancellation if already shipped
            if (order.Shipped)
            {
                TempData["Error"] = "Shipped orders cannot be cancelled. Please contact support for a return.";
                return RedirectToAction("OrderDetails", new { id = orderId });
            }

            try
            {
                // 1. Update the Main Order Status
                order.IsCancelled = true;

                // 2. Update all associated items
                foreach (var item in order.OrderDetails)
                {
                    item.IsCancelled = true;
                    item.CancellationReason = reason ?? "Cancelled by customer";
                    item.CancelledByRole = "Customer";
                    item.IsProcessed = false;
                }

                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order #{orderId} has been successfully cancelled.";
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while trying to cancel the order.";
            }

            return RedirectToAction("MyOrders","Orders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReActivateOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Fetch the order and include the OrderDetails
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                TempData["Error"] = "Order not found.";
                return RedirectToAction("MyOrders", "Orders");
            }

            // 2. Security/Business Logic: Only allow re-activation if it was cancelled
            if (!order.IsCancelled)
            {
                TempData["Info"] = "This order is already active.";
                return RedirectToAction("MyOrders", "Orders");
            }

            try
            {
                // 3. Re-activate the Main Order
                order.IsCancelled = false;
                order.OrderDate = DateTime.Now; // Optional: Reset date to move it to the top of the list

                // 4. Re-activate every item under the order
                foreach (var item in order.OrderDetails)
                {
                    item.IsCancelled = false;
                    item.CancellationReason = null;
                    item.CancelledByRole = null;
                    // Note: Keep IsProcessed as false so the seller sees it as a new task
                    item.IsProcessed = false;
                }

                _context.Update(order);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Order #{orderId} has been successfully re-activated!";
            }
            catch (Exception)
            {
                TempData["Error"] = "An error occurred while trying to re-activate the order.";
            }

            return RedirectToAction("MyOrders", "Orders");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReOrder(int orderId)
        {
            var userId = _userManager.GetUserId(User);

            // 1. Fetch only necessary data. 
            // We use Include to get details, but we won't modify the originals if creating a new order.
            var oldOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (oldOrder == null)
            {
                TempData["Error"] = "Original order not found.";
                return RedirectToAction("MyOrders", "Orders");
            }

            try
            {
                if (oldOrder.IsCancelled)
                {
                    // CASE 1: RE-ACTIVATE EXISTING ORDER
                    // Efficiently update the existing record
                    oldOrder.IsCancelled = false;
                    oldOrder.OrderDate = DateTime.Now;
                    oldOrder.Shipped = false;
                    oldOrder.ShippedDate = null; // Reset shipping date

                    foreach (var item in oldOrder.OrderDetails)
                    {
                        item.IsCancelled = false;
                        item.IsProcessed = false;
                        item.IsReturned = false; // Reset return status if any
                    }

                    _context.Update(oldOrder);
                    TempData["Success"] = $"Order #{orderId} has been successfully re-activated!";
                }
                else
                {
                    // CASE 2: CREATE NEW DUPLICATE ORDER
                    // We project the old details into NEW objects to avoid EF tracking conflicts
                    var newOrderDetails = oldOrder.OrderDetails.Select(item => new OrderDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Image = item.Image,
                        ProductOwner = item.ProductOwner,
                        IsProcessed = false, // Fresh order starts as unprocessed
                        IsCancelled = false,
                        IsReturned = false
                    }).ToList();

                    var newOrder = new Order
                    {
                        UserId = userId,
                        UserName = oldOrder.UserName,
                        OrderDate = DateTime.Now,
                        GrandTotal = oldOrder.GrandTotal,
                        Shipped = false,
                        IsCancelled = false,
                        OrderDetails = newOrderDetails
                    };

                    _context.Orders.Add(newOrder);
                    // SaveChangesAsync is called once here to persist the new order and its details
                    await _context.SaveChangesAsync();

                    TempData["Success"] = $"New order #{newOrder.Id} has been placed!";
                }

                // Final save for the "Re-activate" case or any trailing changes
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log ex if a logger is available
                TempData["Error"] = "An error occurred while trying to process the re-order.";
            }

            return RedirectToAction("MyOrders", "Orders");
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
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == identityUser.Id);

                if(userProfile == null || User.IsInRole("Customer"))
                {
                    // Log info: New user without profile
                    _logger.LogInformation("No UserProfile found for user {UserId}. Initializing new profile view.", identityUser.Id);

                    ViewBag.ProfileStatus = false;
                }
                else
                {
                    ViewBag.ProfileStatus = true;
                }

                    // Map Identity data to the ViewModel
                    var viewModel = new ProfileUpdateViewModel
                    {
                        // CORE IDENTITY (Always exists)
                        UserId = identityUser.Id,
                        UserName = identityUser.UserName,
                        Email = identityUser.Email,
                        PhoneNumber = identityUser.PhoneNumber,

                        // PROFILE FIELDS (May be null for new Amazon-style users)
                        FirstName = userProfile?.FirstName ?? "",
                        LastName = userProfile?.LastName ?? "",
                        ITSNumber = userProfile?.ITSNumber ?? "",
                        About = userProfile?.About,
                        Profession = userProfile?.Profession,
                        ServicesProvided = userProfile?.ServicesProvided,
                        //CurrentRole = userProfile?.CurrentRole,
                        WhatsappNumber = userProfile?.WhatsAppNumber ?? identityUser.PhoneNumber, // Default to Mobile
                        IsProfileVisible = userProfile?.IsProfileVisible ?? true,

                        // IMAGE STATUS
                        IsImageApproved = userProfile?.IsImageApproved ?? false,
                        IsImagePending = userProfile?.IsImagePending ?? false,
                        ExistingProfileImagePath = userProfile?.ProfileImagePath,

                        // STORE FIELDS (May be null)
                        StoreId = userProfile?.StoreId,
                        StoreName = userProfile?.Store?.StoreName ?? "",
                        StoreStreetAddress = userProfile?.Store?.StreetAddress ?? "",
                        StoreCity = userProfile?.Store?.City ?? "",
                        StorePostCode = userProfile?.Store?.PostCode ?? "",
                        StoreCountry = userProfile?.Store?.Country ?? "",
                        StoreEmail = userProfile?.Store?.Email ?? identityUser.Email, // Default to User Email
                        StoreContact = userProfile?.Store?.Contact ?? identityUser.PhoneNumber, // Default to User Phone
                        GSTIN = userProfile?.Store?.GSTIN

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
            // 1. Resolve User ID (Priority: Parameter > Current Logged-in User)
            if (string.IsNullOrEmpty(id))
            {
                id = _userManager.GetUserId(User);
            }

            if (string.IsNullOrEmpty(id))
            {
                TempData["error"] = "Could not identify the user.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 2. Fetch Identity User
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return NotFound();
                }

                // 3. Fetch UserProfile including the related Store (One-to-Many)
                // Use AsNoTracking for better performance on read-only views
                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == id);
                if (userProfile == null || User.IsInRole("Customer"))
                {
                    // Log info: New user without profile
                    _logger.LogInformation("No UserProfile found for user {UserId}. Initializing new profile view.", id);

                    ViewBag.ProfileStatus = false;
                }
                else
                {
                    ViewBag.ProfileStatus = true;
                }

                var roles = await _userManager.GetRolesAsync(user);

                // 4. Map to EditUserModel (Ensuring Store data is flattened for the View)
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

                    // Store Details (Flattened from the navigation property)
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

                // 5. Populate Role List for Dropdowns
                ViewBag.Roles = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

                // 6. Handle ITS availability logic for display
                ViewBag.ITSAvailable = !string.IsNullOrEmpty(userProfile?.ITSNumber)
                                        ? userProfile.ITSNumber
                                        : ITSAvailable.ToString();

                return View("ProfileDetails", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading user ID: {UserId} for edit.", id);
                TempData["error"] = "An error occurred while loading profile data.";
                return RedirectToAction("Index");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> ProfileDetailsView(string userId = "")
        {
            // 1. Resolve User ID
            if (string.IsNullOrEmpty(userId)) userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            try
            {
                // 2. Fetch Identity User
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null) return NotFound();

                // 3. Fetch UserProfile and Store details
                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (userProfile == null || User.IsInRole("Customer"))
                {
                    // Log info: New user without profile
                    _logger.LogInformation("No UserProfile found for user {UserId}. Initializing new profile view.", userId);

                    ViewBag.ProfileStatus = false;
                }
                else
                {
                    ViewBag.ProfileStatus = true;
                }

                var roles = await _userManager.GetRolesAsync(user);

                // HELPER: Clean path and add versioning to force browser refresh
                string GetValidPath(string dbPath, string defaultPath = null)
                {
                    if (string.IsNullOrEmpty(dbPath)) return defaultPath;
                    string cleanPath = dbPath.StartsWith("/") ? dbPath : "/" + dbPath;
                    return $"{cleanPath}?v={DateTime.Now.Ticks}";
                }

                // 4. Map to EditUserModel (using the same model for consistency)
                var model = new EditUserModel
                {
                    // Identity Fields
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault(),

                    // Personal Details
                    FirstName = userProfile?.FirstName,
                    LastName = userProfile?.LastName,
                    ITSNumber = userProfile?.ITSNumber,
                    Profession = userProfile?.Profession,
                    About = userProfile?.About,
                    ServicesProvided = userProfile?.ServicesProvided,

                    // Contact & Address
                    HomeAddress = userProfile?.HomeAddress,
                    HomePhoneNumber = userProfile?.HomePhoneNumber,
                    BusinessAddress = userProfile?.BusinessAddress,
                    BusinessPhoneNumber = userProfile?.BusinessPhoneNumber,

                    // Social Media
                    LinkedInUrl = userProfile?.LinkedInUrl,
                    FacebookUrl = userProfile?.FacebookUrl,
                    InstagramUrl = userProfile?.InstagramUrl,
                    WhatsAppNumber = userProfile?.WhatsAppNumber,

                    // Media (with Cache Buster)
                    ProfileImagePath = GetValidPath(userProfile?.ProfileImagePath, "/images/default_profile.png"),
                    GpayQRCodePath = GetValidPath(userProfile?.GpayQRCodePath),
                    PhonePeQRCodePath = GetValidPath(userProfile?.PhonePeQRCodePath),

                    IsImageApproved = userProfile?.IsImageApproved ?? false,
                    IsProfileVisible = userProfile?.IsProfileVisible ?? true,

                    // Store Details (Flattened)
                    StoreId = userProfile?.Store?.Id,
                    StoreName = userProfile?.Store?.StoreName ?? "No Store Assigned",
                    StoreStreetAddress = userProfile?.Store?.StreetAddress,
                    StoreCity = userProfile?.Store?.City,
                    StorePostCode = userProfile?.Store?.PostCode,
                    StoreCountry = userProfile?.Store?.Country,
                    GSTIN = userProfile?.Store?.GSTIN,
                    StoreEmail = userProfile?.Store?.Email,
                    StoreContact = userProfile?.Store?.Contact
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading ProfileDetailsView for User: {UserId}", userId);
                TempData["error"] = "An error occurred while retrieving profile details.";
                return RedirectToAction("Index", "Home");
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProfileDetails(EditUserModel model)
        {
            // Reload roles
            try { ViewBag.Roles = _roleManager.Roles.Select(r => r.Name).ToList(); }
            catch { ViewBag.Roles = new List<string>(); }

            if (!ModelState.IsValid) return View("ProfileDetailsView", model);

            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store)
                    .FirstOrDefaultAsync(p => p.UserId == model.Id);

                if (userProfile == null || User.IsInRole("Customer"))
                {
                    // Log info: New user without profile
                    _logger.LogInformation("No UserProfile found for user {UserId}. Initializing new profile view.", user.Id);

                    ViewBag.ProfileStatus = false;
                }
                else
                {
                    ViewBag.ProfileStatus = true;
                }

                if (userProfile == null)
                {
                    userProfile = new UserProfile { UserId = user.Id };
                    _context.UserProfiles.Add(userProfile);
                }

                // Ensure Store exists
                if (userProfile.StoreId == null)
                {
                    var newStore = new Store
                    {
                        StoreName = model.StoreName ?? $"{model.FirstName}'s Store",
                        StreetAddress = model.StoreStreetAddress ?? "Not Provided",
                        City = model.StoreCity ?? "Not Provided",
                        PostCode = model.StorePostCode ?? "00000",
                        Country = model.StoreCountry ?? "Not Provided",
                        GSTIN = model.GSTIN,
                        Email = model.StoreEmail ?? model.Email,
                        Contact = model.StoreContact ?? model.PhoneNumber
                    };
                    _context.Stores.Add(newStore);
                    await _context.SaveChangesAsync(); // Generate Store Id
                    userProfile.StoreId = newStore.Id;
                    userProfile.Store = newStore;
                }

                // --- RECTIFIED IMAGE UPLOAD LOGIC ---
                // We use Path.Combine for the physical folder on the server
                string subPath = Path.Combine("images", "useruploads");
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, subPath);

                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                // 1. Profile Image
                if (model.ProfileImageFile != null)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProfileImageFile.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImageFile.CopyToAsync(fileStream);
                    }
                    // SAVE THIS TO DB: The Web URL, not the physical path
                    userProfile.ProfileImagePath = "/images/useruploads/" + fileName;
                }

                // 2. GPay QR
                if (model.GpayQRCodeFile != null)
                {
                    string fileName = "gpay_" + Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.GpayQRCodeFile.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.GpayQRCodeFile.CopyToAsync(fileStream);
                    }
                    userProfile.GpayQRCodePath = "/images/useruploads/" + fileName;
                }

                // 3. PhonePe QR
                if (model.PhonePeQRCodeFile != null)
                {
                    string fileName = "phonepe_" + Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.PhonePeQRCodeFile.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.PhonePeQRCodeFile.CopyToAsync(fileStream);
                    }
                    userProfile.PhonePeQRCodePath = "/images/useruploads/" + fileName;
                }

                // Map Store fields from model to userProfile.Store
                if (userProfile.Store != null)
                {
                    userProfile.Store.StoreName = model.StoreName ?? userProfile.Store.StoreName;
                    userProfile.Store.StreetAddress = model.StoreStreetAddress ?? userProfile.Store.StreetAddress;
                    userProfile.Store.City = model.StoreCity ?? userProfile.Store.City;
                    userProfile.Store.PostCode = model.StorePostCode ?? userProfile.Store.PostCode;
                    userProfile.Store.Country = model.StoreCountry ?? userProfile.Store.Country;
                    userProfile.Store.GSTIN = model.GSTIN ?? userProfile.Store.GSTIN;
                    userProfile.Store.Email = model.StoreEmail ?? userProfile.Store.Email;
                    userProfile.Store.Contact = model.StoreContact ?? userProfile.Store.Contact;
                }

                // Map UserProfile fields from model
                userProfile.FirstName = model.FirstName;
                userProfile.LastName = model.LastName;
                userProfile.ITSNumber = model.ITSNumber;
                userProfile.About = model.About;
                userProfile.Profession = model.Profession;
                userProfile.ServicesProvided = model.ServicesProvided;
                userProfile.IsProfileVisible = model.IsProfileVisible;
                userProfile.LinkedInUrl = model.LinkedInUrl;
                userProfile.FacebookUrl = model.FacebookUrl;
                userProfile.InstagramUrl = model.InstagramUrl;
                userProfile.WhatsAppNumber = model.WhatsAppNumber;
                userProfile.HomeAddress = model.HomeAddress;
                userProfile.HomePhoneNumber = model.HomePhoneNumber;
                userProfile.BusinessAddress = model.BusinessAddress;
                userProfile.BusinessPhoneNumber = model.BusinessPhoneNumber;

                _context.Entry(userProfile).State = EntityState.Modified;
                var updateResult = await _userManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    await transaction.RollbackAsync();
                    foreach (var error in updateResult.Errors) ModelState.AddModelError("", error.Description);
                    return View("ProfileDetailsView", model);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["success"] = "Profile updated successfully.";
                // IMPORTANT: Use userId parameter here to match your GET route
                return RedirectToAction("ProfileDetailsView", new { userId = model.Id });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Profile update failed");
                ModelState.AddModelError("", "Error: " + ex.Message);
                return View("ProfileDetailsView", model);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserRole(string userId, string newRole)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newRole))
            {
                return BadRequest("Invalid user ID or role.");
            }

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound($"User with ID {userId} not found.");
                }

                // Get the current roles for the user
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove the user from all current roles
                await _userManager.RemoveFromRolesAsync(user, currentRoles);

                // Add the user to the new role
                var result = await _userManager.AddToRoleAsync(user, newRole);

                if (result.Succeeded)
                {
                    return Ok($"User role updated to {newRole}.");
                }

                return StatusCode(500, "Error updating user role.");
            }
            catch (Exception ex)
            {
                // Log the error (could be DB issues, etc.)
                // _logger.LogError(ex, "Error updating user role for user ID: {UserId}", userId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            // Encode the token to make it URL-safe
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var encodedToken = WebEncoders.Base64UrlEncode(tokenBytes);

            var resetLink = Url.Action("ResetPassword", "Account", new { token = encodedToken, email = user.Email }, Request.Scheme);

            try
            {
                var subject = "Password Reset Request";
                var body = $@"
                <h2>Password Reset</h2>
                <p>You have requested to reset your password for your account.</p>
                <p>Please click the link below to reset your password:</p>
                <p><a href='{resetLink}'>Reset Password</a></p>
                <p>If you did not request this password reset, please ignore this email.</p>
                <p>This link will expire in 24 hours.</p>
                <br>
                <p>Best regards,<br>Weypaari Team</p>";

                // Use configured sender (we expect EmailSettings to be set to weypaari@gmail.com)
                await _emailService.SendEmailAsync(user.Email, subject, body);
                _logger.LogInformation("Password reset email sent to {Email}", model.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", model.Email);
                TempData["error"] = "An error occurred while sending the password reset email. Please try again.";
                return View(model);
            }

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        // --- FORGOT PASSWORD CONFIRMATION ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            // Renders Views/Account/ForgotPasswordConfirmation.cshtml
            return View();
        }

        // Explicit lowercase route for /account/forgotpasswordconfirmation
        [HttpGet("account/forgotpasswordconfirmation")]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmationRoute()
        {
            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token = null, string email = null)
        {
            if (token == null || email == null)
            {
                TempData["error"] = "Invalid password reset token.";
                return RedirectToAction("ForgotPassword");
            }

            // Token is expected to be Base64Url encoded; pass through to view as-is
            var model = new ResetPasswordViewModel
            {
                Token = token,
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation");
            }

            // Decode the token from Base64Url back to the original token
            string decodedToken;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(model.Token);
                decodedToken = Encoding.UTF8.GetString(tokenBytes);
            }
            catch
            {
                ModelState.AddModelError(string.Empty, "Invalid token format.");
                return View(model);
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password reset successful for user {Email}", model.Email);
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // Explicit lowercase route for reset password confirmation to match URLs like /account/resetpasswordconfirmation
        [HttpGet("account/resetpasswordconfirmation")]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmationRoute()
        {
            return View("ResetPasswordConfirmation");
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
        [Authorize]
        public async Task<IActionResult> OrderDetails(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (order == null) return RedirectToAction("Index", "Orders");

                // --- NEW LOGIC: CALCULATE PROGRESS STEP ---
                int currentStep = 1; // Default: Ordered
                if (order.IsCancelled) currentStep = 0;
                else if (order.Shipped) currentStep = 4; // Delivered/Shipped
                else if (order.OrderDetails.Any(x => x.IsProcessed)) currentStep = 3; // Dispatched
                else if (order.OrderDetails.Count > 0) currentStep = 2; // Processed/Accepted

                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == order.UserId);

                var viewModel = new OrderDetailsViewModel
                {
                    Order = order,
                    OrderDetails = order.OrderDetails?.ToList() ?? new List<OrderDetail>(),
                    UserProfile = userProfile ?? new UserProfile { FirstName = "Guest" },
                    // Ensure your ViewModel has a 'CurrentStep' property, or use ViewBag
                };

                ViewBag.CurrentStep = currentStep;

                return View(viewModel);
            }
            catch (Exception) { return RedirectToAction("Index", "Orders"); }
        }

        [HttpGet]
        public async Task<IActionResult> ViewTrackingImage(int orderDetailId)
        {
            // 1. Get current user
            var userId = _userManager.GetUserId(User);

            // 2. Fetch the order detail and verify ownership
            var orderDetail = await _context.OrderDetails
                .Include(od => od.Order)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (orderDetail == null || string.IsNullOrEmpty(orderDetail.DeliveryImageUrl))
            {
                return NotFound("Image not found.");
            }

            // Security check: Is this the user's order? (Or is the user an Admin?)
            if (orderDetail.Order.UserId != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // 3. Construct the physical path
            // It is recommended to store these in a 'PrivateUploads' folder outside wwwroot
            var filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "PrivateUploads", "DeliveryProof", orderDetail.DeliveryImageUrl);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("The physical image file is missing on the server.");
            }

            // 4. Return the file with the correct MIME type
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => "application/octet-stream"
            };

            return PhysicalFile(filePath, contentType);
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

                // Database Operation: Check if user has a valid ITS Number
                var userProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
                bool ITSAvailable = false;
                if (userProfile != null)
                {
                    ITSAvailable = !string.IsNullOrEmpty(userProfile.ITSNumber);
                }

                if (!ITSAvailable)
                {
                    TempData["error"] = "You must have a valid ITS Number in your profile to request seller access. Please update your profile and try again.";
                    return RedirectToAction("ProfileDetails", new { ITSAvailable = ITSAvailable });
                }

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
                if (current == null) return RedirectToAction("Login");

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

                // 2. Fetch all Profiles with their associated Store (Single Join)
                // Using AsNoTracking() for high-speed read-only list generation
                var allUserProfiles = await _context.UserProfiles
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .ToListAsync();

                // 3. Fetch ALL User-Role mappings at once
                // This avoids calling _userManager.GetRolesAsync inside a loop (which causes DB saturation)
                var userRoles = await (from ur in _context.UserRoles
                                       join r in _context.Roles on ur.RoleId equals r.Id
                                       select new { ur.UserId, r.Name }).ToListAsync();

                // Map for fast memory lookup (O(1) complexity)
                var profilesDict = allUserProfiles.ToDictionary(p => p.UserId, p => p);
                var rolesDict = userRoles.GroupBy(ur => ur.UserId)
                                         .ToDictionary(g => g.Key, g => g.First().Name);

                // 4. Map to ViewModel in memory
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

                        // --- Store Property Mapping ---
                        // We use safe navigation (?.) to prevent crashes if a user lacks a profile/store
                        StoreName = profile?.Store?.StoreName ?? "Independent",
                        StoreId = profile?.Store?.Id,
                        StoreCity = profile?.Store?.City,
                        StoreContact = profile?.Store?.Contact,
                        StoreEmail = profile?.Store?.Email,
                        StoreStreetAddress = profile?.Store?.StreetAddress,
                        StorePostCode = profile?.Store?.PostCode,

                        ExistingProfileImagePath = profile?.ProfileImagePath
                    };
                }).ToList();

                // 5. Apply Filtering Logic
                IEnumerable<ProfileUpdateViewModel> filteredList;

                if (User.IsInRole("Admin"))
                {
                    // Admins see everyone who is marked visible
                    filteredList = finalUserList.Where(u => u.IsProfileVisible);
                }
                else
                {
                    // Subscribers see visible users but NOT Admins
                    filteredList = finalUserList.Where(u => u.IsProfileVisible && u.CurrentRole != "Admin");
                }

                // 6. Sort and Return
                // Sort so that "Independent" sellers appear at the bottom, and stores are alphabetical
                var result = filteredList
                    .OrderBy(u => u.StoreName == "Independent")
                    .ThenBy(u => u.StoreName)
                    .ToList();

                return View(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading the User List Directory.");
                TempData["error"] = "Unable to load the business directory.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Subscriber")]
        public async Task<IActionResult> UserDetails(string id)
        {
            // 1. Determine User ID (Use parameter, fall back to current logged-in user)
            if (string.IsNullOrEmpty(id))
            {
                id = _userManager.GetUserId(User);
            }

            if (string.IsNullOrEmpty(id))
            {
                TempData["error"] = "User ID could not be determined. Please log in again.";
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // 2. Fetch Identity User
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    TempData["error"] = "User not found.";
                    return NotFound();
                }

                // 3. Fetch Profile + Store (Single efficient database hit)
                // Using AsNoTracking because this is a read-only detail view
                var userProfile = await _context.UserProfiles
                    .Include(p => p.Store)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserId == id);

                var roles = await _userManager.GetRolesAsync(user);

                // 4. Map to EditUserModel (Ensuring Store data is included)
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

                    // Store Details (Flattened from the navigation property)
                    StoreId = userProfile?.Store?.Id,
                    StoreName = userProfile?.Store?.StoreName ?? "Independent",
                    StoreCity = userProfile?.Store?.City,
                    StoreContact = userProfile?.Store?.Contact,
                    StoreEmail = userProfile?.Store?.Email,
                    GSTIN = userProfile?.Store?.GSTIN,
                    StoreStreetAddress = userProfile?.Store?.StreetAddress,
                    StorePostCode = userProfile?.Store?.PostCode,
                    StoreCountry = userProfile?.Store?.Country
                };

                // 5. Return to the Read-Only Details View
                return View("ProfileDetailsView", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading details for User ID: {UserId}", id);
                TempData["error"] = "An error occurred while loading the profile.";
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

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Invoice(int id)
        {
            try
            {
                // 1. Fetch the Order and its Line Items in a single optimized query
                // Using .Include(o => o.OrderDetails) reduces database round-trips
                var order = await _context.Orders
                    .Include(o => o.OrderDetails)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    _logger.LogWarning("Invoice requested for non-existent Order ID: {OrderId}", id);
                    return NotFound();
                }

                // 2. Fetch Buyer Profile (for billing details)
                var buyerProfile = await _context.UserProfiles
                    .Include(u => u.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == order.UserId);

                // 3. Fetch unique ProductOwners (sellers) from OrderDetails
                var productOwners = order.OrderDetails
                    .Select(od => od.ProductOwner)
                    .Distinct()
                    .Where(po => !string.IsNullOrEmpty(po))
                    .ToList();

                // 4. Fetch Seller Profiles including their Stores
                var sellerProfiles = await _context.UserProfiles
                    .Include(u => u.Store)
                    .AsNoTracking()
                    .Where(u => productOwners.Contains(u.UserId))
                    .ToDictionaryAsync(u => u.UserId, u => u);

                // 5. Null-Safety for Invoice Rendering
                // Ensure that even if profiles/stores are missing, the invoice doesn't crash
                if (buyerProfile == null)
                {
                    buyerProfile = new UserProfile
                    {
                        UserId = order.UserId,
                        FirstName = "Guest",
                        LastName = "User"
                    };
                }

                // 6. Map to the ViewModel
                var viewModel = new OrderDetailsViewModel
                {
                    Order = order,
                    OrderDetails = order.OrderDetails?.ToList() ?? new List<OrderDetail>(),
                    UserProfile = buyerProfile,
                    SellerProfiles = sellerProfiles
                };

                // 7. Return to the Invoice view
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating invoice for Order ID: {OrderId}", id);
                TempData["error"] = "An error occurred while generating the invoice.";
                return RedirectToAction("Index", "Orders");
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetChatContacts()
        {
            try
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser == null)
                {
                    return Json(new { success = false, message = "User not authenticated" });
                }

                var userId = currentUser.Id;
                var userName = currentUser.UserName;

                // Get user roles
                var roles = await _userManager.GetRolesAsync(currentUser);
                bool isSeller = roles.Contains("Subscriber") || roles.Contains("Admin");

                List<string> contactUserIds = new List<string>();

                if (isSeller)
                {
                    // For sellers: get customers who bought their products
                    var customerUserNames = await _context.OrderDetails
                        .Where(od => od.ProductOwner == userName && od.IsProcessed)
                        .Select(od => od.Customer)
                        .Distinct()
                        .ToListAsync();

                    // Convert usernames to userIds
                    foreach (var customerName in customerUserNames)
                    {
                        var customerUser = await _userManager.FindByNameAsync(customerName);
                        if (customerUser != null)
                        {
                            contactUserIds.Add(customerUser.Id);
                        }
                    }
                }
                else
                {
                    // For buyers: get product owners from their orders
                    var productOwnerUserNames = await _context.Orders
                        .Where(o => o.UserId == userId)
                        .Join(_context.OrderDetails,
                              o => o.Id,
                              od => od.OrderId,
                              (o, od) => od.ProductOwner)
                        .Distinct()
                        .ToListAsync();

                    // Convert usernames to userIds
                    foreach (var ownerName in productOwnerUserNames)
                    {
                        if (!string.IsNullOrEmpty(ownerName))
                        {
                            var ownerUser = await _userManager.FindByNameAsync(ownerName);
                            if (ownerUser != null)
                            {
                                contactUserIds.Add(ownerUser.Id);
                            }
                        }
                    }
                }

                // Remove current user from contacts if present
                contactUserIds = contactUserIds.Where(id => id != userId).Distinct().ToList();

                // Get user profiles and online status
                var contacts = new List<object>();
                foreach (var contactId in contactUserIds)
                {
                    var contactUser = await _userManager.FindByIdAsync(contactId);
                    if (contactUser != null)
                    {
                        var profile = await _context.UserProfiles
                            .FirstOrDefaultAsync(p => p.UserId == contactId);

                        var status = await _context.UserStatuses
                            .FirstOrDefaultAsync(s => s.UserId == contactId);

                        contacts.Add(new
                        {
                            id = contactId,
                            name = profile != null ? $"{profile.FirstName} {profile.LastName}".Trim() : contactUser.UserName,
                            userName = contactUser.UserName,
                            isOnline = status?.IsOnline ?? false,
                            lastActivity = status?.LastActivity
                        });
                    }
                }

                return Json(new { success = true, contacts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chat contacts for user {UserId}", _userManager.GetUserId(User));
                return Json(new { success = false, message = "Error loading contacts" });
            }
        }

    }
}
