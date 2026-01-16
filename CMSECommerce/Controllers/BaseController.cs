using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CMSECommerce.Controllers
{
    public class BaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // 1. Get the cart from the session
            var cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            // 2. Calculate the total number of items
            // This makes 'CartCount' available in any View (including _Layout.cshtml)
            ViewBag.CartCount = cart.Sum(x => x.Quantity);

            // Optional: Also pass the Grand Total if you want it in the header
            ViewBag.CartTotal = cart.Sum(x => x.Quantity * x.Price);

            base.OnActionExecuting(context);
        }
    }
}
