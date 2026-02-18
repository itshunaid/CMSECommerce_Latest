using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Infrastructure.Components
{
    public class MenuViewComponent : ViewComponent
    {
        private readonly DataContext _context;

        public MenuViewComponent(DataContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // 1. Fetch Pages from the database, sorted by a relevant property
            // Assuming your DbSet is named 'Pages' and the Page model has Title, Slug, and Sorting properties.
            var pages = await _context.Pages
                                      .OrderBy(p => p.Order)
                                      .ToListAsync();

            var items = new List<MenuItemViewModel>();

            // 2. Map the fetched Pages to the MenuItemViewModel
            foreach (var page in pages)
            {
                items.Add(new MenuItemViewModel
                {
                    Title = page.Title,
                    Slug = page.Slug,
                    IsAdmin = false // Dynamic pages are not admin links
                });
            }

            // 3. Show admin area link to admins only (Existing Logic)
            var user = HttpContext.User;
            if (user.Identity?.IsAuthenticated == true && user.IsInRole("Admin"))
            {
                // Add the admin link to the end of the menu
                items.Add(new MenuItemViewModel { Title = "Admin", Slug = "admin", IsAdmin = true });
            }

            return View(items);
        }
    }

    // This class remains unchanged, but I'm including it for completeness.
    public class MenuItemViewModel
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public bool IsAdmin { get; set; }
    }
}