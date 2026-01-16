using CMSECommerce.DTOs;
using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
 private readonly IUnitOfWork _unitOfWork;
 private readonly UserManager<IdentityUser> _userManager;
 private readonly int _defaultOnlineThresholdMinutes;
 private readonly int _defaultCleanupThresholdMinutes;

 public UserStatusService(DataContext context, IUnitOfWork unitOfWork, UserManager<IdentityUser> userManager, IConfiguration config)
 {
 _context = context;
 _unitOfWork = unitOfWork;
 _userManager = userManager;
 _defaultOnlineThresholdMinutes = config?.GetValue<int>("UserStatus:OnlineThresholdMinutes") ??5;
 _defaultCleanupThresholdMinutes = config?.GetValue<int>("UserStatus:CleanupThresholdMinutes") ??60;
 }

 public async Task<(int onlineThreshold, int cleanupThreshold)> GetThresholdsAsync()
 {
 var setting = await _unitOfWork.Repository<UserStatusSetting>().GetAll().FirstOrDefaultAsync();
 if (setting != null)
 {
 return (setting.OnlineThresholdMinutes, setting.CleanupThresholdMinutes);
 }
 return (_defaultOnlineThresholdMinutes, _defaultCleanupThresholdMinutes);
 }

 public async Task<List<UserStatusDTO>> GetAllOtherUsersStatusAsync(string excludeUserId = null)
 {
 var fetchedUsers = await _userManager.Users.ToListAsync();
 var statusEntities = await _unitOfWork.Repository<UserStatusTracker>().GetAll().ToListAsync();
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
 var status = await _unitOfWork.Repository<UserStatusTracker>().FirstOrDefaultAsync(s => s.UserId == userId);
 if (status == null)
 {
 status = new UserStatusTracker { UserId = userId, IsOnline = true, LastActivity = DateTime.UtcNow };
 await _unitOfWork.Repository<UserStatusTracker>().AddAsync(status);
 }
 else
 {
 status.IsOnline = true;
 status.LastActivity = DateTime.UtcNow;
 _unitOfWork.Repository<UserStatusTracker>().Update(status);
 }
 await _unitOfWork.SaveChangesAsync();
 }

 public async Task SetOfflineAsync(string userId)
 {
 if (string.IsNullOrEmpty(userId)) return;
 var status = await _unitOfWork.Repository<UserStatusTracker>().FirstOrDefaultAsync(s => s.UserId == userId);
 if (status != null)
 {
 status.IsOnline = false;
 status.LastActivity = DateTime.UtcNow;
 _unitOfWork.Repository<UserStatusTracker>().Update(status);
 await _unitOfWork.SaveChangesAsync();
 }
 }
 }
}
