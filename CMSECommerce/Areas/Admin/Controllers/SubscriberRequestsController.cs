using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// Ensure this controller is protected and only accessible by Admins
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SubscriberRequestsController(
    DataContext context,
    UserManager<IdentityUser> userManager,
    ILogger<SubscriberRequestsController> logger) : Controller // ADDED ILogger
{
    private readonly DataContext _context = context;
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly ILogger<SubscriberRequestsController> _logger = logger; // ADDED logger field

    // -----------------------------------------------------------------
    // R - READ (Index & Details)
    // -----------------------------------------------------------------

    // GET: Admin/SubscriberRequests (Lists all requests)
    public async Task<IActionResult> Index()
    {
        try
        {
            // Fetch all pending/unapproved requests, ordered by the oldest request first
            var requests = await _context.SubscriberRequests.Where(p => p.Approved == false || p.Approved == null)
                .OrderBy(r => r.RequestDate)
                .ToListAsync();

            return View(requests);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error retrieving subscriber requests list in Index.");
            TempData["error"] = "A database error occurred while loading the requests list.";
            // Return an empty list or appropriate view on error
            return View(new List<SubscriberRequest>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving subscriber requests list in Index.");
            TempData["error"] = "An unexpected error occurred while loading the requests.";
            return View(new List<SubscriberRequest>());
        }
    }

    // GET: Admin/SubscriberRequests/Details/5
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var request = await _context.SubscriberRequests.FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["error"] = $"Request with ID {id} not found.";
                return RedirectToAction("Index");
            }

            return View(request);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error retrieving subscriber request ID: {RequestId}", id);
            TempData["error"] = "A database error occurred while loading the request details.";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving subscriber request ID: {RequestId}", id);
            TempData["error"] = "An unexpected error occurred while loading the request details.";
            return RedirectToAction("Index");
        }
    }

    // -----------------------------------------------------------------
    // U - UPDATE (Process)
    // -----------------------------------------------------------------

    // POST: Admin/SubscriberRequests/Process
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id, string adminAction, string adminNotes)
    {
        SubscriberRequest request = null;
        try
        {
            request = await _context.SubscriberRequests.FirstOrDefaultAsync(r => r.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscriber request ID: {RequestId} for processing.", id);
            TempData["error"] = "Database error fetching request for processing.";
            return RedirectToAction("Index");
        }

        if (request == null)
        {
            TempData["error"] = "Request not found or already processed.";
            return RedirectToAction("Index");
        }

        // 1. Update the Request Status metadata
        if (request.Approved == null || (adminAction == "Approve" && request.Approved == false) || (adminAction == "Reject" && request.Approved == true))
        {
            request.ApprovalDate = DateTime.Now;
        }

        request.AdminNotes = adminNotes;

        bool success = false;
        string message = "";

        try
        {
            if (adminAction == "Approve")
            {
                // 2. Assign the Subscriber Role
                var user = await _userManager.FindByIdAsync(request.UserId);
                if (user != null)
                {
                    // Attempt to remove 'Customer' role and add 'Subscriber' role
                    await _userManager.RemoveFromRoleAsync(user, "Customer"); // Ignore failure if role doesn't exist
                    var result = await _userManager.AddToRoleAsync(user, "Subscriber");

                    if (result.Succeeded)
                    {
                        request.Approved = true; // Set status to Approved
                        message = $"Request for {request.UserName} approved and 'Subscriber' role assigned.";
                        success = true;
                    }
                    else
                    {
                        _logger.LogError("Identity error assigning 'Subscriber' role to user ID: {UserId}. Errors: {Errors}",
                            request.UserId, string.Join(", ", result.Errors.Select(e => e.Description)));
                        TempData["error"] = $"Error assigning role to {request.UserName}. Role assignment failed: {string.Join(", ", result.Errors.Select(e => e.Description))}";
                        return RedirectToAction("Details", new { id });
                    }
                }
                else
                {
                    _logger.LogWarning("User ID: {UserId} associated with request ID: {RequestId} was not found during approval.", request.UserId, id);
                    TempData["error"] = "The associated user account was not found.";
                    // We can still mark the request as approved in the database if the user is missing, 
                    // but for a Subscriber request, failing the role assignment should halt the process.
                    return RedirectToAction("Details", new { id });
                }
            }
            else if (adminAction == "Reject")
            {
                request.Approved = false; // Set status to Rejected
                message = $"Request for {request.UserName} rejected.";
                success = true;
            }

            if (success)
            {
                // 3. Save the Request metadata changes (Approved status, notes, date)
                _context.Update(request);
                await _context.SaveChangesAsync();
                TempData["success"] = message;
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error while processing/saving subscriber request ID: {RequestId}", id);
            TempData["error"] = "A database error occurred while saving the request status.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during subscriber request processing for ID: {RequestId}", id);
            TempData["error"] = "An unexpected error occurred while processing the request.";
        }

        return RedirectToAction("Index");
    }

    // -----------------------------------------------------------------
    // D - DELETE
    // -----------------------------------------------------------------

    // POST: Admin/SubscriberRequests/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        SubscriberRequest request = null;

        try
        {
            request = await _context.SubscriberRequests.FindAsync(id);

            if (request == null)
            {
                TempData["error"] = "Request not found.";
                return RedirectToAction("Index");
            }

            _context.SubscriberRequests.Remove(request);
            await _context.SaveChangesAsync();

            TempData["success"] = $"Request for {request.UserName} has been permanently deleted.";
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database error deleting subscriber request ID: {RequestId}", id);
            TempData["error"] = "A database error occurred while deleting the request.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting subscriber request ID: {RequestId}", id);
            TempData["error"] = "An unexpected error occurred during deletion.";
        }

        return RedirectToAction("Index");
    }
}