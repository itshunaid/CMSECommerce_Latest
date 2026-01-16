using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductQueryService _productQueryService;
        private readonly IStoreService _storeService;
        private readonly DataContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserStatusService _userStatusService;

        public ProductService(
            IProductQueryService productQueryService,
            IStoreService storeService,
            DataContext context,
            UserManager<IdentityUser> userManager,
            IUserStatusService userStatusService)
        {
            _productQueryService = productQueryService;
            _storeService = storeService;
            _context = context;
            _userManager = userManager;
            _userStatusService = userStatusService;
        }

        public async Task<ProductListViewModel> GetProductListAsync(
            string slug,
            int page,
            string searchTerm,
            string sortOrder,
            decimal? minPrice,
            decimal? maxPrice,
            int? rating)
        {
            const int pageSize = 20;
            int pageNumber = page;

            // State variables
            string categoryName = null;
            string searchFilter = searchTerm?.Trim();
            int totalProducts = 0;
            int totalPages = 1;

            List<Product> pagedProducts = new List<Product>();
            IdentityUser currentUser = null;
            List<UserStatusDTO> allOtherUsersStatus = new List<UserStatusDTO>();
            List<string> userNamesToLookUp = new List<string>();

            var currentUserId = _userManager.GetUserId(null); // This would need to be passed from controller
            var userName = _userManager.GetUserName(null);

            try
            {
                // Fetch all categories for sidebar
                var allCategories = await _context.Categories
                    .OrderBy(c => c.Level)
                    .ToListAsync();

                // Existing order/user logic for SignalR/Chat
                List<string> customerUserNames = await _context.OrderDetails
                    .Where(p => p.ProductOwner == userName && p.IsProcessed == false)
                    .Select(p => p.Customer)
                    .Distinct()
                    .ToListAsync();

                string usernameLower = userName?.ToLower() ?? "";
                List<int> orderIds = await _context.Orders
                    .Where(o => o.UserName.ToLower().Contains(usernameLower) && o.Shipped == false)
                    .Select(p => p.Id)
                    .ToListAsync();

                List<string> distinctProductOwners = await _context.OrderDetails
                    .Where(x => orderIds.Contains(x.OrderId))
                    .Select(detail => detail.ProductOwner)
                    .Distinct()
                    .ToListAsync();

                userNamesToLookUp.AddRange(distinctProductOwners);
                userNamesToLookUp.AddRange(customerUserNames);
                userNamesToLookUp = userNamesToLookUp.Distinct().ToList();

                // Product catalog retrieval
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
                        categoryName = category.Name;
                    }
                    else
                    {
                        throw new Exception($"Category '{slug}' not found.");
                    }
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchFilter))
                {
                    string lowerSearch = searchFilter.ToLower();
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

                // Apply sorting
                products = sortOrder switch
                {
                    "price-asc" => products.OrderBy(x => (double)x.Price),
                    "price-desc" => products.OrderByDescending(x => (double)x.Price),
                    "name-asc" => products.OrderBy(x => x.Name),
                    "newest" => products.OrderByDescending(x => x.Id),
                    _ => products.OrderByDescending(x => x.Id),
                };

                // Pagination
                totalProducts = await products.CountAsync();
                totalPages = (int)Math.Ceiling((decimal)totalProducts / pageSize);

                if (pageNumber < 1) pageNumber = 1;
                if (pageNumber > totalPages && totalProducts > 0) pageNumber = totalPages;

                pagedProducts = await products
                    .Include(x => x.Category)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // User status service
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    allOtherUsersStatus = await _userStatusService.GetAllOtherUsersStatusAsync(currentUserId);
                    allOtherUsersStatus = allOtherUsersStatus
                        .Where(u => userNamesToLookUp.Contains(u.User.UserName))
                        .ToList();

                    currentUser = await _userManager.FindByIdAsync(currentUserId);
                }
            }
            catch (Exception ex)
            {
                // Log error and return empty results
                pagedProducts = new List<Product>();
            }

            return new ProductListViewModel
            {
                Products = pagedProducts,
                CategoryName = categoryName,
                CurrentSearchTerm = searchFilter,
                AllUsers = allOtherUsersStatus,
                CurrentUser = currentUser
            };
        }

        public async Task<Product> GetProductBySlugAsync(string slug)
        {
            return await _productQueryService.GetProductBySlugAsync(slug);
        }

        public async Task<Store> GetStoreFrontAsync(int? id, int page, string search, string category, string sort)
        {
            return await _storeService.GetStoreFrontAsync(id, page, search, category, sort);
        }

        public async Task<List<string>> GetStoreCategoriesAsync(int storeId)
        {
            return await _storeService.GetStoreCategoriesAsync(storeId);
        }
    }
}
