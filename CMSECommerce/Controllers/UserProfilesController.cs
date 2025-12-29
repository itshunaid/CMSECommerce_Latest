using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
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
        public IActionResult Index()
        {
            return View();
        }

        // GET: UserProfiles/Create
        public async Task<IActionResult> Create(bool isNewProfile)
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
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserProfile profile, IFormFile profileImg, IFormFile gpayQR, IFormFile phonepeQR)
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

       
    }
}
