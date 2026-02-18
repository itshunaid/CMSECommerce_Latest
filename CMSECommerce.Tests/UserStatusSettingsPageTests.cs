using System.Threading.Tasks;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Infrastructure;
using CMSECommerce.Areas.Admin.Pages;
using CMSECommerce.Models;
using Microsoft.AspNetCore.Mvc;

namespace CMSECommerce.Tests
{
 public class UserStatusSettingsPageTests
 {
 [Fact]
 public async Task OnGetAsync_CreatesDefaultIfMissing()
 {
 var options = new DbContextOptionsBuilder<DataContext>()
 .UseInMemoryDatabase(databaseName: "TestDb_Settings_Get")
 .Options;

 await using var context = new DataContext(options);

 var page = new UserStatusSettingsModel(context);

 var result = await page.OnGetAsync();

 // Should have created default setting
 var setting = await context.UserStatusSettings.FirstOrDefaultAsync();
 Assert.NotNull(setting);
 Assert.Equal(5, setting.OnlineThresholdMinutes);
 Assert.Equal(60, setting.CleanupThresholdMinutes);

 // Page properties should be populated
 Assert.Equal(5, page.OnlineThresholdMinutes);
 Assert.Equal(60, page.CleanupThresholdMinutes);
 }

 [Fact]
 public async Task OnPostAsync_SavesUpdatedValues()
 {
 var options = new DbContextOptionsBuilder<DataContext>()
 .UseInMemoryDatabase(databaseName: "TestDb_Settings_Post")
 .Options;

 await using var context = new DataContext(options);

 // Seed an initial setting
 context.UserStatusSettings.Add(new UserStatusSetting { OnlineThresholdMinutes =3, CleanupThresholdMinutes =30 });
 await context.SaveChangesAsync();

 var page = new UserStatusSettingsModel(context)
 {
 OnlineThresholdMinutes =10,
 CleanupThresholdMinutes =120
 };

 var result = await page.OnPostAsync();

 Assert.IsType<RedirectToPageResult>(result);

 var setting = await context.UserStatusSettings.FirstOrDefaultAsync();
 Assert.NotNull(setting);
 Assert.Equal(10, setting.OnlineThresholdMinutes);
 Assert.Equal(120, setting.CleanupThresholdMinutes);
 }
 }
}
