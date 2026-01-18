using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using CMSECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.CodeDom;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Subscriber,Admin,SuperAdmin")]
    public class SettingsController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SettingsController(DataContext context, UserManager<IdentityUser> userManager, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.UserProfiles
                .Include(up => up.Store)
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }

            return View(userProfile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(UserProfile model, IFormFile ProfileImageFile)
        {
            // Remove ITSNumber from validation so ModelState.IsValid passes
            ModelState.Remove("ITSNumber");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            var userProfile = await _context.UserProfiles
                .Include(up => up.Store)
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (userProfile == null)
            {
                return NotFound("User profile not found.");
            }

            // Handle Profile Image Upload
            if (ProfileImageFile != null && ProfileImageFile.Length > 0)
            {
                string subPath = Path.Combine("images", "useruploads");
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, subPath);

                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(ProfileImageFile.FileName);
                string filePath = Path.Combine(uploadFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImageFile.CopyToAsync(fileStream);
                }

                userProfile.ProfileImagePath = "/images/useruploads/" + fileName;
            }

            // Update UserProfile fields
            userProfile.FirstName = model.FirstName;
            userProfile.LastName = model.LastName;
            userProfile.Profession = model.Profession;
            userProfile.ServicesProvided = model.ServicesProvided;
            userProfile.About = model.About;
            userProfile.WhatsAppNumber = model.WhatsAppNumber;
            userProfile.HomeAddress = model.HomeAddress;
            userProfile.HomePhoneNumber = model.HomePhoneNumber;
            userProfile.BusinessAddress = model.BusinessAddress;
            userProfile.BusinessPhoneNumber = model.BusinessPhoneNumber;
            userProfile.FacebookUrl = model.FacebookUrl;
            userProfile.LinkedInUrl = model.LinkedInUrl;
            userProfile.InstagramUrl = model.InstagramUrl;
            userProfile.IsProfileVisible = model.IsProfileVisible;

            // Update Store fields if exists
            if (userProfile.Store != null)
            {
                userProfile.Store.StoreName = model.Store?.StoreName ?? userProfile.Store.StoreName;
                userProfile.Store.Email = model.Store?.Email ?? userProfile.Store.Email;
                userProfile.Store.Contact = model.Store?.Contact ?? userProfile.Store.Contact;
                userProfile.Store.StreetAddress = model.Store?.StreetAddress ?? userProfile.Store.StreetAddress;
                userProfile.Store.City = model.Store?.City ?? userProfile.Store.City;
                userProfile.Store.PostCode = model.Store?.PostCode ?? userProfile.Store.PostCode;
                userProfile.Store.Country = model.Store?.Country ?? userProfile.Store.Country;
                userProfile.Store.GSTIN = model.Store?.GSTIN ?? userProfile.Store.GSTIN;
            }

            try
            {
                await _context.SaveChangesAsync();
                TempData["Success"] = "Settings updated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while updating settings.";
                // Log the exception
            }

            return RedirectToAction("Index", "Dashboard", new { area = "Seller" });
        }
    }
}
