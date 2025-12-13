using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using CMSECommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CMSECommerce.Services
{
 public class UserStatusOptions
 {
 public int OnlineThresholdMinutes { get; set; } =5;
 public int CleanupThresholdMinutes { get; set; } =60;
 }

 public class UserStatusCleanupService : BackgroundService
 {
 private readonly ILogger<UserStatusCleanupService> _logger;
 private readonly IServiceProvider _services;
 private readonly UserStatusOptions _options;

 public UserStatusCleanupService(ILogger<UserStatusCleanupService> logger, IServiceProvider services, IOptions<UserStatusOptions> options)
 {
 _logger = logger;
 _services = services;
 _options = options.Value;
 }

 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
 _logger.LogInformation("UserStatusCleanupService started with cleanup minutes: {Minutes}", _options.CleanupThresholdMinutes);

 while (!stoppingToken.IsCancellationRequested)
 {
 try
 {
 using (var scope = _services.CreateScope())
 {
 var context = scope.ServiceProvider.GetRequiredService<DataContext>();

 var cutoff = DateTime.UtcNow.AddMinutes(-_options.CleanupThresholdMinutes);

 var stale = await context.UserStatuses.Where(s => s.LastActivity < cutoff && s.IsOnline).ToListAsync(stoppingToken);
 if (stale.Any())
 {
 foreach (var s in stale)
 {
 s.IsOnline = false;
 s.LastActivity = DateTime.UtcNow; // mark when we updated
 }
 await context.SaveChangesAsync(stoppingToken);
 _logger.LogInformation("Marked {Count} stale user statuses offline older than {Cutoff}", stale.Count, cutoff);
 }
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error during UserStatusCleanupService run");
 }

 await Task.Delay(TimeSpan.FromMinutes(Math.Max(1, _options.CleanupThresholdMinutes)), stoppingToken);
 }
 }
 }
}
