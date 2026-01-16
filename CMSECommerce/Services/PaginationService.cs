using CMSECommerce.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class PaginationService : IPaginationService
    {
        public async Task<(List<Product> pagedProducts, int totalPages)> ApplyPaginationAsync(IQueryable<Product> products, int pageNumber, int pageSize)
        {
            int totalProducts = await products.CountAsync();
            int totalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalProducts > 0) pageNumber = totalPages;

            var pagedProducts = await products
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (pagedProducts, totalPages);
        }
    }
}
