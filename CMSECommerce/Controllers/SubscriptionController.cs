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

            // 2. Fetch the Identity User
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            // 3. Update Request Status
            request.Status = RequestStatus.Approved;

            // 4. Update Role to Subscriber
            if (!await _userManager.IsInRoleAsync(user, "Subscriber"))
            {
                await _userManager.AddToRoleAsync(user, "Subscriber");
            }

            // 5. Fetch and Update UserProfile
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile != null)
            {
                // Set Subscription Dates
                profile.SubscriptionStartDate = DateTime.Now;

                // Calculate End Date based on Tier Duration (assuming DurationMonths exists in Tier model)
                profile.SubscriptionEndDate = DateTime.Now.AddMonths(request.Tier.DurationMonths);

                // Update Product Limit from the Tier
                profile.CurrentProductLimit = request.Tier.ProductLimit;

                // Ensure the profile is tracked for update
                _context.UserProfiles.Update(profile);
            }
            else
            {
                // Fallback: If for some reason a profile doesn't exist, you might want to log this 
                // or create a basic one to prevent the subscription from being "lost".
                return BadRequest("User profile missing. Cannot update subscription limits.");
            }

            // 6. Final Save (Updates Request and Profile in one transaction)
            await _context.SaveChangesAsync();

            TempData["Success"] = $"User {user.UserName} is now a Subscriber. Limit: {request.Tier.ProductLimit} products.";

            return RedirectToAction(nameof(AdminDashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revert(int requestId)
        {
            // 1. Fetch the request
            var request = await _context.SubscriptionRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            // 2. Fetch the User
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User associated with this request not found.");

            // 3. Update Request Status back to Pending
            request.Status = RequestStatus.Pending;

            // 4. Remove Role (Check if they are in the role first to avoid errors)
            if (await _userManager.IsInRoleAsync(user, "Subscriber"))
            {
                var roleResult = await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                if (!roleResult.Succeeded)
                {
                    return BadRequest("Failed to remove Subscriber role.");
                }
            }

            // 5. Fetch and Reset UserProfile
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                profile.CurrentProductLimit = 0; // Reset to default/zero
                profile.SubscriptionStartDate = null;
                profile.SubscriptionEndDate = null;

                // Explicitly mark the profile as modified
                _context.UserProfiles.Update(profile);
            }

            // 6. Final Save
            await _context.SaveChangesAsync();

            TempData["Warning"] = $"Subscription for {user.UserName} has been reverted to Pending.";
            return RedirectToAction(nameof(AdminDashboard));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int requestId, string reason)
        {
            // 1. Fetch the request
            var request = await _context.SubscriptionRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            // 2. Fetch the User
            var user = await _userManager.FindByIdAsync(request.UserId);

            // 3. Update Request Status and Reason
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = reason;

            if (user != null)
            {
                // 4. Strip Subscriber Role (Safety check in case they were previously approved)
                if (await _userManager.IsInRoleAsync(user, "Subscriber"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                }

                // 5. Reset Profile Limits and Dates
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    profile.CurrentProductLimit = 0;
                    profile.SubscriptionStartDate = null;
                    profile.SubscriptionEndDate = null;

                    _context.UserProfiles.Update(profile);
                }
            }

            // 6. Final Save
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Request for {user?.UserName ?? "User"} has been Rejected.";
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

