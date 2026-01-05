using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.Metrics;
using System.Security.Claims;
using System.Text;

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
            var tiers = await _context.SubscriptionTiers.OrderBy(t => t.Id).ToListAsync();

            // Check if the user is logged in to provide personalized tier selection
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == userId);

                if (profile != null)
                {
                    // Pass the current tier ID to the view to handle UI logic
                    // (e.g., hiding/disabling tiers lower than the current one)
                    ViewBag.CurrentTierId = profile.CurrentTierId;

                    // Check if there is already a pending request
                    ViewBag.HasPendingRequest = await _context.SubscriptionRequests
                        .AnyAsync(r => r.UserId == userId && r.Status == RequestStatus.Pending);

                    // Check if current subscription is still active
                    ViewBag.IsActiveSubscriber = profile.SubscriptionEndDate.HasValue &&
                                                profile.SubscriptionEndDate.Value > DateTime.Now;
                }
            }

            return View(tiers);
        }

        // 2. Registration Page (AC 2)
        // 2. Registration Page (GET)
        [HttpGet]
        public async Task<IActionResult> Register(int tierId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            // 1. Profile & ITS Validation
            if (userProfile == null)
            {
                TempData["Error"] = "Please update your ITS number in your profile first.";
                return RedirectToAction("Create", "UserProfiles", new { isNewProfile = true, callingFrom = "UserProfiles", tierId = tierId });
            }

            if (string.IsNullOrEmpty(userProfile.ITSNumber))
            {
                TempData["Error"] = "Please update your ITS number in your profile first.";
                return RedirectToAction("Edit", "UserProfiles", new { id = userProfile.Id }); // Pass Id for route consistency
            }

            // 2. Fetch the Target Tier
            var targetTier = await _context.SubscriptionTiers.FindAsync(tierId);
            if (targetTier == null) return NotFound();

            // 3. Pending Request Shield
            var hasPending = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == user.Id && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                TempData["Error"] = "You already have a request pending review. Please wait for Admin approval.";
                return RedirectToAction("Status");
            }

            // 4. Upgrade/Downgrade Logic
            if (userProfile.CurrentTierId.HasValue)
            {
                // If they are selecting their current plan
                if (userProfile.CurrentTierId == tierId)
                {
                    TempData["Info"] = "You are currently on this plan. You can renew once your current term is near expiry.";
                    return RedirectToAction("Status");
                }

                // Rule: If the request is to downgrade, reject immediately
                // (Assumes higher TierId = Higher Level)
                if (tierId < userProfile.CurrentTierId.Value)
                {
                    TempData["Error"] = "Downgrading your plan is not supported through the portal.";
                    return RedirectToAction("Index");
                }
            }

            // 5. Build the ViewModel
            var model = new SubscriptionRequestViewModel
            {
                TierId = tierId,
                TierName = targetTier.Name,
                Price = targetTier.Price,
                ITSNumber = userProfile.ITSNumber,
                IsUpgrade = userProfile.CurrentTierId.HasValue // Useful for the View to show "Upgrade" text
            };

            return View(model);
        }

        // 3. Form Submission (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(SubscriptionRequestViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userId = user.Id;

            // 1. Fetch User Profile to check current tier for Upgrade/Downgrade validation
            var userProfile = await _context.UserProfiles
                .FirstOrDefaultAsync(up => up.UserId == userId);

            // 2. Final Gatekeeper: Block if a PENDING request already exists
            var hasPending = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == userId && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                TempData["Error"] = "Submission failed: You already have a request pending review.";
                return RedirectToAction("Status");
            }

            // 3. Business Rule: Upgrade vs Downgrade Check
            if (userProfile?.CurrentTierId != null)
            {
                // Prevent Downgrades (New TierId must be > Current TierId)
                if (model.TierId < userProfile.CurrentTierId.Value)
                {
                    TempData["Error"] = "Downgrades are not permitted. Please select a higher-tier plan.";
                    return RedirectToAction("Index");
                }

                // Prevent Duplicate Subscriptions for the same plan
                if (model.TierId == userProfile.CurrentTierId.Value)
                {
                    TempData["Info"] = "You are already subscribed to this plan.";
                    return RedirectToAction("Status");
                }
            }

            if (!ModelState.IsValid) return View("Register", model);

            // 4. Handle Receipt Upload
            string fileName = "default.png";
            if (model.Receipt != null)
            {
                // Ensure directory exists
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                fileName = Guid.NewGuid() + Path.GetExtension(model.Receipt.FileName);
                string path = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.Receipt.CopyToAsync(stream);
                }
            }

            // 5. Create Subscription Request
            var request = new SubscriptionRequest
            {
                UserId = userId!,
                TierId = model.TierId,
                ItsNumber = model.ITSNumber,
                ReceiptImagePath = fileName,
                CreatedAt = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                // Optional: You might want to flag this as an upgrade in your DB if you have a column for it
                IsUpgrade = userProfile?.CurrentTierId != null 
            };

            _context.SubscriptionRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = userProfile?.CurrentTierId != null
                ? "Your upgrade request has been submitted successfully."
                : "Your subscription request has been submitted for review.";

            return RedirectToAction("Status");
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var requests = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                // Join with UserProfile to see what they are currently on vs what they want
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // Optional: Calculate stats for the View
            ViewBag.PendingCount = requests.Count(r => r.Status == RequestStatus.Pending);
            ViewBag.UpgradeCount = requests.Count(r => r.IsUpgrade && r.Status == RequestStatus.Pending);

            return View(requests);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int requestId)
        {
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            // 1. Basic Constraint Checks
            if (request.Status == RequestStatus.Approved) return BadRequest("This request is already approved.");
            if (request.Status == RequestStatus.Rejected) return BadRequest("Cannot approve a rejected request.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return BadRequest("User profile missing.");

            // Safety check: Ensure profile hasn't been deactivated
            if (user.LockoutEnabled && await _userManager.IsLockedOutAsync(user))
                return BadRequest("User is currently deactivated/locked.");

            // 2. Identify Tier context
            int currentTierId = profile.CurrentTierId ?? 0;
            int requestedTierId = request.TierId;

            // 3. Downgrade Check
            if (requestedTierId < currentTierId && currentTierId != 0)
            {
                string reason = $"Downgrade not permitted. Active Tier: {currentTierId}, Requested: {requestedTierId}.";
                // Assuming Reject is a local method or logic to set status to Rejected
                return await Reject(requestId, reason);
            }

            // 4. Calculate Date Baseline (Stacking Logic)
            // If current subscription is still active, start the new duration from the end of the old one.
            DateTime baseline = (profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate.Value > DateTime.Now)
                                ? profile.SubscriptionEndDate.Value
                                : DateTime.Now;

            // 5. Upgrade or Renewal Logic
            // We update the limit and tier ID regardless if it's an upgrade or same-tier renewal
            profile.SubscriptionEndDate = baseline.AddMonths(request.Tier.DurationMonths);
            profile.CurrentProductLimit = request.Tier.ProductLimit;
            profile.CurrentTierId = requestedTierId;

            // 6. Handle New Subscription Start Date
            // If they never had a sub or it was expired, set the start date to today
            if (!profile.SubscriptionStartDate.HasValue || profile.SubscriptionEndDate.Value.AddMonths(-request.Tier.DurationMonths) < DateTime.Now)
            {
                profile.SubscriptionStartDate = DateTime.Now;
            }

            // 7. Role Management
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Subscriber"))
            {
                await _userManager.AddToRoleAsync(user, "Subscriber");
            }

            // Update Request Status and Save
            request.Status = RequestStatus.Approved;
            _context.UserProfiles.Update(profile);
            _context.SubscriptionRequests.Update(request);

            await _context.SaveChangesAsync();

            // Force user security refresh (re-logs them to update claims/roles)
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"Plan '{request.Tier.Name}' Approved! New expiry: {profile.SubscriptionEndDate?.ToString("dd MMM yyyy")}";
            return RedirectToAction(nameof(AdminDashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revert(int requestId)
        {
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            if (request.Status != RequestStatus.Approved)
                return BadRequest("Only approved requests can be reverted.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return BadRequest("Profile missing.");

            // 1. Subtract the specific months added by this request
            if (profile.SubscriptionEndDate.HasValue)
            {
                profile.SubscriptionEndDate = profile.SubscriptionEndDate.Value.AddMonths(-request.Tier.DurationMonths);

                // 2. Fetch the MOST RECENT Approved request excluding the one we are reverting
                var previousApprovedRequest = await _context.SubscriptionRequests
                    .Include(r => r.Tier)
                    .Where(r => r.UserId == user.Id &&
                                r.Status == RequestStatus.Approved &&
                                r.Id != requestId)
                    .OrderByDescending(r => r.Id)
                    .FirstOrDefaultAsync();

                // 3. Check if reverting leads to expiration
                if (profile.SubscriptionEndDate <= DateTime.Now)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains("Subscriber"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                    }

                    profile.CurrentProductLimit = 0;
                    profile.SubscriptionStartDate = null;
                    profile.SubscriptionEndDate = null;
                    profile.CurrentTierId = null;
                }
                else
                {
                    // Restore state to the tier they were on before this specific approval
                    if (previousApprovedRequest != null)
                    {
                        profile.CurrentProductLimit = previousApprovedRequest.Tier.ProductLimit;
                        profile.CurrentTierId = previousApprovedRequest.TierId;
                    }
                    else
                    {
                        profile.CurrentProductLimit = 0;
                        profile.CurrentTierId = null;
                    }
                }
            }

            // 4. Reset Request Status back to Pending
            request.Status = RequestStatus.Pending;

            _context.UserProfiles.Update(profile);
            _context.SubscriptionRequests.Update(request);
            await _context.SaveChangesAsync();
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Warning"] = "Approval Reverted. Limits and Tier have been reset to previous values.";
            return RedirectToAction(nameof(AdminDashboard));
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int requestId, string reason)
        {
            // 1. Fetch the request including the User reference for better error handling
            var request = await _context.SubscriptionRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            // 2. Fetch the User
            var user = await _userManager.FindByIdAsync(request.UserId);

            // 3. Update Request Status and Reason
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = reason;

            if (user != null)
            {
                // 4. Reset Profile State
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                if (profile != null)
                {
                    // Resetting everything to baseline values
                    profile.CurrentProductLimit = 0;
                    profile.SubscriptionStartDate = null;
                    profile.SubscriptionEndDate = null;

                    // NEW: Reset the Tier marker so they can request any tier (including Trial) again
                    profile.CurrentTierId = null;

                    _context.UserProfiles.Update(profile);
                }

                // 5. Role Management: Strip Subscriber Role
                // Rejection implies the user is no longer an active subscriber
                if (await _userManager.IsInRoleAsync(user, "Subscriber"))
                {
                    await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                }

                // 6. Security Stamp Update
                // Critical: This invalidates the user's current session so they immediately 
                // lose access to "Subscriber" authorized features.
                await _userManager.UpdateSecurityStampAsync(user);
            }

            // 7. Final Persistence
            _context.SubscriptionRequests.Update(request);
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Request for {user?.UserName ?? "User"} has been Rejected. Reason: {reason}";
            return RedirectToAction(nameof(AdminDashboard));
        }
        public async Task<IActionResult> Status()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 1. Fetch the profile including the CurrentTierId marker
            var profile = await _context.UserProfiles
                .Include(p => p.CurrentTier) // Optional: include if you want to show tier name
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // Pass the profile to ViewBag
            ViewBag.UserProfile = profile;

            // 2. Fetch all subscription requests for this user
            var requests = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 3. Optional architectural check: 
            // If profile.SubscriptionEndDate < DateTime.Now, you might want to 
            // flag in the UI that the current tier is expired despite what CurrentTierId says.
            ViewBag.IsExpired = profile?.SubscriptionEndDate < DateTime.Now;

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

        //[Authorize]
        //public async Task<IActionResult> DownloadReceipt(int requestId)
        //{
        //    var userId = _userManager.GetUserId(User);

        //    // Ensure the request belongs to the user and is actually approved
        //    var request = await _context.SubscriptionRequests
        //        .Include(r => r.Tier)
        //        .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId && r.Status == RequestStatus.Approved);

        //    if (request == null)
        //    {
        //        return NotFound("Receipt not found or request not yet approved.");
        //    }

        //    // Create the receipt content
        //    var sb = new System.Text.StringBuilder();
        //    sb.AppendLine("============================================");
        //    sb.AppendLine("           SUBSCRIPTION RECEIPT             ");
        //    sb.AppendLine("============================================");
        //    sb.AppendLine($"Receipt ID:    {request.Id}");
        //    sb.AppendLine($"Date Issued:   {DateTime.Now:dd MMM yyyy}");
        //    sb.AppendLine("--------------------------------------------");
        //    sb.AppendLine($"User ITS:      {request.ItsNumber}");
        //    sb.AppendLine($"Plan Name:     {request.Tier.Name}");
        //    sb.AppendLine($"Duration:      {request.Tier.DurationMonths} Month(s)");
        //    sb.AppendLine($"Product Limit: {request.Tier.ProductLimit}");
        //    sb.AppendLine("--------------------------------------------");
        //    sb.AppendLine("Status:        PAID & APPROVED");
        //    sb.AppendLine("============================================");
        //    sb.AppendLine("Thank you for your business!");

        //    var fileName = $"Receipt_{request.ItsNumber}_{request.Id}.txt";
        //    var fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());

        //    return File(fileBytes, "text/plain", fileName);
        //}

        [HttpGet]
        public async Task<IActionResult> DownloadReceipt(int requestId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Fetch the specific request and ensure it belongs to the logged-in user
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId && r.UserId == userId);

            if (request == null || request.Status != RequestStatus.Approved)
            {
                TempData["Error"] = "Receipt not found or request not approved.";
                return RedirectToAction(nameof(Status));
            }

            // Using Rotativa to return the view as a PDF
            return View("ReceiptPdf", request);
           
        }



        // --- CSV EXPORT ---
        public async Task<IActionResult> DownloadCsvHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var history = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var builder = new StringBuilder();
            builder.AppendLine("Date,Package,ITS Number,Duration,Status,Note");

            foreach (var r in history)
            {
                builder.AppendLine($"{r.CreatedAt:yyyy-MM-dd},{r.Tier.Name},{r.ItsNumber},{r.Tier.DurationMonths} Mo,{r.Status},\"{r.RejectionReason}\"");
            }

            return File(Encoding.UTF8.GetBytes(builder.ToString()), "text/csv", $"History_{DateTime.Now:yyyyMMdd}.csv");
        }

        // --- PDF EXPORT ---
        // Note: This logic assumes you use a library like Rotativa or QuestPDF. 
        // Here is a structured approach for a View-to-PDF flow.
        public async Task<IActionResult> DownloadPdfHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var history = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // If using Rotativa.AspNetCore:
            // return new ViewAsPdf("HistoryPdfReport", history) { FileName = "SubscriptionHistory.pdf" };

            return Ok("PDF Generated - logic depends on your installed PDF library");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> ForceExpiryCheck()
        {
            // 1. Find users whose subscription has already passed
            var expiredProfiles = await _context.UserProfiles
                .Where(p => p.SubscriptionEndDate.HasValue && p.SubscriptionEndDate.Value < DateTime.Now)
                .ToListAsync();

            int count = 0;
            foreach (var profile in expiredProfiles)
            {
                var user = await _userManager.FindByIdAsync(profile.UserId);
                if (user != null)
                {
                    // Remove Role
                    if (await _userManager.IsInRoleAsync(user, "Subscriber"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                    }

                    // Reset Profile
                    profile.CurrentTierId = null;
                    profile.CurrentProductLimit = 0;
                    profile.SubscriptionEndDate = null;
                    profile.SubscriptionStartDate = null;

                    _context.UserProfiles.Update(profile);
                    count++;
                }
            }

            if (count > 0)
            {
                await _context.SaveChangesAsync();
                // Force security update so users lose access immediately
                foreach (var profile in expiredProfiles)
                {
                    var user = await _userManager.FindByIdAsync(profile.UserId);
                    if (user != null) await _userManager.UpdateSecurityStampAsync(user);
                }
            }

            TempData["Success"] = $"Force Check Complete. {count} expired subscriptions were processed.";
            return RedirectToAction(nameof(AdminDashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UserDetails(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return NotFound();

            // 1. Fetch Profile and User info
            var profile = await _context.UserProfiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return NotFound();

            // 2. Fetch all subscription requests (The Timeline)
            var history = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 3. Calculate Total Spending (Assuming Tier has a Price property)
            // If you don't have a Price property yet, you can skip this or use a placeholder
            ViewBag.TotalSpent = history.Where(r => r.Status == RequestStatus.Approved)
                                        .Sum(r => r.Tier.Price);

            ViewBag.History = history;

            return View(profile);
        }


    }
}

