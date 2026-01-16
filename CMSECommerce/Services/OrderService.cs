using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
    public class OrderService : IOrderService
    {
        private readonly DataContext _context;

        public OrderService(DataContext context)
        {
            _context = context;
        }

        public async Task<List<Order>> GetUserOrdersAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Id)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int orderId, string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == orderId && x.UserId == userId);
        }

        public async Task CancelOrderAsync(int orderId, string userId, string reason)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) throw new Exception("Order not found.");

            // Time validation
            var timeLimit = order.OrderDate.Value.AddHours(24);
            if (DateTime.Now > timeLimit)
            {
                throw new Exception("Cancellation period has expired.");
            }

            // Business logic
            if (order.Shipped)
            {
                throw new Exception("Shipped orders cannot be cancelled.");
            }

            // Update order and items
            order.IsCancelled = true;
            foreach (var item in order.OrderDetails)
            {
                item.IsCancelled = true;
                item.CancellationReason = reason ?? "Cancelled by customer";
                item.CancelledByRole = "Customer";
                item.IsProcessed = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ReActivateOrderAsync(int orderId, string userId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) throw new Exception("Order not found.");

            if (!order.IsCancelled)
            {
                throw new Exception("This order is already active.");
            }

            order.IsCancelled = false;
            order.OrderDate = DateTime.Now;

            foreach (var item in order.OrderDetails)
            {
                item.IsCancelled = false;
                item.CancellationReason = null;
                item.CancelledByRole = null;
                item.IsProcessed = false;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ReOrderAsync(int orderId, string userId)
        {
            var oldOrder = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (oldOrder == null) throw new Exception("Original order not found.");

            if (oldOrder.IsCancelled)
            {
                // Re-activate existing order
                oldOrder.IsCancelled = false;
                oldOrder.OrderDate = DateTime.Now;
                oldOrder.Shipped = false;
                oldOrder.ShippedDate = null;

                foreach (var item in oldOrder.OrderDetails)
                {
                    item.IsCancelled = false;
                    item.IsProcessed = false;
                    item.IsReturned = false;
                }

                _context.Update(oldOrder);
            }
            else
            {
                // Create new duplicate order
                var newOrderDetails = oldOrder.OrderDetails.Select(item => new OrderDetail
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    Image = item.Image,
                    ProductOwner = item.ProductOwner,
                    IsProcessed = false,
                    IsCancelled = false,
                    IsReturned = false
                }).ToList();

                var newOrder = new Order
                {
                    UserId = userId,
                    UserName = oldOrder.UserName,
                    OrderDate = DateTime.Now,
                    GrandTotal = oldOrder.GrandTotal,
                    Shipped = false,
                    IsCancelled = false,
                    OrderDetails = newOrderDetails
                };

                _context.Orders.Add(newOrder);
            }

            await _context.SaveChangesAsync();
        }

        public async Task CancelItemAsync(int orderDetailId, string userId, string reason)
        {
            var detail = await _context.OrderDetails
                .Include(od => od.Order)
                .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId);

            if (detail == null) throw new Exception("Order detail not found.");

            // Time validation
            var timeLimit = detail.Order.OrderDate.Value.AddHours(24);
            if (DateTime.Now > timeLimit)
            {
                throw new Exception("Cancellation period has expired.");
            }

            // Permission check
            if (detail.Customer != userId && detail.ProductOwner != userId)
            {
                throw new Exception("Unauthorized to cancel this item.");
            }

            // Apply cancellation
            detail.IsCancelled = true;
            detail.CancellationReason = reason;
            detail.CancelledByRole = detail.Customer == userId ? "User" : "Seller";

            detail.Order.GrandTotal -= (detail.Price * detail.Quantity);

            if (detail.Order.OrderDetails.All(od => od.IsCancelled))
            {
                detail.Order.IsCancelled = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ReturnItemAsync(int orderDetailId, int orderId, string reason)
        {
            var detail = await _context.OrderDetails.FindAsync(orderDetailId);
            var order = await _context.Orders.FindAsync(orderId);

            if (detail == null || order == null)
            {
                throw new Exception("Order information not found.");
            }

            detail.IsReturned = true;
            detail.ReturnReason = reason;
            detail.ReturnDate = DateTime.Now;
            detail.IsCancelled = false;
            detail.CancellationReason = "Returned: " + reason;
            detail.IsProcessed = false;

            order.Shipped = false;
            order.IsCancelled = false;
            order.ShippedDate = null;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrderShippedStatusAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return;

            try
            {
                var ordersToUpdate = await _context.Orders
                    .Where(o => o.UserId == userId && !o.Shipped)
                    .Where(o => o.OrderDetails.Any() && o.OrderDetails.All(d => d.IsProcessed))
                    .ToListAsync();

                if (ordersToUpdate.Any())
                {
                    foreach (var order in ordersToUpdate)
                    {
                        order.Shipped = true;
                        _context.Entry(order).Property(x => x.Shipped).IsModified = true;
                    }

                    await _context.SaveChangesAsync();
                }
            }
            catch { }
        }
    }
}
