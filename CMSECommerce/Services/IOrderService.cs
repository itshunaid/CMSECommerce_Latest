using CMSECommerce.Models;
using CMSECommerce.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public interface IOrderService
    {
        Task<List<Order>> GetUserOrdersAsync(string userId);
        Task<Order> GetOrderByIdAsync(int orderId, string userId);
        Task CancelOrderAsync(int orderId, string userId, string reason);
        Task ReActivateOrderAsync(int orderId, string userId);
        Task ReOrderAsync(int orderId, string userId);
        Task CancelItemAsync(int orderDetailId, string userId, string reason);
        Task ReturnItemAsync(int orderDetailId, int orderId, string reason);
        Task UpdateOrderShippedStatusAsync(string userId);
    }
}
