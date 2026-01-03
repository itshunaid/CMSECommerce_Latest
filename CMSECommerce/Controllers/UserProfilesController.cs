using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Controllers
{
    public class UserProfilesController : Controller
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
        public async Task<IActionResult> Index(string userId=null)
        {
            bool requestFromDictionary = false;
            if (string.IsNullOrEmpty(userId))
            {
                // Get the current logged-in user's ID
                userId = _userManager.GetUserId(User);
            }
            else
            {
                requestFromDictionary = true;
            }

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            // Fetch profile including the Store details
            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                // If no profile exists, send them to the create page
                return RedirectToAction("Create", new { isNewProfile = true });
            }

            ViewBag.RequestFromDictionary = requestFromDictionary;

            return View(profile);
        }

        // GET: UserProfiles/View/5
        // This allows a public link like /UserProfiles/View/123 to be shared with customers
        [AllowAnonymous]
        public async Task<IActionResult> View(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(m => m.Id == id);

            // Only show if the profile exists and the user has set it to visible
            if (profile == null || !profile.IsProfileVisible)
            {
                return NotFound();
            }

            return View("Index", profile);
        }

        // GET: UserProfiles/Create
        public async Task<IActionResult> Create(bool isNewProfile, string callingFrom="", int tierId=0)
        {
            var userId = _userManager.GetUserId(User);
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p=> p.UserId==userId);
            if(!isNewProfile || profile!=null)
            {
                return RedirectToAction("Index", "Products");
            }
            // Set defaults for a new profile
            var model = new UserProfile
            {
                UserId = _userManager.GetUserId(User),
                IsProfileVisible = true,
                CurrentProductLimit = 0 // Default limit
            };
            if(callingFrom=="UserProfiles" && tierId>0)
            {
                ViewBag.CallingFrom = "UserProfiles";
                ViewBag.TierId = tierId;
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserProfile profile, IFormFile profileImg, IFormFile gpayQR, IFormFile phonepeQR, string callingFrom="", int tierId=0)
        {
            // 1. Get the current User Object (needed for Email/Phone)
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); // Ensure user is logged in

            if (!ModelState.IsValid)
            {
                return View(profile);
            }

            try
            {
                string subPath = Path.Combine("images", "useruploads");
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, subPath);

                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                // 1. Profile Image
                if (profileImg != null)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(profileImg.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await profileImg.CopyToAsync(fileStream);
                    }
                    // SAVE THIS TO DB: The Web URL, not the physical path
                    profile.ProfileImagePath = "/images/useruploads/" + fileName;
                }

                // 2. GPay QR
                if (gpayQR != null)
                {
                    string fileName = "gpay_" + Guid.NewGuid().ToString() + "_" + Path.GetFileName(gpayQR.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await gpayQR.CopyToAsync(fileStream);
                    }
                    profile.GpayQRCodePath = "/images/useruploads/" + fileName;
                }

                // 3. PhonePe QR
                if (phonepeQR != null)
                {
                    string fileName = "phonepe_" + Guid.NewGuid().ToString() + "_" + Path.GetFileName(phonepeQR.FileName);
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await phonepeQR.CopyToAsync(fileStream);
                    }
                    profile.PhonePeQRCodePath = "/images/useruploads/" + fileName;
                }

                

                // 3. Set Default Logic
                profile.UserId = user.Id;
                profile.IsImagePending = profileImg != null;
                profile.IsImageApproved = false;
                profile.SubscriptionStartDate = null;
                var newStore = new Store
                {
                    UserId = user.Id, // Ensure Store is also linked to the User
                    StoreName = $"{profile.FirstName}'s Store",
                    StreetAddress = profile.BusinessAddress ?? "Pending Update",
                    City = "Pending",
                    PostCode = "Pending",
                    Country = "Pending",
                    Email = user.Email,        // Use the 'user' object we fetched
                    Contact = user.PhoneNumber  // Use the 'user' object we fetched
                };

                _context.Stores.Add(newStore);
                await _context.SaveChangesAsync(); // Generates the newStore.Id

                profile.StoreId = newStore.Id;
                
                _context.Add(profile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Your profile and store have been created successfully.";
                if(callingFrom=="UserProfiles" && tierId>0)
                {                   

                    return RedirectToAction("Register", "Subscription", new { tierId = tierId });
                }
               
                return RedirectToAction("Index", "Cart");
            }
            catch (IOException)
            {
                ModelState.AddModelError("", "Error saving images. Please check file permissions.");
            }
            catch (DbUpdateException ex)
            {
                // Log the inner exception for better debugging
                ModelState.AddModelError("", "Database error: Ensure you don't already have a profile.");
            }
            catch (Exception)
            {
                ModelState.AddModelError("", "An unexpected error occurred.");
            }

            return View(profile);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = _userManager.GetUserId(User);

            // Use .Include(p => p.Store) to load the related store data
            var profile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return RedirectToAction("Create", new { isNewProfile = true });
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserProfile model, IFormFile? profileImg, IFormFile? gpayQR, IFormFile? phonepeQR)
        {
            var userId = _userManager.GetUserId(User);
            var existingProfile = await _context.UserProfiles
                .Include(p => p.Store)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (existingProfile == null) return NotFound();

            // Remove Validation for fields handled manually or not present in the form
            ModelState.Remove("UserId");
            ModelState.Remove("User");

            if (!ModelState.IsValid) return View(model);

            try
            {
                string subPath = Path.Combine("images", "useruploads");
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, subPath);
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                async Task<string?> UpdateFile(IFormFile? newFile, string? oldPath, string prefix)
                {
                    if (newFile == null) return oldPath;
                    if (!string.IsNullOrEmpty(oldPath))
                    {
                        var oldFilePath = Path.Combine(_webHostEnvironment.WebRootPath, oldPath.TrimStart('/'));
                        if (System.IO.File.Exists(oldFilePath)) System.IO.File.Delete(oldFilePath);
                    }
                    string fileName = $"{prefix}_{Guid.NewGuid()}_{Path.GetFileName(newFile.FileName)}";
                    string filePath = Path.Combine(uploadFolder, fileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await newFile.CopyToAsync(fileStream);
                    }
                    return "/images/useruploads/" + fileName;
                }

                // 1. Update Media Paths & Logic
                if (profileImg != null)
                {
                    existingProfile.ProfileImagePath = await UpdateFile(profileImg, existingProfile.ProfileImagePath, "profile");
                    existingProfile.IsImagePending = true;
                    existingProfile.IsImageApproved = false;
                }
                existingProfile.GpayQRCodePath = await UpdateFile(gpayQR, existingProfile.GpayQRCodePath, "gpay");
                existingProfile.PhonePeQRCodePath = await UpdateFile(phonepeQR, existingProfile.PhonePeQRCodePath, "phonepe");

                // 2. Map Basic Info
                existingProfile.FirstName = model.FirstName;
                existingProfile.LastName = model.LastName;
                existingProfile.Profession = model.Profession;
                existingProfile.ServicesProvided = model.ServicesProvided;
                existingProfile.About = model.About;
                existingProfile.ITSNumber = model.ITSNumber;
                existingProfile.WhatsAppNumber = model.WhatsAppNumber;

                // 3. Map Addresses & Contact
                existingProfile.HomeAddress = model.HomeAddress;
                existingProfile.HomePhoneNumber = model.HomePhoneNumber;
                existingProfile.BusinessAddress = model.BusinessAddress;
                existingProfile.BusinessPhoneNumber = model.BusinessPhoneNumber;

                // 4. Map Social Media
                existingProfile.FacebookUrl = model.FacebookUrl;
                existingProfile.LinkedInUrl = model.LinkedInUrl;
                existingProfile.InstagramUrl = model.InstagramUrl;
                existingProfile.IsProfileVisible = model.IsProfileVisible;

                // 5. Update Related Store Info (Optional: if you want to sync these)
                if (existingProfile.Store != null)
                {
                    existingProfile.Store.GSTIN = model.Store?.GSTIN;
                    existingProfile.Store.StoreName = model.Store?.StoreName ?? $"{model.FirstName}'s Store";
                    // Sync business address to store if needed
                    existingProfile.Store.StreetAddress = model.BusinessAddress;
                }

                _context.Update(existingProfile);
                await _context.SaveChangesAsync();

                TempData["Success"] = "Profile and Store settings updated successfully.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "An unexpected error occurred: " + ex.Message);
                return View(model);
            }
        }
    }
}
