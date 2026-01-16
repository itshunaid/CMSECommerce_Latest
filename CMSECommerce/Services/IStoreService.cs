using CMSECommerce.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IStoreService
    {
        Task<Store> GetStoreFrontAsync(int? id, int page, string search, string category, string sort);
        Task<List<string>> GetStoreCategoriesAsync(int storeId);
    }
}
