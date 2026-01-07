using CMSECommerce.Areas.Admin.Models;
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
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly DataContext _context;

        public AccountController(
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<IdentityUser> signInManager,
            DataContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _context = context;
        }


        #region User Registration
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var user = new IdentityUser
                {
                    UserName = model.Email,
                    PhoneNumber = model.WhatsAppNumber,
                    Email = model.Email,
                    PhoneNumberConfirmed = true,
                    NormalizedUserName = model.Email.ToUpper(),
                    EmailConfirmed = true
                };
                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");

                    // 1. Map Store Fields
                    var store = new Store
                    {
                        UserId = user.Id,
                        StoreName = model.StoreName,
                        GSTIN = model.GSTIN,
                        Email = model.Email,
                        Contact = model.WhatsAppNumber,
                        StreetAddress = model.StreetAddress,
                        City = model.City,
                        PostCode = model.PostCode,
                        Country = model.Country
                    };
                    _context.Stores.Add(store);
                    await _context.SaveChangesAsync();

                    // 2. Map All UserProfile Fields
                    var profile = new UserProfile
                    {
                        UserId = user.Id,
                        StoreId = store.Id,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        ITSNumber = model.ITSNumber,
                        WhatsAppNumber = model.WhatsAppNumber,
                        Profession = model.Profession,
                        ServicesProvided = model.ServicesProvided,
                        About = model.About,
                        FacebookUrl = model.FacebookUrl,
                        LinkedInUrl = model.LinkedInUrl,
                        InstagramUrl = model.InstagramUrl,
                        HomeAddress = model.HomeAddress,
                        HomePhoneNumber = model.HomePhoneNumber,
                        BusinessAddress = model.BusinessAddress,
                        BusinessPhoneNumber = model.BusinessPhoneNumber,

                        // Defaults for new users
                        IsProfileVisible = true,
                        CurrentProductLimit = 0,
                        CurrentTierId = null
                    };

                    _context.UserProfiles.Add(profile);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    TempData["Success"] = "Account created successfully! Welcome to your dashboard.";

                    // Redirect to Dashboard Controller, Index Action, in the Admin Area
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }
                foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Registration failed. Check if ITS Number is unique.");
            }
            return View(model);
        }
        #endregion

        #region Attributes Validations
        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> CheckEmail(string email, string? id)
        {
            var user = await _userManager.FindByEmailAsync(email);
            // Valid if: No user found OR the user found is the one we are currently editing
            bool isValid = (user == null || user.Id == id);
            return Json(isValid ? true : $"Email {email} is already in use.");
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> CheckITSNumber(string itsNumber, string? id)
        {
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.ITSNumber == itsNumber);
            bool isValid = (profile == null || profile.UserId == id);
            return Json(isValid ? true : "ITS Number is already registered to another account.");
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> CheckStoreName(string storeName, string? id)
        {
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.StoreName == storeName);
            bool isValid = (store == null || store.UserId == id);
            return Json(isValid ? true : "This store name is already taken by another merchant.");
        }

        [AcceptVerbs("Get", "Post")]
        public async Task<IActionResult> CheckWhatsApp(string whatsappNumber, string? id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == whatsappNumber);
            bool isValid = (user == null || user.Id == id);
            return Json(isValid ? true : "This phone number is already linked to another account.");
        }
        #endregion

        #region Edit User

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            // 1. Retrieve the three core entities
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == id);

            // 2. Map to ViewModel (The "Hydration" phase)
            var model = new RegisterUserViewModel
            {
                // CRITICAL: Set the Id so Remote Validation knows who the current user is
                Id = user.Id,

                Email = user.Email,
                FirstName = profile?.FirstName,
                LastName = profile?.LastName,
                ITSNumber = profile?.ITSNumber,
                WhatsAppNumber = profile?.WhatsAppNumber,
                Profession = profile?.Profession,
                ServicesProvided = profile?.ServicesProvided,
                About = profile?.About,
                FacebookUrl = profile?.FacebookUrl,
                LinkedInUrl = profile?.LinkedInUrl,
                InstagramUrl = profile?.InstagramUrl,
                HomeAddress = profile?.HomeAddress,
                HomePhoneNumber = profile?.HomePhoneNumber,
                BusinessAddress = profile?.BusinessAddress,
                BusinessPhoneNumber = profile?.BusinessPhoneNumber,
                StoreName = store?.StoreName,
                GSTIN = store?.GSTIN,
                StreetAddress = store?.StreetAddress,
                City = store?.City,
                PostCode = store?.PostCode,
                Country = store?.Country
            };

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(RegisterUserViewModel model)
        {
            // 1. Password is not required for a profile update
            ModelState.Remove("Password");
            ModelState.Remove("ConfirmPassword");

            if (!ModelState.IsValid) return View(model);

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 2. Fetch existing records using the ID from the model
                var user = await _userManager.FindByIdAsync(model.Id);
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == model.Id);
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == model.Id);

                if (user == null || profile == null || store == null) return NotFound();

                // 3. Update Identity User (Account Level)
                user.Email = model.Email;
                user.UserName = model.Email;
                user.PhoneNumber = model.WhatsAppNumber;
                var identityResult = await _userManager.UpdateAsync(user);

                if (!identityResult.Succeeded)
                {
                    foreach (var error in identityResult.Errors) ModelState.AddModelError("", error.Description);
                    return View(model);
                }

                // 4. Update Store (Business Level)
                store.StoreName = model.StoreName;
                store.GSTIN = model.GSTIN;
                store.Contact = model.WhatsAppNumber;
                store.StreetAddress = model.StreetAddress;
                store.City = model.City;
                store.PostCode = model.PostCode;
                store.Country = model.Country;
                _context.Stores.Update(store);

                // 5. Update Profile (Personal Level)
                profile.FirstName = model.FirstName;
                profile.LastName = model.LastName;
                profile.ITSNumber = model.ITSNumber;
                profile.WhatsAppNumber = model.WhatsAppNumber;
                profile.Profession = model.Profession;
                profile.ServicesProvided = model.ServicesProvided;
                profile.About = model.About;
                profile.FacebookUrl = model.FacebookUrl;
                profile.LinkedInUrl = model.LinkedInUrl;
                profile.InstagramUrl = model.InstagramUrl;
                profile.HomeAddress = model.HomeAddress;
                profile.HomePhoneNumber = model.HomePhoneNumber;
                profile.BusinessAddress = model.BusinessAddress;
                profile.BusinessPhoneNumber = model.BusinessPhoneNumber;
                _context.UserProfiles.Update(profile);

                // 6. Persist changes and commit transaction
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["Success"] = "Merchant profile updated successfully.";
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError("", "Update failed. Potential database constraint violation.");
                return View(model);
            }
        }

        #endregion

        #region Delete User

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateStore(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1. Fetch the Store associated with the user
                var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == id);
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);

                if (store == null || profile == null) return NotFound();

                // 2. Perform Soft Delete (Deactivation)
                store.IsActive = false;
                profile.IsDeactivated = true;

                _context.Stores.Update(store);
                _context.UserProfiles.Update(profile);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 3. Informative Feedback
                TempData["Success"] = "Your store has been deactivated and hidden from the public. You can reactivate it at any time.";

                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                TempData["Error"] = "Deactivation failed. Please try again or contact support.";
                return RedirectToAction("Edit", new { id = id });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateStore(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == id);
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);

            if (store == null || profile == null) return NotFound();

            // Reset status flags
            store.IsActive = true;
            profile.IsDeactivated = false;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Welcome back! Your store is now live and visible to customers.";
            return RedirectToAction("Index", "Dashboard");
        }

        #endregion
    }
}
