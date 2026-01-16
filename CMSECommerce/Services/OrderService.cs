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
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrderService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _unitOfWork.Repository<Order>()
                .Find(o => o.UserId == userId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId, string userId)
        {
            return await _unitOfWork.Repository<Order>()
                .Find(o => o.Id == orderId && o.UserId == userId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync();
        }

        public async Task CancelOrderAsync(int orderId, string userId, string reason)
        {
            var order = await _unitOfWork.Repository<Order>()
                .Find(o => o.Id == orderId && o.UserId == userId)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync();

            if (order == null) throw new Exception("Order not found.");
            if (order.Shipped) throw new Exception("Cannot cancel shipped order.");

            order.IsCancelled = true;
            foreach (var detail in order.OrderDetails)
            {
                detail.IsCancelled = true;
                detail.CancellationReason = reason;
                detail.CancelledByRole = "Customer";
            }

            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReActivateOrderAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Repository<Order>()
                .Find(o => o.Id == orderId && o.UserId == userId)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync();

            if (order == null) throw new Exception("Order not found.");
            if (!order.IsCancelled) throw new Exception("Order is not cancelled.");

            order.IsCancelled = false;
            foreach (var detail in order.OrderDetails)
            {
                detail.IsCancelled = false;
                detail.CancellationReason = null;
                detail.CancelledByRole = null;
            }

            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReOrderAsync(int orderId, string userId)
        {
            var order = await _unitOfWork.Repository<Order>()
                .Find(o => o.Id == orderId && o.UserId == userId)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync();

            if (order == null) throw new Exception("Order not found.");

            var newOrder = new Order
            {
                UserId = userId,
                UserName = order.UserName,
                PhoneNumber = order.PhoneNumber,
                GrandTotal = order.GrandTotal,
                OrderDate = DateTime.Now,
                Shipped = false
            };

            await _unitOfWork.Repository<Order>().AddAsync(newOrder);
            await _unitOfWork.SaveChangesAsync();

            foreach (var detail in order.OrderDetails)
            {
                var newDetail = new OrderDetail
                {
                    OrderId = newOrder.Id,
                    ProductId = detail.ProductId,
                    ProductName = detail.ProductName,
                    Quantity = detail.Quantity,
                    Price = detail.Price,
                    Image = detail.Image,
                    ProductOwner = detail.ProductOwner,
                    Customer = detail.Customer,
                    CustomerNumber = detail.CustomerNumber,
                    IsProcessed = false
                };

                await _unitOfWork.Repository<OrderDetail>().AddAsync(newDetail);
            }

            await _unitOfWork.SaveChangesAsync();
        }

        public async Task CancelItemAsync(int orderDetailId, string userId, string reason)
        {
            var orderDetail = await _unitOfWork.Repository<OrderDetail>()
                .Find(od => od.Id == orderDetailId)
                .Include(od => od.Order)
                .FirstOrDefaultAsync();

            if (orderDetail == null) throw new Exception("Order detail not found.");
            if (orderDetail.Order.UserId != userId) throw new Exception("Unauthorized.");
            if (orderDetail.Order.Shipped) throw new Exception("Cannot cancel item from shipped order.");

            orderDetail.IsCancelled = true;
            orderDetail.CancellationReason = reason;
            orderDetail.CancelledByRole = "Customer";

            _unitOfWork.Repository<OrderDetail>().Update(orderDetail);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task ReturnItemAsync(int orderDetailId, int orderId, string reason)
        {
            var orderDetail = await _unitOfWork.Repository<OrderDetail>()
                .Find(od => od.Id == orderDetailId && od.OrderId == orderId)
                .Include(od => od.Order)
                .FirstOrDefaultAsync();

            if (orderDetail == null) throw new Exception("Order detail not found.");
            if (!orderDetail.Order.Shipped) throw new Exception("Cannot return item from unshipped order.");

            orderDetail.IsReturned = true;
            orderDetail.ReturnReason = reason;

            _unitOfWork.Repository<OrderDetail>().Update(orderDetail);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateOrderShippedStatusAsync(string userId)
        {
            var orders = await _unitOfWork.Repository<Order>()
                .Find(o => o.UserId == userId && !o.Shipped && !o.IsCancelled)
                .Include(o => o.OrderDetails)
                .ToListAsync();

            foreach (var order in orders)
            {
                if (order.OrderDetails.All(od => od.IsProcessed || od.IsCancelled))
                {
                    order.Shipped = true;
                    _unitOfWork.Repository<Order>().Update(order);
                }
            }

            await _unitOfWork.SaveChangesAsync();
        }
    }
}
