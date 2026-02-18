using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Infrastructure;
using CMSECommerce.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CMSECommerce.Tests
{
 public class UserStatusServiceTests
 {
 [Fact]
 public async Task UpdateActivity_CreatesOrUpdatesStatus()
 {
 var services = new ServiceCollection();
 services.AddDbContext<DataContext>(opts => opts.UseInMemoryDatabase("TestDb_StatusService"));
 services.AddIdentity<IdentityUser, IdentityRole>().AddEntityFrameworkStores<DataContext>();
 var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
 services.AddSingleton<IConfiguration>(config);
 services.AddScoped<UserStatusService>();
 var provider = services.BuildServiceProvider();

 var db = provider.GetRequiredService<DataContext>();
 var userManager = provider.GetRequiredService<UserManager<IdentityUser>>();
 // create a user
 var u = new IdentityUser { UserName = "testuser", Email = "test@example.com" };
 await userManager.CreateAsync(u, "Password123!");

 var svc = new UserStatusService(db, userManager, config);
 await svc.UpdateActivityAsync(u.Id);
 var s = await db.UserStatuses.FindAsync(u.Id);
 Assert.NotNull(s);
 Assert.True(s.IsOnline);
 }
 }
}
