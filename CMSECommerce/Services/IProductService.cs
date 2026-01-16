using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IProductService
    {
        Task<ProductListViewModel> GetProductListAsync(string slug, int page, string searchTerm, string sortOrder, decimal? minPrice, decimal? maxPrice, int? rating);
        Task<Product> GetProductBySlugAsync(string slug);
        Task<Store> GetStoreFrontAsync(int? id, int page, string search, string category, string sort);
        Task<List<string>> GetStoreCategoriesAsync(int storeId);
    }
}
