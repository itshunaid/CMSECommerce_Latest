using CMSECommerce.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IPaginationService
    {
        Task<(List<Product> pagedProducts, int totalPages)> ApplyPaginationAsync(IQueryable<Product> products, int pageNumber, int pageSize);
    }
}
