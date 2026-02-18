using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Infrastructure.Filters
{
    public class PopulateCategoriesFilter : IAsyncActionFilter
    {
        private readonly DataContext _context;

        public PopulateCategoriesFilter(DataContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            try
            {
                var controller = context.Controller as Controller;
                if (controller == null)
                {
                    await next();
                    return;
                }

                var cats = await _context.Categories.AsNoTracking().ToListAsync();

                // Build hierarchical select list
                var byParent = cats.GroupBy(c => c.ParentId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());
                var items = new List<SelectListItem>();

                void AddChildren(int? parentId, string prefix)
                {
                    if (!byParent.ContainsKey(parentId)) return;
                    foreach (var c in byParent[parentId])
                    {
                        items.Add(new SelectListItem
                        {
                            Value = c.Id.ToString(),
                            Text = prefix + c.Name
                        });
                        AddChildren(c.Id, prefix + "— ");
                    }
                }

                AddChildren(null, string.Empty);

                controller.ViewBag.Categories = items;
            }
            catch
            {
                // swallow - do not block action on filter failure
            }

            await next();
        }
    }
}
