using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CMSECommerce.Services
{
   
    public class SubscriptionExpiryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SubscriptionExpiryService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<DataContext>();
                    // CORRECT: Use your Identity User class (usually ApplicationUser or IdentityUser)
                    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

                    // 1. Find users whose subscription has expired
                    var expiredProfiles = await context.UserProfiles
                        .Where(p => p.SubscriptionEndDate.HasValue && p.SubscriptionEndDate.Value < DateTime.UtcNow)
                        .ToListAsync(stoppingToken);

                    foreach (var profile in expiredProfiles)
                    {
                        var user = await userManager.FindByIdAsync(profile.UserId);
                        if (user != null)
                        {
                            // 2. Remove Subscriber Role
                            if (await userManager.IsInRoleAsync(user, "Subscriber"))
                            {
                                await userManager.RemoveFromRoleAsync(user, "Subscriber");
                            }

                            // 3. Reset Profile stats
                            profile.CurrentTierId = null;
                            profile.CurrentProductLimit = 0;
                            profile.SubscriptionEndDate = null;
                            profile.SubscriptionStartDate = null;

                            context.UserProfiles.Update(profile);
                        }
                    }

                    if (expiredProfiles.Any())
                    {
                        await context.SaveChangesAsync(stoppingToken);
                    }
                }

                // Run once every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
