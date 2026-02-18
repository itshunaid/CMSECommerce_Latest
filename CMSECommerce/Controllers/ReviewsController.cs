using CMSECommerce.Infrastructure;
using CMSECommerce.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CMSECommerce.Controllers
{
    // Inject the database context
    // It is highly recommended to also inject ILogger<ReviewsController> for production logging.
    public class ReviewsController(DataContext context, IAuditService auditService) : BaseController
    {
        private readonly DataContext _context = context;
        private readonly IAuditService _auditService = auditService;

        // This action handles the POST request from the review form on the Product page
        [HttpPost]
        [Authorize] // Ensure only logged-in users can submit a review
        public async Task<IActionResult> AddReview(Review review)
        {
            // 1. Get current logged-in user details (assuming ASP.NET Core Identity)
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            // Use GetUserName() from UserManager if available, or fallback as shown:
            string userName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity.Name;

            // Basic checks for required fields
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "Authentication error: You must be logged in to submit a review.";
                return RedirectToAction("Index", "Products");
            }

            // Populate required Review properties
            review.UserId = userId;
            review.UserName = userName;
            review.DateCreated = DateTime.Now;

            // Initialize product variable for use in both try/catch and redirect logic
            Product product = null;

            try
            {
                // 2. Validate the incoming Review model
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Review submission failed. Please ensure your rating and comments are valid.";
                    // Skip to product retrieval block to handle redirect
                }
                else
                {
                    // Ensure ProductId is valid and the product exists before saving
                    product = await _context.Products
                        .AsNoTracking() // Read-only query
                        .FirstOrDefaultAsync(p => p.Id == review.ProductId);

                    if (product == null)
                    {
                        TempData["Error"] = "Cannot submit review: The target product was not found.";
                        return RedirectToAction("Index", "Products");
                    }

                    // 3. Save the new review
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    // Audit logging
                    await _auditService.LogEntityCreationAsync(review, review.Id.ToString(), HttpContext);

                    TempData["Success"] = "Your review was submitted successfully and is awaiting approval.";

                    // Redirect back to the product's detail page (using the product's slug)
                    return RedirectToAction("Product", "Products", new { slug = product.Slug });
                }
            }
            catch (DbUpdateException dbEx)
            {
                // Log the database exception (in a real app, use ILogger)
                // _logger.LogError(dbEx, "Database error saving review for ProductId: {ProductId}", review.ProductId);
                TempData["Error"] = "A database error occurred while saving your review. Please try again.";
            }
            catch (Exception ex)
            {
                // Log general runtime exception
                // _logger.LogError(ex, "Unexpected error saving review for ProductId: {ProductId}", review.ProductId);
                TempData["Error"] = "An unexpected error occurred during review submission.";
            }

            // --- Fallback/Error Redirection Logic ---
            // If an error occurred or ModelState was invalid, we need to find the product to redirect to its page.
            if (product == null && review.ProductId > 0)
            {
                // Try to retrieve the product just for the slug
                product = await _context.Products
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == review.ProductId);
            }

            // Redirect back to the product page or the shop index
            if (product != null)
            {
                // Redirect back to the product page so the user can see their error message
                return RedirectToAction("Product", "Products", new { slug = product.Slug });
            }

            // Final fallback if the product ID was invalid or we couldn't retrieve it
            return RedirectToAction("Index", "Products");
        }
    }
}