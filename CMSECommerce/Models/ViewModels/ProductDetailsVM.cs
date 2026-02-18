using Microsoft.AspNetCore.Identity;

namespace CMSECommerce.Models.ViewModels
{
    public class ProductDetailsVM
    {
        // 1. The main Product details
        public Product Product { get; set; }

        // 2. Statistics for the review system
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }

        // Amazon-style rating breakdown (e.g., 5-star: 50, 4-star: 30, etc.)
        public Dictionary<int, int> RatingCounts { get; set; } = new Dictionary<int, int>();

        // 3. Form model for submitting a NEW review
        public ProductReview NewReview { get; set; }

        // 4. Utility properties
        // Used to determine if the current user can submit a review
        public bool CanReview { get; set; } = false;

        // Used to store the current user (if logged in)
        public IdentityUser CurrentUser { get; set; }
    }
}