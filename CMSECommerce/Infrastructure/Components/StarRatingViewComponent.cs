using Microsoft.AspNetCore.Mvc;
namespace CMSECommerce.Infrastructure.Components
{
    

    // View Components should reside in the 'ViewComponents' folder 
    // or be explicitly decorated with [ViewComponent].
    public class StarRatingViewComponent : ViewComponent
    {
        // The Invoke method receives the 'rating' argument passed from the view.
        public IViewComponentResult Invoke(double rating)
        {
            // Pass the rating directly to the View Component's view.
            ViewBag.Rating = rating;

            // This will look for the view file: 
            // /Views/Shared/Components/StarRating/Default.cshtml
            return View();
        }
    }
}
