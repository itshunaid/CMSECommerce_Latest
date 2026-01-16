using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IProductQueryService
    {
        Task<IQueryable<Product>> GetFilteredProductsAsync(string slug, string searchTerm, decimal? minPrice, decimal? maxPrice, int? rating);
        Task<Product> GetProductBySlugAsync(string slug);
    }
}
