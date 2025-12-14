using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using CMSECommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Services;

namespace CMSECommerce.Hubs
{
 [Authorize]
 public class ChatHub : Hub
 {
 private readonly DataContext _context;
 private readonly IUserStatusService _userStatusService;
 private readonly ILogger<ChatHub> _logger;
 public ChatHub(DataContext context, IUserStatusService userStatusService, ILogger<ChatHub> logger)
 {
 _context = context;
 _userStatusService = userStatusService;
 _logger = logger;
 }

 public override async Task OnConnectedAsync()
 {
 try
 {
 var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
 if (!string.IsNullOrEmpty(userId))
 {
 await _userStatusService.UpdateActivityAsync(userId);
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error updating activity on connect");
 }
 await base.OnConnectedAsync();
 }

 public override async Task OnDisconnectedAsync(Exception exception)
 {
 try
 {
 var userId = Context.UserIdentifier ?? Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
 if (!string.IsNullOrEmpty(userId))
 {
 // Mark offline on disconnect
 await _userStatusService.UpdateActivityAsync(userId); // ensure LastActivity updated
 var status = await _context.UserStatuses.FindAsync(userId);
 if (status != null)
 {
 status.IsOnline = false;
 status.LastActivity = DateTime.UtcNow;
 await _context.SaveChangesAsync();
 }
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error updating activity on disconnect");
 }
 await base.OnDisconnectedAsync(exception);
 }

 // Broadcast to everyone (one-to-all)
 public async Task SendMessageToAll(string message)
 {
 var senderId = Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "Unknown";
 var senderName = Context.User?.Identity?.Name ?? "Unknown";
 var chat = new ChatMessage { SenderId = senderId, SenderName = senderName, MessageContent = message };
 _context.ChatMessages.Add(chat);
 await _context.SaveChangesAsync();
 await Clients.All.SendAsync("ReceiveMessage", senderName, message);
 }

 // Send a private message to a specific user (by user id)
 public async Task SendPrivateMessage(string recipientUserId, string message)
 {
 var senderId = Context.UserIdentifier ?? Context.User?.Identity?.Name ?? "Unknown";
 var senderName = Context.User?.Identity?.Name ?? "Unknown";
 var chat = new ChatMessage
 {
 SenderId = senderId,
 SenderName = senderName,
 RecipientId = recipientUserId,
 MessageContent = message,
 IsRead = false
 };
 _context.ChatMessages.Add(chat);
 await _context.SaveChangesAsync();

 // Send a private event that includes sender id so recipient can auto-open chat
 await Clients.User(recipientUserId).SendAsync("ReceivePrivateMessage", senderId, senderName, message);

 // We DO NOT echo to caller via ReceiveMessage to avoid duplicate local display
 }

 // Mark messages between two users as read (when recipient opens chat)
 public async Task MarkMessagesRead(string otherUserId)
 {
 var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name;
 var toMark = await _context.ChatMessages.Where(m => m.SenderId == otherUserId && m.RecipientId == userId && !m.IsRead).ToListAsync();
 if (toMark.Any()) {
 foreach(var m in toMark) m.IsRead = true;
 await _context.SaveChangesAsync();
 }
 }

 // Return recent messages for a chat (user-to-user or group)
 public async Task GetRecentMessages(string targetId, bool isGroup)
 {
 if (isGroup)
 {
 var msgs = await _context.ChatMessages
 .Where(m => m.GroupName == targetId)
 .OrderBy(m => m.Timestamp)
 .Take(50)
 .Select(m => new { m.SenderName, m.MessageContent, m.Timestamp })
 .ToListAsync();
 await Clients.Caller.SendAsync("LoadHistory", msgs);
 }
 else
 {
 var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name;
 var msgs = await _context.ChatMessages
 .Where(m => (m.SenderId == userId && m.RecipientId == targetId) || (m.SenderId == targetId && m.RecipientId == userId))
 .OrderBy(m => m.Timestamp)
 .Take(50)
 .Select(m => new { m.SenderId, m.SenderName, m.MessageContent, m.Timestamp, m.IsRead })
 .ToListAsync();
 await Clients.Caller.SendAsync("LoadHistory", msgs);
 }
 }
 }
}
