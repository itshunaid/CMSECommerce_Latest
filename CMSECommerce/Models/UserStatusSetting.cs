namespace CMSECommerce.Models
{
 public class UserStatusSetting
 {
 public int Id { get; set; }
 // Threshold in minutes to consider a user online
 public int OnlineThresholdMinutes { get; set; } =5;
 // Threshold in minutes after which a user is considered stale and will be marked offline
 public int CleanupThresholdMinutes { get; set; } =60;
 }
}
