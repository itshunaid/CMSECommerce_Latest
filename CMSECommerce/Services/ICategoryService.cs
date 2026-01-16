using CMSECommerce.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface ICategoryService
    {
        Task<List<Category>> GetAllCategoriesAsync();
    }
}
