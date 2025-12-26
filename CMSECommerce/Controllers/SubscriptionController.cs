using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;

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
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            request.Status = RequestStatus.Approved;

            if (!await _userManager.IsInRoleAsync(user, "Subscriber"))
            {
                await _userManager.AddToRoleAsync(user, "Subscriber");
            }

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile != null)
            {
                DateTime currentEnd = (profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate.Value > DateTime.Now)
                                      ? profile.SubscriptionEndDate.Value
                                      : DateTime.Now;

                // Set the start of THIS specific period
                profile.SubscriptionStartDate = currentEnd;

                // Stack the months
                DateTime newEnd = currentEnd.AddMonths(request.Tier.DurationMonths);
                profile.SubscriptionEndDate = newEnd;

                profile.CurrentProductLimit = request.Tier.ProductLimit;

                _context.UserProfiles.Update(profile);
            }
            else
            {
                return BadRequest("User profile missing.");
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = $"Approved! New expiry: {profile.SubscriptionEndDate?.ToString("dd MMM yyyy")}";

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

        // Add this to your SubscriptionController.cs
        [HttpGet]
        public async Task<IActionResult> GetLatestStatuses()
        {
            var userId = _userManager.GetUserId(User);
            var statuses = await _context.SubscriptionRequests
                .Where(r => r.UserId == userId)
                .Select(r => new { id = r.Id, status = r.Status.ToString() })
                .ToListAsync();

            return Json(statuses);
        }

        [Authorize]
        public async Task<IActionResult> DownloadReceipt(int requestId)
        {
            var userId = _userManager.GetUserId(User);

            // Ensure the request belongs to the user and is actually approved
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId && r.Status == RequestStatus.Approved);

            if (request == null)
            {
                return NotFound("Receipt not found or request not yet approved.");
            }

            // Create the receipt content
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("============================================");
            sb.AppendLine("           SUBSCRIPTION RECEIPT             ");
            sb.AppendLine("============================================");
            sb.AppendLine($"Receipt ID:    {request.Id}");
            sb.AppendLine($"Date Issued:   {DateTime.Now:dd MMM yyyy}");
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine($"User ITS:      {request.ItsNumber}");
            sb.AppendLine($"Plan Name:     {request.Tier.Name}");
            sb.AppendLine($"Duration:      {request.Tier.DurationMonths} Month(s)");
            sb.AppendLine($"Product Limit: {request.Tier.ProductLimit}");
            sb.AppendLine("--------------------------------------------");
            sb.AppendLine("Status:        PAID & APPROVED");
            sb.AppendLine("============================================");
            sb.AppendLine("Thank you for your business!");

            var fileName = $"Receipt_{request.ItsNumber}_{request.Id}.txt";
            var fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

            return File(fileBytes, "text/plain", fileName);
        }
    }
}

