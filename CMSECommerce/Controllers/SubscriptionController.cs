using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Controllers
{
    
    [Authorize]
    public class SubscriptionController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        public SubscriptionController(DataContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 1. Tier Selection Page
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var tiers = await _context.SubscriptionTiers.ToListAsync();
            return View(tiers);
        }

        // 2. Registration Page (AC 2)
        public async Task<IActionResult> Register(int tierId)
        {
            var user = await _userManager.GetUserAsync(User);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile == null || string.IsNullOrEmpty(userProfile.ITSNumber))
            {
                TempData["Error"] = "Please update your ITS number in your profile first.";
                return RedirectToAction("EditProfile", "Account");
            }

            var tier = await _context.SubscriptionTiers.FindAsync(tierId);
            if (tier == null) return NotFound();

            var model = new SubscriptionRequestViewModel
            {
                TierId = tierId,
                TierName = tier.Name,
                Price = tier.Price,
                ITSNumber = userProfile.ITSNumber
            };
            return View(model);
        }

        // 3. Form Submission
        [HttpPost]
        public async Task<IActionResult> SubmitRequest(SubscriptionRequestViewModel model)
        {
            if (!ModelState.IsValid) return View("Register", model);

            string fileName = "default.png";
            if (model.Receipt != null)
            {
                fileName = Guid.NewGuid() + Path.GetExtension(model.Receipt.FileName);
                string path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", fileName);
                using var stream = new FileStream(path, FileMode.Create);
                await model.Receipt.CopyToAsync(stream);
            }

            var request = new SubscriptionRequest
            {
                UserId = _userManager.GetUserId(User)!,
                TierId = model.TierId,
                ItsNumber = model.ITSNumber,
                ReceiptImagePath = fileName
            };

            _context.SubscriptionRequests.Add(request);
            await _context.SaveChangesAsync();
            return RedirectToAction("Status");
        }

        // 4. Admin Dashboard (AC 3)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var requests = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
            return View(requests);
        }

        // 5. Approval Logic (AC 4)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int requestId)
        {
            // 1. Fetch the request with Tier details
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);


            
            if (request == null) return NotFound();

            request.Status = RequestStatus.Approved;
            var user = await _userManager.FindByIdAsync(request.UserId);

            // 4. Update Role to Subscriber/Seller
            if (!await _userManager.IsInRoleAsync(user, "Subscriber"))
            {
                await _userManager.AddToRoleAsync(user, "Subscriber");
            }

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            
            // 5. SET SUBSCRIPTION DATES & LIMITS
            // Subscription starts exactly now (Date of Approval)
            profile.SubscriptionStartDate = DateTime.Now;
                        

            await _userManager.AddToRoleAsync(user, "Subscriber");
            profile.CurrentProductLimit = request.Tier.ProductLimit;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminDashboard));
        }


        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revert(int requestId)
        {
            var request = await _context.SubscriptionRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.Status = RequestStatus.Pending;

            var user = await _userManager.FindByIdAsync(request.UserId);
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (user != null) await _userManager.RemoveFromRoleAsync(user, "Subscriber");

            if (profile != null)
            {
                profile.CurrentProductLimit = 0;
                profile.SubscriptionStartDate = null; // Clear dates
                profile.SubscriptionEndDate = null;   // Clear dates
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminDashboard));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int requestId, string reason)
        {
            var request = await _context.SubscriptionRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.Status = RequestStatus.Rejected;
            request.RejectionReason = reason;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(AdminDashboard));
        }

        public async Task<IActionResult> Status()
        {
            var userId = _userManager.GetUserId(User);

            // Pass the profile to ViewBag so the View can access SubscriptionEndDate
            ViewBag.UserProfile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            var requests = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(requests);
        }
    }
}

