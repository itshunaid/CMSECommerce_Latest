using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace CMSECommerce.Services
{
    public class OrderAutoDeclineService : BackgroundService
    {
        private readonly ILogger<OrderAutoDeclineService> _logger;
        private readonly IServiceProvider _services;
        private readonly IEmailSender _emailSender;

        public OrderAutoDeclineService(ILogger<OrderAutoDeclineService> logger, IServiceProvider services, IEmailSender emailSender)
        {
            _logger = logger;
            _services = services;
            _emailSender = emailSender;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OrderAutoDeclineService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

                        // Find orders that are pending for more than 24 hours
                        var cutoff = DateTime.UtcNow.AddHours(-24);
                        var pendingOrders = await context.OrderDetails
                            .Where(od => od.Status == OrderStatus.Pending && od.LastUpdated < cutoff)
                            .ToListAsync(stoppingToken);

                        if (pendingOrders.Any())
                        {
                            foreach (var order in pendingOrders)
                            {
                                order.Status = OrderStatus.Cancelled;
                                order.IsCancelled = true;
                                order.CancelledByRole = "System";
                                order.CancellationReason = "Automatically declined due to seller not taking action within 24 hours";
                                order.LastUpdated = DateTime.UtcNow;

                                // Notify customer via email
                                var customerEmail = order.Customer; // Assuming Customer is the email
                                if (!string.IsNullOrEmpty(customerEmail))
                                {
                                    try
                                    {
                                        await _emailSender.SendEmailAsync(
                                            customerEmail,
                                            "Order Automatically Declined",
                                            $"Dear Customer,<br><br>Your order for {order.ProductName} has been automatically declined because the seller did not accept it within 24 hours.<br><br>Order ID: {order.OrderId}<br>Product: {order.ProductName}<br><br>We apologize for the inconvenience. You can place a new order or contact support for assistance.<br><br>Best regards,<br>CMSECommerce Team"
                                        );
                                        _logger.LogInformation("Notification sent to customer {Email} for auto-declined order {OrderId}", customerEmail, order.Id);
                                    }
                                    catch (Exception emailEx)
                                    {
                                        _logger.LogError(emailEx, "Failed to send notification email to {Email} for order {OrderId}", customerEmail, order.Id);
                                    }
                                }
                            }

                            await context.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Automatically declined {Count} orders older than 24 hours", pendingOrders.Count);
                        }

                        // Run every hour
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // shutdown requested
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during OrderAutoDeclineService run");
                    // Sleep briefly before retrying after an error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}
