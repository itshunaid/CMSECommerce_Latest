using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Models; // Ensure SubscriberRequest model is accessible
using System.Linq;
using System.Threading.Tasks;

// Ensure this controller is protected and only accessible by Admins
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SubscriberRequestsController(
    DataContext context,
    UserManager<IdentityUser> userManager) : Controller
{
    private readonly DataContext _context = context;
    private readonly UserManager<IdentityUser> _userManager = userManager;

    // -----------------------------------------------------------------
    // C - CREATE (Admin rarely creates, but included for completeness)
    // -----------------------------------------------------------------
    /*
    // Note: The customer handles creation via AccountController.RequestSeller().
    // An Admin 'Create' function would require selecting a UserId, making it complex.
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubscriberRequest request)
    {
        if (ModelState.IsValid)
        {
            _context.Add(request);
            await _context.SaveChangesAsync();
            TempData["success"] = "Request manually created.";
            return RedirectToAction("Index");
        }
        return View(request);
    }
    */

    // -----------------------------------------------------------------
    // R - READ (Index & Details)
    // -----------------------------------------------------------------

    // GET: Admin/SubscriberRequests (Lists all requests)
    public async Task<IActionResult> Index()
    {
        // Fetch all requests, ordered by the oldest request first
        var requests = await _context.SubscriberRequests.Where(p=>p.Approved==false)
            .OrderBy(r => r.RequestDate)
            .ToListAsync();

        return View(requests);
    }

    // GET: Admin/SubscriberRequests/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var request = await _context.SubscriberRequests.FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            TempData["error"] = "Request not found.";
            return RedirectToAction("Index");
        }

        return View(request);
    }

    // -----------------------------------------------------------------
    // U - UPDATE (Process)
    // -----------------------------------------------------------------

    // POST: Admin/SubscriberRequests/Process
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Process(int id, string adminAction, string adminNotes)
    {
        var request = await _context.SubscriberRequests.FirstOrDefaultAsync(r => r.Id == id);

        if (request == null)
        {
            TempData["error"] = "Request not found.";
            return RedirectToAction("Index");
        }

        // 1. Update the Request Status metadata
        // Ensure ApprovalDate is only set if it's currently null or being explicitly changed
        if (request.Approved == null || (adminAction == "Approve" && request.Approved == false) || (adminAction == "Reject" && request.Approved == true))
        {
            request.ApprovalDate = DateTime.Now;
        }

        request.AdminNotes = adminNotes;

        bool success = false;
        string message = "";

        if (adminAction == "Approve")
        {
            // 2. Assign the Subscriber Role
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user != null)
            {
                // Ensure user does not have the 'Customer' role and add 'Subscriber'
                await _userManager.RemoveFromRoleAsync(user, "Customer");
                var result = await _userManager.AddToRoleAsync(user, "Subscriber");

                if (result.Succeeded)
                {
                    request.Approved = true; // Set status to Approved
                    message = $"Request for {request.UserName} approved and 'Subscriber' role assigned.";
                    success = true;
                }
                else
                {
                    TempData["error"] = $"Error assigning role to {request.UserName}. Role assignment failed.";
                    return RedirectToAction("Details", new { id });
                }
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
            _context.Update(request);
            await _context.SaveChangesAsync();
            TempData["success"] = message;
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
        var request = await _context.SubscriberRequests.FindAsync(id);

        if (request == null)
        {
            TempData["error"] = "Request not found.";
            return RedirectToAction("Index");
        }

        _context.SubscriberRequests.Remove(request);
        await _context.SaveChangesAsync();

        TempData["success"] = $"Request for {request.UserName} has been permanently deleted.";
        return RedirectToAction("Index");
    }
}