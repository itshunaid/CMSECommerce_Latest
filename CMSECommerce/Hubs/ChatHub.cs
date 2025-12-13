using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using CMSECommerce.Infrastructure;
using CMSECommerce.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace CMSECommerce.Hubs
{
 [Authorize]
 public class ChatHub : Hub
 {
 private readonly DataContext _context;
 public ChatHub(DataContext context)
 {
 _context = context;
 }

 public override Task OnConnectedAsync()
 {
 return base.OnConnectedAsync();
 }

 public override Task OnDisconnectedAsync(Exception exception)
 {
 return base.OnDisconnectedAsync(exception);
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
