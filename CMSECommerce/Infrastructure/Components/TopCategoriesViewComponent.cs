using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Infrastructure.Components
{
    public class TopCategoriesViewComponent(DataContext context) : ViewComponent
    {
        private readonly DataContext _context = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var topCategories = await _context.Categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .AsNoTracking()
                .ToListAsync();

            return View(topCategories);
        }
    }
}
