namespace CMSECommerce.Areas.Admin.Models
{
    // Inside a 'ViewModels' folder
    public class ProductListViewModel
    {
        public PaginatedList<Product> Products { get; set; }

        // Filtering Properties
        public string CurrentSearchName { get; set; }
        public string CurrentSearchDescription { get; set; }
        public string CurrentSearchCategory { get; set; }

        // Sorting Properties
        public string CurrentSortOrder { get; set; }
        public int CurrentPageSize { get;  set; }
    }
}
