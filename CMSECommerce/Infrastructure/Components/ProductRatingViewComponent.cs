using Microsoft.AspNetCore.Mvc;
namespace CMSECommerce.Infrastructure.Components
{   

    public class ProductRatingViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(double averageRating, int totalReviews, Dictionary<int, int> ratingCounts)
        {
            // Passed directly from the ProductDetailsVM
            ViewBag.AverageRating = averageRating;
            ViewBag.TotalReviews = totalReviews;
            ViewBag.RatingCounts = ratingCounts;

            // Simple way to calculate the percentage for the bar charts
            ViewBag.MaxCount = ratingCounts.Any() ? ratingCounts.Values.Max() : 1;

            return View();
        }
    }
}
