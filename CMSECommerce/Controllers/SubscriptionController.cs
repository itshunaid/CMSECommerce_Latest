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
            var tiers = await _context.SubscriptionTiers.ToListAsync();
            return View(tiers);
        }

        // 2. Registration Page (AC 2)
        // 2. Registration Page (GET)
        [HttpGet]
        public async Task<IActionResult> Register(int tierId)
        {
            var user = await _userManager.GetUserAsync(User);
            var userProfile = await _context.UserProfiles.FirstOrDefaultAsync(up => up.UserId == user.Id);

            if (userProfile == null || string.IsNullOrEmpty(userProfile.ITSNumber))
            {
                TempData["Error"] = "Please update your ITS number in your profile first.";
                return RedirectToAction("EditProfile", "Account");
            }

            // NEW: Check if user already has a Pending or Approved request
            var existingRequest = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == user.Id &&
                         (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved));

            if (existingRequest)
            {
                TempData["Error"] = "You already have an active subscription or a pending request.";
                return RedirectToAction("Status");
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

        // 3. Form Submission (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitRequest(SubscriptionRequestViewModel model)
        {
            var userId = _userManager.GetUserId(User);

            // Final Gatekeeper: Ensure no request was created between the GET and POST
            var existingRequest = await _context.SubscriptionRequests
                .AnyAsync(r => r.UserId == userId &&
                         (r.Status == RequestStatus.Pending || r.Status == RequestStatus.Approved));

            if (existingRequest)
            {
                TempData["Error"] = "Submission failed: A request is already in progress for this account.";
                return RedirectToAction("Status");
            }

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
                UserId = userId!,
                TierId = model.TierId,
                ItsNumber = model.ITSNumber,
                ReceiptImagePath = fileName,
                CreatedAt = DateTime.UtcNow, // Ensure CreatedAt is set
                Status = RequestStatus.Pending // Explicitly set starting status
            };

            _context.SubscriptionRequests.Add(request);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Your subscription request has been submitted for review.";
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

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Approve(int requestId)
        {
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();
            if (request.Status == RequestStatus.Approved) return BadRequest("This request is already approved.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            // 1. Fetch current roles of the user being approved
            var roles = await _userManager.GetRolesAsync(user);
            bool isUserAdmin = roles.Contains("Admin");
            bool isAlreadySubscriber = roles.Contains("Subscriber");

            // 2. Fetch Profile and check for Soft Delete
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return BadRequest("User profile missing.");
            // Note: Assuming LockoutEnabled is your check for "Active" status
            if (!profile.User.LockoutEnabled) return BadRequest("Cannot approve subscription for a deactivated user.");

            // 3. Role Management Logic
            if (!isAlreadySubscriber)
            {
                if (isUserAdmin)
                {
                    // If they are Admin, just add Subscriber (Multi-role)
                    await _userManager.AddToRoleAsync(user, "Subscriber");
                }
                else
                {
                    // If NOT Admin, overwrite roles: Remove everything, then add Subscriber
                    await _userManager.RemoveFromRolesAsync(user, roles);
                    await _userManager.AddToRoleAsync(user, "Subscriber");
                }
            }

            // 4. Update Request Status
            request.Status = RequestStatus.Approved;

            // 5. Subscription Time Stacking
            DateTime baseline = (profile.SubscriptionEndDate.HasValue && profile.SubscriptionEndDate.Value > DateTime.Now)
                                ? profile.SubscriptionEndDate.Value
                                : DateTime.Now;

            if (!profile.SubscriptionStartDate.HasValue || profile.SubscriptionEndDate < DateTime.Now)
            {
                profile.SubscriptionStartDate = DateTime.Now;
            }

            profile.SubscriptionEndDate = baseline.AddMonths(request.Tier.DurationMonths);
            profile.CurrentProductLimit = request.Tier.ProductLimit;

            _context.UserProfiles.Update(profile);
            _context.SubscriptionRequests.Update(request); // Ensure request status is saved
            await _context.SaveChangesAsync();

            // Force a security stamp update so the user sees the role change immediately
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Success"] = $"Approved! New expiry: {profile.SubscriptionEndDate?.ToString("dd MMM yyyy")}";
            return RedirectToAction(nameof(AdminDashboard));
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revert(int requestId)
        {
            // 1. Fetch Request with Tier details
            var request = await _context.SubscriptionRequests
                .Include(r => r.Tier)
                .FirstOrDefaultAsync(r => r.Id == requestId);

            if (request == null) return NotFound();

            // Safety: Only revert if it was previously Approved
            if (request.Status != RequestStatus.Approved)
                return BadRequest("Only approved requests can be reverted.");

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null) return BadRequest("User not found.");

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null) return BadRequest("Profile missing.");

            var roles = await _userManager.GetRolesAsync(user);
            bool isUserAdmin = roles.Contains("Admin");

            // 2. Roll back the Subscription Dates
            if (profile.SubscriptionEndDate.HasValue)
            {
                // Subtract the months that were added during approval
                profile.SubscriptionEndDate = profile.SubscriptionEndDate.Value.AddMonths(-request.Tier.DurationMonths);

                // Check if the user has no remaining subscription time
                if (profile.SubscriptionEndDate <= DateTime.Now)
                {
                    // Remove Subscriber role
                    if (roles.Contains("Subscriber"))
                    {
                        await _userManager.RemoveFromRoleAsync(user, "Subscriber");
                    }

                    // Reset profile fields
                    profile.CurrentProductLimit = 0;
                    profile.SubscriptionStartDate = null;
                    profile.SubscriptionEndDate = null;

                    // 3. Role Restoration Logic (Crucial for the "Overwrite" logic)
                    // If the user is NOT an admin, and we just removed Subscriber, 
                    // ensure they are back to being a Customer.
                    if (!isUserAdmin)
                    {
                        if (!roles.Contains("Customer"))
                        {
                            await _userManager.AddToRoleAsync(user, "Customer");
                        }
                    }
                }
                else
                {
                    // If they still have time left from a different stacked subscription,
                    // we keep the Subscriber role but might want to reset the limit 
                    // to whatever the remaining/previous tier allowed.
                    profile.CurrentProductLimit = 0;
                }
            }

            // 4. Update Request Status back to Pending
            request.Status = RequestStatus.Pending;

            // 5. Final Save and Security Sync
            _context.UserProfiles.Update(profile);
            _context.SubscriptionRequests.Update(request);
            await _context.SaveChangesAsync();

            // Force refresh of user claims so UI updates immediately
            await _userManager.UpdateSecurityStampAsync(user);

            TempData["Warning"] = $"Reverted! Subscription reduced by {request.Tier.DurationMonths} months.";
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



    }
}

