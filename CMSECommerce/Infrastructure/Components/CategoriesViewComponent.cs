using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Infrastructure.Components
{
    public class CategoriesViewComponent(DataContext context) : ViewComponent
    {
        private readonly DataContext _context = context;

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var cats = await _context.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

            // Build hierarchical list
            var byParent = cats.GroupBy(c => c.ParentId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());
            List<Category> BuildTree(int? parentId)
            {
                var list = new List<Category>();
                if (!byParent.ContainsKey(parentId)) return list;
                foreach (var c in byParent[parentId])
                {
                    var copy = new Category
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Slug = c.Slug,
                        ParentId = c.ParentId,
                        Level = c.Level,
                        Children = BuildTree(c.Id)
                    };
                    list.Add(copy);
                }
                return list;
            }

            var tree = BuildTree(null);
            return View(tree);
        }
    }
}
