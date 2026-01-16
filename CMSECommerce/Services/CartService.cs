using CMSECommerce.Infrastructure;
using CMSECommerce.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class CartService : ICartService
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(DataContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<CartViewModel> GetCartAsync()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var cart = session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();

            if (cart.Count == 0)
            {
                return new CartViewModel { CartItems = new List<CartItem>(), GrandTotal = 0 };
            }

            var productIds = cart.Select(c => c.ProductId).Distinct().ToList();

            var productLookup = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.StockQuantity,
                    p.Image,
                    p.Slug,
                    DisplayName = _context.UserProfiles
                        .Where(up => up.UserId == p.UserId)
                        .Select(up => up.Store.StoreName)
                        .FirstOrDefault() ?? p.User.UserName
                })
                .ToDictionaryAsync(x => x.Id);

            foreach (var item in cart)
            {
                if (productLookup.TryGetValue(item.ProductId, out var dbProduct))
                {
                    item.Image = dbProduct.Image;
                    item.ProductSlug = dbProduct.Slug;
                    item.SellerName = dbProduct.DisplayName;

                    if (item.Quantity > dbProduct.StockQuantity)
                    {
                        item.IsOutOfStock = true;
                        item.Quantity = Math.Max(1, dbProduct.StockQuantity);
                    }
                    else
                    {
                        item.IsOutOfStock = dbProduct.StockQuantity <= 0;
                    }
                }
                else
                {
                    item.IsOutOfStock = true;
                }
            }

            session.SetJson("Cart", cart);

            return new CartViewModel
            {
                CartItems = cart,
                GrandTotal = cart.Sum(x => x.Price * x.Quantity)
            };
        }

        public async Task AddToCartAsync(int productId)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null) throw new Exception("Product not found.");

            var session = _httpContextAccessor.HttpContext.Session;
            var cart = session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (cartItem == null)
            {
                cart.Add(new CartItem(product));
            }
            else
            {
                cartItem.Quantity += 1;
            }

            session.SetJson("Cart", cart);
        }

        public async Task UpdateCartQuantityAsync(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                await RemoveFromCartAsync(productId);
                return;
            }

            var session = _httpContextAccessor.HttpContext.Session;
            var cart = session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var cartItem = cart.FirstOrDefault(x => x.ProductId == productId);

            if (cartItem == null) throw new Exception("Item not found in cart.");

            var productInfo = await _context.Products
                .Where(p => p.Id == productId)
                .Select(p => new { p.StockQuantity, p.Name })
                .FirstOrDefaultAsync();

            if (productInfo != null)
            {
                if (quantity > productInfo.StockQuantity)
                {
                    cartItem.Quantity = productInfo.StockQuantity;
                    cartItem.IsOutOfStock = productInfo.StockQuantity == 0;
                }
                else
                {
                    cartItem.Quantity = quantity;
                    cartItem.IsOutOfStock = false;
                }
            }
            else
            {
                throw new Exception("Product no longer available.");
            }

            session.SetJson("Cart", cart);
        }

        public async Task DecreaseCartQuantityAsync(int productId)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var cart = session.GetJson<List<CartItem>>("Cart");

            if (cart == null) return;

            var cartItem = cart.FirstOrDefault(x => x.ProductId == productId);
            if (cartItem != null)
            {
                if (cartItem.Quantity > 1)
                {
                    cartItem.Quantity -= 1;
                }
                else
                {
                    cart.RemoveAll(x => x.ProductId == productId);
                }
            }

            if (cart.Count == 0)
            {
                session.Remove("Cart");
            }
            else
            {
                session.SetJson("Cart", cart);
            }
        }

        public async Task RemoveFromCartAsync(int productId)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var cart = session.GetJson<List<CartItem>>("Cart");

            if (cart == null) return;

            cart.RemoveAll(x => x.ProductId == productId);

            if (cart.Count == 0)
            {
                session.Remove("Cart");
            }
            else
            {
                session.SetJson("Cart", cart);
            }
        }

        public async Task ClearCartAsync()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            session.Remove("Cart");
        }
    }
}
