using Microsoft.Extensions.Options;
using CMSECommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
 _logger.LogInformation("UserStatusCleanupService started");

 while (!stoppingToken.IsCancellationRequested)
 {
 try
 {
 using (var scope = _services.CreateScope())
 {
 var context = scope.ServiceProvider.GetRequiredService<DataContext>();

 // Read thresholds from DB if present, otherwise fallback to config/options
 var dbSetting = await context.UserStatusSettings.AsNoTracking().FirstOrDefaultAsync(stoppingToken);
 int cleanupMinutes = dbSetting?.CleanupThresholdMinutes ?? _options.CleanupThresholdMinutes;

 var cutoff = DateTime.UtcNow.AddMinutes(-cleanupMinutes);

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

 // Determine delay for next run: use DB cleanupMinutes if present
 var delayMinutes = Math.Max(1, cleanupMinutes);
 await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
 }
 }
 catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
 {
 // shutdown requested
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error during UserStatusCleanupService run");
 // Sleep briefly before retrying after an error
 await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
 }
 }
 }
 }
}
