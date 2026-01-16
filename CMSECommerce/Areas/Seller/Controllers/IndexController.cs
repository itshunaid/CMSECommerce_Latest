using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CMSECommerce.Areas.Seller.Controllers
{
    [Area("Seller")]
    [Authorize(Roles = "Subscriber")]
    public class IndexController : Controller
    {
        public IActionResult Index(int categoryId = 0, string search = "", string status = "", int p = 1)
        {
            // Redirect to the Products Index action with the same parameters
            return RedirectToAction("Index", "Products", new { categoryId, search, status, p });
        }
    }
}
