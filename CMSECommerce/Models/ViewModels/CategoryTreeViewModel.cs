using CMSECommerce.Models;

namespace CMSECommerce.Models.ViewModels
{
    public class CategoryTreeViewModel
    {
        public IEnumerable<Category> Categories { get; set; } = Enumerable.Empty<Category>();
        public string? CurrentCategorySlug { get; set; }
        public string? SearchTerm { get; set; }
    }
}
