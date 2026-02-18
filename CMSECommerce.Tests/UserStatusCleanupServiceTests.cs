using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Infrastructure;
using CMSECommerce.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using CMSECommerce.Models;

namespace CMSECommerce.Tests
{
 public class UserStatusCleanupServiceTests
 {
 [Fact]
 public async Task Cleanup_MarksStaleUsersOffline()
 {
 // Arrange: in-memory db
 var services = new ServiceCollection();
 services.AddDbContext<DataContext>(opts => opts.UseInMemoryDatabase("TestDb_Cleanup"));
 services.AddLogging();
 services.AddSingleton<IOptions<UserStatusOptions>>(Options.Create(new UserStatusOptions { CleanupThresholdMinutes =10 }));
 services.AddSingleton<UserStatusCleanupService>();
 var provider = services.BuildServiceProvider();

 var db = provider.GetRequiredService<DataContext>();

 // add a user status that is stale (last activity older than threshold)
 var stale = new UserStatusTracker { UserId = "u1", IsOnline = true, LastActivity = DateTime.UtcNow.AddMinutes(-30) };
 var fresh = new UserStatusTracker { UserId = "u2", IsOnline = true, LastActivity = DateTime.UtcNow };
 db.UserStatuses.AddRange(stale, fresh);
 await db.SaveChangesAsync();

 var logger = NullLogger<UserStatusCleanupService>.Instance;
 var options = provider.GetRequiredService<IOptions<UserStatusOptions>>();
 var svc = new UserStatusCleanupService(logger, provider, options);

 // Act: run one loop iteration by calling ExecuteAsync and cancel quickly
 var cts = new System.Threading.CancellationTokenSource();
 var task = svc.StartAsync(cts.Token);
 // Wait briefly to allow background work
 await Task.Delay(500);
 cts.Cancel();
 await task;

 // Assert: stale should be marked offline
 var s = await db.UserStatuses.FindAsync("u1");
 var f = await db.UserStatuses.FindAsync("u2");
 Assert.False(s.IsOnline);
 Assert.True(f.IsOnline);
 }
 }
}
