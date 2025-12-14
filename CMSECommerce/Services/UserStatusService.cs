using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMSECommerce.Services
{
 public interface IUserStatusService
 {
 Task<List<UserStatusDTO>> GetAllOtherUsersStatusAsync(string excludeUserId = null);
 Task UpdateActivityAsync(string userId);
 Task SetOfflineAsync(string userId);
 Task<(int onlineThreshold, int cleanupThreshold)> GetThresholdsAsync();
 }

 public class UserStatusService : IUserStatusService
 {
 private readonly DataContext _context;
 private readonly UserManager<IdentityUser> _userManager;
 private readonly int _defaultOnlineThresholdMinutes;
 private readonly int _defaultCleanupThresholdMinutes;

 public UserStatusService(DataContext context, UserManager<IdentityUser> userManager, IConfiguration config)
 {
 _context = context;
 _userManager = userManager;
 _defaultOnlineThresholdMinutes = config?.GetValue<int>("UserStatus:OnlineThresholdMinutes") ??5;
 _defaultCleanupThresholdMinutes = config?.GetValue<int>("UserStatus:CleanupThresholdMinutes") ??60;
 }

 public async Task<(int onlineThreshold, int cleanupThreshold)> GetThresholdsAsync()
 {
 var setting = await _context.UserStatusSettings.FirstOrDefaultAsync();
 if (setting != null)
 {
 return (setting.OnlineThresholdMinutes, setting.CleanupThresholdMinutes);
 }
 return (_defaultOnlineThresholdMinutes, _defaultCleanupThresholdMinutes);
 }

 public async Task<List<UserStatusDTO>> GetAllOtherUsersStatusAsync(string excludeUserId = null)
 {
 var fetchedUsers = await _userManager.Users.ToListAsync();
 var statusEntities = await _context.UserStatuses.ToListAsync();
 var thresholds = await GetThresholdsAsync();
 var recentThreshold = DateTime.UtcNow.AddMinutes(-thresholds.onlineThreshold);
 var statuses = statusEntities.ToDictionary(s => s.UserId, s => (s.IsOnline && s.LastActivity >= recentThreshold));

 var userStatusDtos = fetchedUsers
 .Where(u => u != null && u.Id != excludeUserId)
 .Select(user => new UserStatusDTO
 {
 User = user,
 IsOnline = statuses.GetValueOrDefault(user.Id, false)
 }).ToList();

 return userStatusDtos;
 }

 public async Task UpdateActivityAsync(string userId)
 {
 if (string.IsNullOrEmpty(userId)) return;
 var status = await _context.UserStatuses.FindAsync(userId);
 if (status == null)
 {
 status = new Models.UserStatusTracker { UserId = userId, IsOnline = true, LastActivity = DateTime.UtcNow };
 _context.UserStatuses.Add(status);
 }
 else
 {
 status.IsOnline = true;
 status.LastActivity = DateTime.UtcNow;
 _context.UserStatuses.Update(status);
 }
 await _context.SaveChangesAsync();
 }

 public async Task SetOfflineAsync(string userId)
 {
 if (string.IsNullOrEmpty(userId)) return;
 var status = await _context.UserStatuses.FindAsync(userId);
 if (status != null)
 {
 status.IsOnline = false;
 status.LastActivity = DateTime.UtcNow;
 _context.UserStatuses.Update(status);
 await _context.SaveChangesAsync();
 }
 }
 }
}
