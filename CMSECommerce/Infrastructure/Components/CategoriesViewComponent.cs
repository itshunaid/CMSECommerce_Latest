using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Infrastructure.Components
{
    public class CategoriesViewComponent(DataContext context) : ViewComponent
    {
        private readonly DataContext _context = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            return View(await _context.Categories.ToListAsync());
        }
    }
}
