using CMSECommerce.Models.ViewModels;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface ICartService
    {
        Task<CartViewModel> GetCartAsync();
        Task AddToCartAsync(int productId);
        Task UpdateCartQuantityAsync(int productId, int quantity);
        Task DecreaseCartQuantityAsync(int productId);
        Task RemoveFromCartAsync(int productId);
        Task ClearCartAsync();
    }
}
