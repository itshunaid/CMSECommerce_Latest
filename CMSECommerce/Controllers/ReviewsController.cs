using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CMSECommerce.Controllers
{
    // Inject the database context
    public class ReviewsController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        // This action handles the POST request from the review form on the Product page
        [HttpPost]
        [Authorize] // Ensure only logged-in users can submit a review
        public async Task<IActionResult> AddReview(Review review)
        {
            // 1. Get current logged-in user details (assuming ASP.NET Core Identity)
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string userName = User.FindFirstValue(ClaimTypes.GivenName) ?? User.Identity.Name; // Use username or fallback

            review.UserId = userId;
            review.UserName = userName;
            review.DateCreated = DateTime.Now;

            // 2. Validate the incoming Review model
            if (ModelState.IsValid)
            {
                // Ensure ProductId is valid and the product exists before saving
                var product = await _context.Products.FindAsync(review.ProductId);
                if (product == null)
                {
                    ModelState.AddModelError("", "Product not found.");
                    // Fall through to return to the product page with an error
                }
                else
                {
                    // 3. Save the new review
                    _context.Reviews.Add(review);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Your review was submitted successfully!";

                    // Redirect back to the product's detail page (using the product's slug)
                    return RedirectToAction("Product", "Products", new { slug = product.Slug });
                }
            }

            // If validation failed or product wasn't found, you need to load the product 
            // and return to its detail view to show errors.
            var productForReturn = await _context.Products
                                                .Include(p => p.Reviews) // Eager load reviews
                                                .Include(p => p.Category)
                                                .FirstOrDefaultAsync(p => p.Id == review.ProductId);

            if (productForReturn != null)
            {
                // You may want to pass the product back to the view, or handle errors differently.
                // For simplicity here, we redirect to prevent a crash, but a full solution
                // would return the model to the original view with validation errors.
                TempData["Error"] = "Review submission failed. Please check your rating and comments.";
                return RedirectToAction("Product", "Products", new { slug = productForReturn.Slug });
            }

            // Final fallback if we can't find the product
            return RedirectToAction("Index", "Products");
        }
    }
}