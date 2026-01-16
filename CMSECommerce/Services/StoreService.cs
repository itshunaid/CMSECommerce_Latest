using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class StoreService : IStoreService
    {
        private readonly DataContext _context;

        public StoreService(DataContext context)
        {
            _context = context;
        }

        public async Task<Store> GetStoreFrontAsync(int? id, int page, string search, string category, string sort)
        {
            if (id == null)
            {
                throw new ArgumentException("Store ID is required.");
            }

            int pageSize = 12;

            var store = await _context.Stores
                .Include(s => s.Products.Where(p => p.Status == ProductStatus.Approved && p.IsVisible))
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
            {
                throw new Exception("Store not found.");
            }

            IQueryable<Product> productsQuery = _context.Products
                .Where(p => p.StoreId == id && p.Status == ProductStatus.Approved && p.IsVisible);

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
            }

            if (!string.IsNullOrEmpty(category))
            {
                productsQuery = productsQuery.Where(p => p.Category.Name == category);
            }

            productsQuery = sort switch
            {
                "price_asc" => productsQuery.OrderBy(p => p.Price),
                "price_desc" => productsQuery.OrderByDescending(p => p.Price),
                "newest" => productsQuery.OrderByDescending(p => p.Id),
                _ => productsQuery.OrderBy(p => p.Name)
            };

            int totalItems = await productsQuery.CountAsync();
            var pagedProducts = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            store.Products = pagedProducts;

            return store;
        }

        public async Task<List<string>> GetStoreCategoriesAsync(int storeId)
        {
            return await _context.Products
                .Where(pr => pr.StoreId == storeId)
                .Select(pr => pr.Category.Name)
                .Distinct()
                .ToListAsync();
        }
    }
}
