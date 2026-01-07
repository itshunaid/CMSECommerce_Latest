using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace CMSECommerce.Controllers
{
    [Authorize]
    public class UserProfilesController : BaseController
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UserProfilesController(DataContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: UserProfiles/Index
        public async Task<IActionResult> Index(string userId = null)
        {
            var isCustomer = User.IsInRole("Customer");
            ViewBag.IsCustomer = isCustomer;
            bool requestFromDictionary = false;
            if (string.IsNullOrEmpty(userId))
            {
                userId = _userManager.GetUserId(User);
            }
            else
            {
                requestFromDictionary = true;
            }


            if (string.IsNullOrEmpty(userId)) return Challenge();            

            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return RedirectToAction("Create", new { isNewProfile = true });
            }

            ViewBag.RequestFromDictionary = requestFromDictionary;
            return View(profile);
        }

        // GET: UserProfiles/View/5 (Public)
        [AllowAnonymous]
        public async Task<IActionResult> View(int? id)
        {
            if (id == null) return NotFound();

            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (profile == null || !profile.IsProfileVisible)
            {
                return NotFound();
            }

            return View(profile);
        }

        // GET: UserProfiles/Create
        public async Task<IActionResult> Create(bool isNewProfile, string callingFrom = "", int tierId = 0)
        {
            var userId = _userManager.GetUserId(User);
            

            var existingProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile != null)
            {
                return RedirectToAction("Index");
            }

            if(User.IsInRole("Customer"))
            {

                ViewBag.IsCustomer = true;
            }


            // Pre-fill defaults for the view to satisfy Migration Requirements
            var model = new UserProfile
            {
                UserId = userId,
                IsProfileVisible = true,
                CurrentProductLimit = 0,
                About = "Default about info",
                Profession = "Merchant",
                HomeAddress = "Update Required",
                BusinessAddress = "Update Required"
            };

            ViewBag.CallingFrom = callingFrom;
            ViewBag.TierId = tierId;
            return View(model);
        }

        // POST: UserProfiles/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserProfile profile, IFormFile? profileImg, IFormFile? gpayQR, IFormFile? phonepeQR, string callingFrom = "", int tierId = 0)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Clear non-form validation logic
            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Store");

            if (!ModelState.IsValid) return View(profile);

            try
            {
                // Handle File Uploads
                profile.ProfileImagePath = await ProcessUpload(profileImg, "profile");
                profile.GpayQRCodePath = await ProcessUpload(gpayQR, "gpay");
                profile.PhonePeQRCodePath = await ProcessUpload(phonepeQR, "phonepe");

                profile.UserId = user.Id;
                profile.IsImagePending = profileImg != null;
                profile.IsImageApproved = false;

                // 1. Create the associated Store automatically
                var newStore = new Store
                {
                    UserId = user.Id,
                    StoreName = $"{profile.FirstName}'s Store",
                    StreetAddress = profile.BusinessAddress ?? "Pending Address",
                    Email = user.Email,
                    Contact = user.PhoneNumber ?? profile.WhatsAppNumber,
                    City = "Update City",
                    Country = "Update Country"
                };

                _context.Stores.Add(newStore);
                await _context.SaveChangesAsync();

                // 2. Link Store to Profile and Save Profile
                profile.StoreId = newStore.Id;
                Random generator = new Random();
                // Generates a number between 10,000,000 and 99,999,999
                int randomNumber = generator.Next(10000000, 100000000);
                profile.ITSNumber = randomNumber.ToString();
                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Profile created successfully.";

                if (callingFrom == "UserProfiles" && tierId > 0)
                {
                    return RedirectToAction("Register", "Subscription", new { tierId = tierId });
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Creation failed: " + ex.Message);
                return View(profile);
            }
        }

        // GET: UserProfiles/Edit
        public async Task<IActionResult> Edit()
        {

            var isCustomer = User.IsInRole("Customer");
            ViewBag.IsCustomer = isCustomer;
            var userId = _userManager.GetUserId(User);
            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return RedirectToAction("Create", new { isNewProfile = true });

            return View(profile);
        }

        // POST: UserProfiles/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfile model, IFormFile? profileImg, IFormFile? gpayQR, IFormFile? phonepeQR)
        {
            var userId = _userManager.GetUserId(User);
            var existingProfile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile == null) return NotFound();

            ModelState.Remove("User");
            ModelState.Remove("UserId");
            ModelState.Remove("Store");

            if (!ModelState.IsValid) return View(model);

            try
            {
                // Update Files
                existingProfile.ProfileImagePath = await UpdateFile(profileImg, existingProfile.ProfileImagePath, "profile");
                existingProfile.GpayQRCodePath = await UpdateFile(gpayQR, existingProfile.GpayQRCodePath, "gpay");
                existingProfile.PhonePeQRCodePath = await UpdateFile(phonepeQR, existingProfile.PhonePeQRCodePath, "phonepe");

                // Update Fields
                existingProfile.FirstName = model.FirstName;
                existingProfile.LastName = model.LastName;
                existingProfile.Profession = model.Profession;
                existingProfile.About = model.About;
                existingProfile.ITSNumber = model.ITSNumber;
                existingProfile.WhatsAppNumber = model.WhatsAppNumber;
                existingProfile.HomeAddress = model.HomeAddress;
                existingProfile.BusinessAddress = model.BusinessAddress;
                existingProfile.IsProfileVisible = model.IsProfileVisible;

                // Sync Store details
                if (existingProfile.Store != null)
                {
                    existingProfile.Store.StoreName = model.Store?.StoreName ?? existingProfile.Store.StoreName;
                    existingProfile.Store.StreetAddress = model.BusinessAddress;
                    existingProfile.Store.Email = model.Store?.Email ?? existingProfile.Store.Email;
                    existingProfile.Store.PostCode = model.Store?.PostCode ?? existingProfile.Store.PostCode;
                    existingProfile.Store.City = model.Store?.City ?? existingProfile.Store.City;
                    existingProfile.Store.Country = model.Store?.Country ?? existingProfile.Store.Country;

                }

                _context.Update(existingProfile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Profile updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Update failed: " + ex.Message);
                return View(model);
            }
        }






        // Remote validation for uniqueness
        [HttpGet]
        public async Task<JsonResult> CheckUniqueness(string field, string value, int currentId)
        {
            bool isDuplicate = false;
            var currentProfile = await _context.UserProfiles.Select(p => new { p.Id, p.StoreId, p.UserId }).FirstOrDefaultAsync(p => p.Id == currentId);
            int? currentStoreId = currentProfile?.StoreId;
            string currentUserId = currentProfile?.UserId;

            switch (field)
            {
                case "WhatsAppNumber":
                    isDuplicate = await _context.UserProfiles.AnyAsync(p => p.Id != currentId && p.WhatsAppNumber == value);
                    break;
                case "StoreName":
                    isDuplicate = await _context.Stores.AnyAsync(s => s.Id != currentStoreId && s.StoreName == value);
                    break;
            }

            return Json(new { isDuplicate });
        }

        // --- HELPER METHODS ---

        private async Task<string> ProcessUpload(IFormFile? file, string prefix)
        {
            if (file == null) return null;
            string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "useruploads");
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string fileName = $"{prefix}_{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string path = Path.Combine(folder, fileName);
            using (var stream = new FileStream(path, FileMode.Create)) await file.CopyToAsync(stream);
            return "/images/useruploads/" + fileName;
        }

        private async Task<string> UpdateFile(IFormFile? newFile, string? oldPath, string prefix)
        {
            if (newFile == null) return oldPath;
            if (!string.IsNullOrEmpty(oldPath))
            {
                var oldFull = Path.Combine(_webHostEnvironment.WebRootPath, oldPath.TrimStart('/'));
                if (System.IO.File.Exists(oldFull)) System.IO.File.Delete(oldFull);
            }
            return await ProcessUpload(newFile, prefix);
        }
    }
}