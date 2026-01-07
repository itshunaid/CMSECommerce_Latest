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
    
    public class SubscriptionController : BaseController
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


        [HttpGet]
        public async Task<IActionResult> Register(int tierId)
        {
            var isCustomer = User.IsInRole("Customer");
            ViewBag.IsCustomer = isCustomer;
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // 1. Fetch User Profile and Target Tier concurrently to save time
            var userProfileTask = _context.UserProfiles
                .Include(up => up.CurrentTier)
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            var targetTierTask = _context.SubscriptionTiers
                                    .FirstOrDefaultAsync(t => t.Id == tierId);

            await Task.WhenAll(userProfileTask, targetTierTask);

            var userProfile = await userProfileTask;
            var targetTier = await targetTierTask;

            if (targetTier == null) return NotFound();

            // 2. Profile & ITS Validation
            if (userProfile == null)
            {
                TempData["Error"] = "Please update your profile first.";
                return RedirectToAction("Create", "UserProfiles", new { isNewProfile = true, tierId = tierId });
            }

            if (string.IsNullOrEmpty(userProfile.ITSNumber))
            {
                TempData["Error"] = "Please update your ITS number in your profile first.";
                return RedirectToAction("Edit", "UserProfiles", new { id = userProfile.Id });
            }

            // 3. Pending Request Shield
            var hasPending = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == user.Id && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                TempData["Error"] = "You already have a request pending review.";
                return RedirectToAction("Status");
            }

            // 4. Fair Value Upgrade Logic & Calculation
            decimal finalPriceToPay = targetTier.Price;
            decimal creditApplied = 0;
            bool isUpgrade = false;

            // Check if user is currently an active subscriber
            if (userProfile.CurrentTierId.HasValue && userProfile.SubscriptionEndDate > DateTime.Now)
            {
                // Prevent requesting the same plan
                if (userProfile.CurrentTierId == tierId)
                {
                    TempData["Info"] = "You are already on this plan.";
                    return RedirectToAction("Status");
                }

                // Block downgrades (Safety check)
                if (targetTier.Price <= userProfile.CurrentTier.Price)
                {
                    TempData["Error"] = "Downgrades are not permitted. Please wait for your current plan to expire.";
                    return RedirectToAction("Index");
                }

                // SUCCESSFUL UPGRADE PATH
                isUpgrade = true;
                creditApplied = userProfile.CurrentTier.Price;
                finalPriceToPay = Math.Max(0, targetTier.Price - creditApplied);
            }

            // 5. Build the ViewModel
            var model = new SubscriptionRequestViewModel
            {
                TierId = tierId,
                TierName = targetTier.Name,
                Price = targetTier.Price,           // Base price of new tier
                FinalAmount = finalPriceToPay,      // What they pay now
                CreditApplied = creditApplied,      // The "Discount" shown
                ITSNumber = userProfile.ITSNumber,
                FullName = $"{userProfile.FirstName} {userProfile.LastName}",
                IsUpgrade = isUpgrade
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

            // 1. Fetch User Profile with CurrentTier to validate/re-calculate credit
            var userProfile = await _context.UserProfiles
                .Include(up => up.CurrentTier)
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (userProfile == null) return NotFound();

            // --- NEW LOGIC: Update ITSNumber in UserProfile if provided (for Customers) ---
            // We assume if IsCustomer is true (passed from view or checked via role), we allow the update.
            // You can also add a role check here: await _userManager.IsInRoleAsync(user, "Customer")
            if (!string.IsNullOrEmpty(model.ITSNumber) && userProfile.ITSNumber != model.ITSNumber)
            {
                userProfile.ITSNumber = model.ITSNumber;
                _context.UserProfiles.Update(userProfile);
                // Note: SaveChangesAsync is called later in Step 6 to keep the operation atomic
            }

            // 2. Final Gatekeeper: Block if a PENDING request already exists
            var hasPending = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == userId && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                TempData["Error"] = "Submission failed: You already have a request pending review.";
                return RedirectToAction("Status");
            }

            // 3. Fetch Target Tier to verify price
            var targetTier = await _context.SubscriptionTiers.FindAsync(model.TierId);
            if (targetTier == null) return NotFound();

            // 4. THE FAIR VALUE UPGRADE LOGIC (Server-Side Implementation)
            decimal serverCalculatedFinalAmount = targetTier.Price;
            decimal serverCalculatedCredit = 0;

            if (userProfile?.CurrentTierId != null && userProfile.SubscriptionEndDate > DateTime.Now)
            {
                // Block same-plan requests if already active
                if (userProfile.CurrentTierId == model.TierId)
                {
                    TempData["Info"] = "You are already on this plan.";
                    return RedirectToAction("Status");
                }

                // Only Upgrade allowed (Target Price > Current Price)
                if (targetTier.Price <= userProfile.CurrentTier.Price)
                {
                    TempData["Error"] = "Downgrading or switching to a cheaper plan is not supported.";
                    return RedirectToAction("Index");
                }

                // --- THE UPDATED FAIR CREDIT CALCULATION ---
                serverCalculatedCredit = userProfile.CurrentTier.Price;
                serverCalculatedFinalAmount = Math.Max(0, targetTier.Price - serverCalculatedCredit);
            }

            if (!ModelState.IsValid) return View("Register", model);

            // 5. Handle Receipt Upload
            string fileName = "default.png";
            if (model.Receipt != null)
            {
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                fileName = Guid.NewGuid() + Path.GetExtension(model.Receipt.FileName);
                string path = Path.Combine(uploadFolder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await model.Receipt.CopyToAsync(stream);
                }
            }

            // 6. Create Subscription Request with New Columns
            var request = new SubscriptionRequest
            {
                UserId = userId!,
                TierId = model.TierId,
                ItsNumber = model.ITSNumber,
                ReceiptImagePath = fileName,
                CreatedAt = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                IsUpgrade = userProfile?.CurrentTierId != null && targetTier.Price > (userProfile.CurrentTier?.Price ?? 0),
                FinalAmount = serverCalculatedFinalAmount,
                CreditApplied = serverCalculatedCredit
            };

            _context.SubscriptionRequests.Add(request);

            // This will save BOTH the UserProfile update and the SubscriptionRequest
            await _context.SaveChangesAsync();

            // 7. Success Message
            if (userProfile?.CurrentTierId != null)
            {
                TempData["Success"] = $"Upgrade submitted! Adjusted price: ₹{serverCalculatedFinalAmount} (₹{serverCalculatedCredit} credit applied from your {userProfile.CurrentTier.Name} plan). Profile updated.";
            }
            else
            {
                TempData["Success"] = "Subscription request submitted and profile updated.";
            }

            return RedirectToAction("Status");
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            // 1. Fetch requests
            var requests = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 2. Efficiently fetch all relevant profiles in ONE query
            var userIds = requests.Select(r => r.UserId).Distinct().ToList();

            var userProfiles = await _context.UserProfiles
                .Include(r=> r.User) // Include User for easier access to username/email in the View
                .Where(p => userIds.Contains(p.UserId))
                .ToListAsync();

            // 3. Store in ViewBag for the View to access
            ViewBag.UserProfiles = userProfiles;

            // 4. Stats
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

            var profile = await _context.UserProfiles
                .Include(p => p.CurrentTier)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return BadRequest("User profile missing.");

            if (user.LockoutEnabled && await _userManager.IsLockedOutAsync(user))
                return BadRequest("User is currently deactivated/locked.");

            // 2. Capture Current Context & Old Plan Data
            int currentTierId = profile.CurrentTierId ?? 0;
            int requestedTierId = request.TierId;
            string oldPlanName = profile.CurrentTier?.Name ?? "No Active Plan";

            // --- THE FAIR PLAY DATE CALCULATION (CARRY OVER) ---
            // Rule: New plan duration starts TODAY + Append remaining days from the old plan.
            int carryOverDays = 0;
            if (profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate.Value > DateTime.Now)
            {
                carryOverDays = (int)(profile.SubscriptionEndDate.Value - DateTime.Now).TotalDays;
            }

            // 3. Upgrade Detection Logic
            // Using price-based logic to match the Register/SubmitRequest actions
            bool isActuallyUpgrade = profile.CurrentTierId.HasValue &&
                                     request.Tier.Price > (profile.CurrentTier?.Price ?? 0);

            request.IsUpgrade = isActuallyUpgrade;

            // 4. Downgrade Check (Double-check before final approval)
            if (profile.CurrentTierId.HasValue && request.Tier.Price < profile.CurrentTier.Price)
            {
                string reason = $"Downgrade detected (Price Drop). Active: {oldPlanName}, Requested: {request.Tier.Name}. Approval blocked.";
                return await Reject(requestId, reason);
            }

            // 5. Apply Dates
            DateTime newStartDate = DateTime.Now;
            int newPlanDays = request.Tier.DurationMonths * 30; // Standardized duration

            // New Expiry = Today + New Plan Days + Carried Over Days
            profile.SubscriptionEndDate = newStartDate.AddDays(newPlanDays + Math.Max(0, carryOverDays));
            profile.SubscriptionStartDate = newStartDate;

            // 6. Update Profile Quotas & Tier
            profile.CurrentProductLimit = request.Tier.ProductLimit;
            profile.CurrentTierId = requestedTierId;

            // 7. Role Management
            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Contains("Subscriber"))
            {
                // --- NEW ROLE REPLACEMENT LOGIC ---
                // Get all current roles for the user
                var currentRoles = await _userManager.GetRolesAsync(user);

                // Remove the user from all current roles
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Add the user to the "Subscriber" role
                await _userManager.AddToRoleAsync(user, "Subscriber");
            }

            // 8. Finalize Request
            request.Status = RequestStatus.Approved;

            _context.SubscriptionRequests.Update(request);
            _context.UserProfiles.Update(profile);

            await _context.SaveChangesAsync();
            await _userManager.UpdateSecurityStampAsync(user);

            // 9. Success Message
            string upgradeNote = isActuallyUpgrade
                ? $" (Upgraded from {oldPlanName}. {carryOverDays} days carried over)"
                : "";

            TempData["Success"] = $"Plan '{request.Tier.Name}' Approved!{upgradeNote} New expiry: {profile.SubscriptionEndDate?.ToString("dd MMM yyyy")}";

            return RedirectToAction(nameof(AdminDashboard));
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revert(int requestId)
        {
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            // --- SECURITY BLOCK START ---
            // If the status is already Pending, Rejected, or any other state, block access.
            // This aligns with the UI change where the Revert button is hidden for non-approved items.
            if (request.Status != RequestStatus.Approved)
            {
                TempData["Warning"] = "Action Denied: Only Approved requests can be reverted. This record is currently locked.";
                return RedirectToAction(nameof(AdminDashboard));
            }
            // --- SECURITY BLOCK END ---

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

            // Invalidate security stamp to force-refresh user's claims/roles in their session
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Warning"] = "Approval Reverted. Limits and Tier have been reset to previous values.";
            return RedirectToAction(nameof(AdminDashboard));
        }


        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Reject(int requestId, string reason)
        {
            // Find the request and the user profile to check existing status
            var request = await _context.SubscriptionRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            // 1. Update Request Status and Reason
            request.Status = RequestStatus.Rejected;
            request.RejectionReason = reason;

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile != null)
            {
                // 2. SAFE PROFILE STATE RESET (Fair Play Logic)
                // Check 1: If it's an Upgrade, we DO NOT touch their profile. 
                // They should keep their CurrentTier, CurrentProductLimit, and ExpiryDate.

                // Check 2: If it's a fresh registration (!IsUpgrade), we reset to 0 
                // because their payment for the initial access was invalid.
                if (!request.IsUpgrade)
                {
                    // Only reset if they aren't somehow already on an active plan 
                    // (e.g., protecting against accidental double-clicks or manual admin errors)
                    if (!profile.CurrentTierId.HasValue || profile.SubscriptionEndDate < DateTime.Now)
                    {
                        profile.CurrentProductLimit = 0;
                        profile.SubscriptionStartDate = null;
                        profile.SubscriptionEndDate = null;
                        profile.CurrentTierId = null;

                        // Strip Subscriber Role only if they are not active
                        if (await _userManager.IsInRoleAsync(user, "Subscriber"))
                        {
                            await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                        }
                    }
                }
                else
                {
                    // Logic for Upgrades:
                    // We do nothing to the profile. The 'CreditApplied' and 'FinalAmount' 
                    // on the request record simply become historical data of a failed attempt.
                    // The user stays on their old plan as if the upgrade never happened.
                }

                _context.UserProfiles.Update(profile);
            }

            // 3. Security Stamp Update
            // Forces the user's browser to sync with the server on their next click
            await _userManager.UpdateSecurityStampAsync(user);

            // 4. Final Persistence
            _context.SubscriptionRequests.Update(request);
            await _context.SaveChangesAsync();

            TempData["Error"] = $"Request for {request.Tier?.Name ?? "Subscription"} Rejected. Reason: {reason}";
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
                .Include(p => p.CurrentTier)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return NotFound();

            // 2. Fetch all subscription requests
            var history = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 3. Calculate Total Spending (Actual Cash Received)
            // We sum 'FinalAmount'. If it's an older record where FinalAmount was null/0, 
            // we fallback to the Tier Price.
            ViewBag.TotalSpent = history
                .Where(r => r.Status == RequestStatus.Approved)
                .Sum(r => r.FinalAmount > 0 ? r.FinalAmount : r.Tier.Price);

            // 4. Calculate Total Credits Applied (Fair Adjustment Savings)
            // This shows the Admin how much the user has "saved" via the upgrade logic.
            ViewBag.TotalCreditsGranted = history
                .Where(r => r.Status == RequestStatus.Approved)
                .Sum(r => r.CreditApplied);

            // 5. Calculate Remaining Days & Current Value
            // This helps the Admin see if the user is eligible for another upgrade soon.
            int remainingDays = 0;
            if (profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate > DateTime.Now)
            {
                remainingDays = (int)(profile.SubscriptionEndDate.Value - DateTime.Now).TotalDays;
            }
            ViewBag.RemainingDays = Math.Max(0, remainingDays);

            ViewBag.History = history;

            return View(profile);
        }
        public IActionResult Terms() => View();
        public IActionResult Privacy() => View();
    }
}

