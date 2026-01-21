using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using CMSECommerce.Services;
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
        private readonly IAuditService _auditService;
        public SubscriptionController(DataContext context, UserManager<IdentityUser> userManager, IAuditService auditService)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
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
        [Route("Register")]
        public async Task<IActionResult> Register(int tierId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            ViewBag.IsCustomer = User.IsInRole("Customer");

            var userProfile = await _context.UserProfiles
                .Include(up => up.CurrentTier)
                .FirstOrDefaultAsync(up => up.UserId == user.Id);

            var targetTier = await _context.SubscriptionTiers
                .FirstOrDefaultAsync(t => t.Id == tierId);

            if (targetTier == null) return NotFound();

            if (userProfile == null)
            {
                TempData["Error"] = "Please update your profile first.";
                return RedirectToAction("Create", "UserProfiles", new { isNewProfile = true, tierId = tierId, callingFrom="Subscription" });
            }

            if (string.IsNullOrEmpty(userProfile.ITSNumber))
            {
                TempData["Error"] = "Please update your ITS number in your profile first.";
                return RedirectToAction("Edit", "UserProfiles", new { id = userProfile.Id });
            }

            var hasPending = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == user.Id && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                TempData["Error"] = "You already have a request pending review.";
                return RedirectToAction("Status");
            }

            // --- UPDATED PRORATED CALCULATION FOR VIEW ---
            decimal finalPriceToPay = targetTier.Price;
            decimal creditApplied = 0;
            bool isUpgrade = false;

            if (userProfile.CurrentTierId.HasValue && userProfile.SubscriptionEndDate > DateTime.Now)
            {
                if (userProfile.CurrentTierId == tierId)
                {
                    TempData["Info"] = "You are already on this plan.";
                    return RedirectToAction("Status");
                }

                if (userProfile.CurrentTier != null && targetTier.Price <= userProfile.CurrentTier.Price)
                {
                    TempData["Error"] = "Downgrades are not permitted. Please wait for your current plan to expire.";
                    return RedirectToAction("Index");
                }

                isUpgrade = true;

                // Calculate daily value of current subscription
                int totalDaysOldPlan = userProfile.CurrentTier.DurationMonths * 30;
                decimal dailyRate = userProfile.CurrentTier.Price / totalDaysOldPlan;
                int remainingDays = (int)(userProfile.SubscriptionEndDate.Value - DateTime.Now).TotalDays;

                creditApplied = dailyRate * Math.Max(0, remainingDays);
                finalPriceToPay = Math.Max(0, targetTier.Price - creditApplied);
            }

            var model = new SubscriptionRequestViewModel
            {
                TierId = tierId,
                TierName = targetTier.Name,
                Price = targetTier.Price,
                FinalAmount = finalPriceToPay,
                CreditApplied = creditApplied,
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
            ModelState.Remove("ITSNumber");
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userProfile = await _context.UserProfiles
                .Include(up => up.CurrentTier)
                .FirstOrDefaultAsync(up => up.UserId == user.Id);
            

            if (userProfile == null) return NotFound();

            if (!string.IsNullOrEmpty(model.ITSNumber))
            {
                userProfile.ITSNumber = model.ITSNumber;

                _context.UserProfiles.Update(userProfile);

            }

            var hasPending = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == user.Id && r.Status == RequestStatus.Pending);

            if (hasPending)
            {
                TempData["Error"] = "Submission failed: You already have a request pending review.";
                return RedirectToAction("Status");
            }

            var targetTier = await _context.SubscriptionTiers.FindAsync(model.TierId);
            if (targetTier == null) return NotFound();

            // --- UPDATED SERVER-SIDE PRORATED CALCULATION ---
            decimal serverCalculatedFinalAmount = targetTier.Price;
            decimal serverCalculatedCredit = 0;

            if (userProfile.CurrentTierId != null && userProfile.SubscriptionEndDate > DateTime.Now)
            {
                if (userProfile.CurrentTierId == model.TierId)
                {
                    TempData["Info"] = "You are already on this plan.";
                    return RedirectToAction("Status");
                }

                if (targetTier.Price <= userProfile.CurrentTier.Price)
                {
                    TempData["Error"] = "Downgrading is not supported.";
                    return RedirectToAction("Index");
                }

                // Exact Proration Calculation
                int totalDaysOldPlan = userProfile.CurrentTier.DurationMonths * 30;
                decimal dailyRate = userProfile.CurrentTier.Price / totalDaysOldPlan;
                int remainingDays = (int)(userProfile.SubscriptionEndDate.Value - DateTime.Now).TotalDays;

                serverCalculatedCredit = dailyRate * Math.Max(0, remainingDays);
                serverCalculatedFinalAmount = Math.Max(0, targetTier.Price - serverCalculatedCredit);
            }

            if (!ModelState.IsValid) return View("Register", model);

            string fileName = "default.png";
            if (model.Receipt != null)
            {
                var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads");
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);
                fileName = Guid.NewGuid() + Path.GetExtension(model.Receipt.FileName);
                string path = Path.Combine(uploadFolder, fileName);
                using (var stream = new FileStream(path, FileMode.Create)) await model.Receipt.CopyToAsync(stream);
            }

            var request = new SubscriptionRequest
            {
                UserId = user.Id,
                TierId = model.TierId,
                ItsNumber = model.ITSNumber,
                ReceiptImagePath = fileName,
                CreatedAt = DateTime.UtcNow,
                Status = RequestStatus.Pending,
                IsUpgrade = userProfile.CurrentTierId != null,
                FinalAmount = serverCalculatedFinalAmount,
                CreditApplied = serverCalculatedCredit
            };

            _context.SubscriptionRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = userProfile.CurrentTierId != null
                ? $"Upgrade submitted! Adjusted price: ₹{serverCalculatedFinalAmount:N2} (₹{serverCalculatedCredit:N2} credit applied)."
                : "Subscription request submitted successfully.";

            return RedirectToAction("Status");
        }
        [Authorize(Roles = "Admin,SuperAdmin")]
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
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> Approve(int requestId)
        {
            // 1. Fetch Request with Tier details
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            // 2. Status Guard Clauses
            if (request.Status == RequestStatus.Approved) return BadRequest("This request is already approved.");
            if (request.Status == RequestStatus.Rejected) return BadRequest("Cannot approve a rejected request.");

            // 3. Fetch User and Profile
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            var profile = await _context.UserProfiles
                .Include(p => p.CurrentTier)
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null) return BadRequest("User profile missing.");

            if (user.LockoutEnabled && await _userManager.IsLockedOutAsync(user))
                return BadRequest("User is currently deactivated/locked.");

            // 4. ARCHITECTURAL CHANGE: Use Saved Values
            // Instead of recalculating, we use the values frozen at the time of user submission.
            // This ensures the Admin approves the exact amount the user paid/submitted.
            decimal finalPriceToCharge = request.FinalAmount;
            decimal creditFromOldPlan = request.CreditApplied;

            // 5. Apply New Subscription Dates & Quotas
            // Use UTC for server-side consistency
            DateTime newStartDate = DateTime.UtcNow;
            int newPlanDays = request.Tier.DurationMonths * 30;

            profile.SubscriptionStartDate = newStartDate;
            profile.SubscriptionEndDate = newStartDate.AddDays(newPlanDays);
            profile.CurrentTierId = request.TierId;
            profile.CurrentProductLimit = request.Tier.ProductLimit;
            profile.IsDeactivated = false;

            // 6. Role Management (Optimized check)
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
            }
            await _userManager.AddToRoleAsync(user, "Subscriber");

            // 7. Store Logic & Finalize
            request.Status = RequestStatus.Approved;
            request.ApprovedAt = DateTime.UtcNow; // Audit trail
            request.ApprovedBy= User.Identity.Name;

            var store = await _context.Stores.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (store != null)
            {
                store.IsActive = true;
                if (profile.StoreId == null)
                {
                    profile.StoreId = store.Id;
                }
                _context.Stores.Update(store);
            }

            // 8. Transactional Save
            _context.SubscriptionRequests.Update(request);
            _context.UserProfiles.Update(profile);

            await _context.SaveChangesAsync();

            // Audit logging
            await _auditService.LogActionAsync("Approve Subscription Request", "SubscriptionRequest", requestId.ToString(), $"Approved subscription for user {user.UserName} to tier {request.Tier.Name}", HttpContext);

            // Refresh security stamp so the user's "Subscriber" role takes effect immediately
            await _userManager.UpdateSecurityStampAsync(user);

            // 9. Feedback Message
            string creditNote = creditFromOldPlan > 0
                ? $" ₹{creditFromOldPlan:N2} credit applied."
                : "";

            TempData["Success"] = $"Plan '{request.Tier.Name}' Approved! Total: ₹{finalPriceToCharge:N2}.{creditNote}";

            return RedirectToAction("AdminDashboard", "Subscription");
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
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

            // Audit logging
            await _auditService.LogActionAsync("Revert Subscription Request", "SubscriptionRequest", requestId.ToString(), $"Reverted approval for user {user.UserName} from tier {request.Tier.Name}", HttpContext);

            // Invalidate security stamp to force-refresh user's claims/roles in their session
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Warning"] = "Approval Reverted. Limits and Tier have been reset to previous values.";
            return RedirectToAction(nameof(AdminDashboard));
        }


        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
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

            // Audit logging
            await _auditService.LogActionAsync("Reject Subscription Request", "SubscriptionRequest", requestId.ToString(), $"Rejected subscription request. Reason: {reason}", HttpContext);

            TempData["Error"] = $"Request for {request.Tier?.Name ?? "Subscription"} Rejected. Reason: {reason}";
            return RedirectToAction(nameof(AdminDashboard));
        }
        public async Task<IActionResult> Status()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // 1. Fetch the profile with the current tier
            var profile = await _context.UserProfiles
                .Include(p => p.CurrentTier)
                .FirstOrDefaultAsync(p => p.UserId == userId);

            // 2. Fetch all subscription requests for this user
            var requests = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            // 3. --- NEW: CALCULATE CURRENT TRADE-IN VALUE ---
            decimal currentProratedValue = 0;
            int remainingDays = 0;

            if (profile?.CurrentTier != null && profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate > DateTime.Now)
            {
                remainingDays = (int)(profile.SubscriptionEndDate.Value - DateTime.Now).TotalDays;

                if (remainingDays > 0)
                {
                    // Logic: (Price / TotalDays) * RemainingDays
                    int totalDaysInPlan = profile.CurrentTier.DurationMonths * 30;
                    decimal dailyRate = profile.CurrentTier.Price / totalDaysInPlan;
                    currentProratedValue = dailyRate * remainingDays;
                }
            }

            // 4. Pass data to ViewBag
            ViewBag.UserProfile = profile;
            ViewBag.IsExpired = profile?.SubscriptionEndDate < DateTime.Now;
            ViewBag.RemainingDays = Math.Max(0, remainingDays);
            ViewBag.CurrentTradeInValue = currentProratedValue; // To show "You have ₹XX credit available for upgrade"

            return View(requests);
        }

        // Add this to your SubscriptionController.cs
        [HttpGet]
        public async Task<IActionResult> GetLatestStatuses()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Fetch the latest state of all requests for this user
            var statuses = await _context.SubscriptionRequests
                .Where(r => r.UserId == userId)
                .Select(r => new
                {
                    id = r.Id,
                    status = r.Status.ToString(),
                    // Adding these ensures that any UI logic tracking amount 
                    // changes during the background poll stays in sync
                    finalAmount = r.FinalAmount,
                    creditApplied = r.CreditApplied
                })
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

        [Authorize(Roles = "Admin,SuperAdmin")]
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
            ViewBag.TotalCreditsGranted = history
                .Where(r => r.Status == RequestStatus.Approved)
                .Sum(r => r.CreditApplied);

            // 5. Calculate Remaining Days & Current Monetary Value
            int remainingDays = 0;
            decimal currentProRatedValue = 0;

            if (profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate > DateTime.Now)
            {
                remainingDays = (int)(profile.SubscriptionEndDate.Value - DateTime.Now).TotalDays;

                // NEW: Calculate what the remaining time is worth in currency
                if (profile.CurrentTier != null && remainingDays > 0)
                {
                    int totalDaysInPlan = profile.CurrentTier.DurationMonths * 30;
                    decimal dailyRate = profile.CurrentTier.Price / totalDaysInPlan;
                    currentProRatedValue = dailyRate * remainingDays;
                }
            }

            ViewBag.RemainingDays = Math.Max(0, remainingDays);
            ViewBag.CurrentValue = currentProRatedValue; // The "Trade-in" value if they upgrade now
            ViewBag.History = history;

            return View(profile);
        }
        public IActionResult Terms() => View();
        public IActionResult Privacy() => View();
    }
}

