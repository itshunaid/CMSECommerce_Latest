using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CMSECommerce.Controllers
{
    public class PagesController(DataContext context) : Controller
    {
        private readonly DataContext _context = context;

        public async Task<IActionResult> Index(string slug = "")
        {
            //slug = slug.IsNullOrEmpty() ? "home" : slug;
            slug = string.IsNullOrEmpty(slug) ? "home" : slug;
            Page page = await _context.Pages.Where(x => x.Slug == slug).FirstOrDefaultAsync();

            if (page == null) { return NotFound(); }

            return View(page);
        }
    }
}
