using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class ProductQueryService : IProductQueryService
    {
        private readonly DataContext _context;

        public ProductQueryService(DataContext context)
        {
            _context = context;
        }

        public async Task<IQueryable<Product>> GetFilteredProductsAsync(string slug, string searchTerm, decimal? minPrice, decimal? maxPrice, int? rating)
        {
            IQueryable<Product> products = _context.Products
                .Where(x => x.Status == ProductStatus.Approved && x.IsVisible)
                .AsNoTracking();

            // Apply category filter
            if (!string.IsNullOrWhiteSpace(slug))
            {
                var category = await _context.Categories
                    .Where(x => x.Slug == slug)
                    .AsNoTracking()
                    .FirstOrDefaultAsync();

                if (category != null)
                {
                    products = products.Where(x => x.CategoryId == category.Id);
                }
                else
                {
                    throw new Exception($"Category '{slug}' not found.");
                }
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearch = searchTerm.ToLower();
                products = products.Where(x => x.Name.ToLower().Contains(lowerSearch) ||
                                             x.Description.ToLower().Contains(lowerSearch));
            }

            // Apply price filters
            if (minPrice.HasValue)
            {
                products = products.Where(x => x.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                products = products.Where(x => x.Price <= maxPrice.Value);
            }

            // Apply rating filter
            if (rating.HasValue && rating.Value > 0)
            {
                products = products.Where(x => x.Reviews.Any() &&
                                             x.Reviews.Average(r => r.Rating) >= rating.Value);
            }

            return products;
        }

        public async Task<Product> GetProductBySlugAsync(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
            {
                throw new ArgumentException("Invalid product identifier.");
            }

            var product = await _context.Products
                .Where(x => x.Slug == slug)
                .Include(x => x.Category)
                .Include(x => x.Reviews)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (product == null)
            {
                throw new Exception($"The product with slug '{slug}' was not found.");
            }

            // Load gallery images
            string galleryDir = Path.Combine("wwwroot", "media", "gallery", product.Id.ToString());
            if (Directory.Exists(galleryDir))
            {
                try
                {
                    product.GalleryImages = Directory.EnumerateFiles(galleryDir)
                        .Select(x => Path.GetFileName(x))
                        .ToList();
                }
                catch (IOException)
                {
                    product.GalleryImages = new List<string>();
                }
            }
            else
            {
                product.GalleryImages = new List<string>();
            }

            return product;
        }
    }
}
