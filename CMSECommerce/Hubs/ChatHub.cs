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

 // Get order-related contacts (buyers for sellers, sellers for buyers) with online status
 public async Task GetOrderContacts()
 {
 var userId = Context.UserIdentifier ?? Context.User?.Identity?.Name;
 if (string.IsNullOrEmpty(userId)) return;

 // Get all user statuses for quick lookup
 var allStatuses = await _userStatusService.GetAllOtherUsersStatusAsync(userId);
 var statusDict = allStatuses.ToDictionary(s => s.User.Id, s => s.IsOnline);

 // Find contacts based on orders
 var contacts = new List<dynamic>();

 // Check if user is a seller (has products)
 var userProducts = await _context.Products.Where(p => p.OwnerName == userId).Select(p => p.Id).ToListAsync();
 if (userProducts.Any())
 {
 // User is a seller: get buyers who ordered their products
 var buyerIds = await _context.OrderDetails
 .Where(od => userProducts.Contains(od.ProductId))
 .Select(od => od.Order.UserId)
 .Distinct()
 .ToListAsync();

 var buyers = await _context.UserProfiles
 .Where(up => buyerIds.Contains(up.UserId))
 .Select(up => new { up.UserId, up.FirstName, up.LastName })
 .ToListAsync();

 foreach (var buyer in buyers)
 {
 var fullName = $"{buyer.FirstName} {buyer.LastName}".Trim();
 contacts.Add(new
 {
 UserId = buyer.UserId,
 Name = string.IsNullOrEmpty(fullName) ? "Unknown Buyer" : fullName,
 IsOnline = statusDict.GetValueOrDefault(buyer.UserId, false)
 });
 }
 }

 // Get sellers of products the user has ordered
 var sellerIds = await _context.OrderDetails
 .Where(od => od.Order.UserId == userId)
 .Select(od => od.ProductOwner)
 .Distinct()
 .Where(s => !string.IsNullOrEmpty(s))
 .ToListAsync();

 var sellers = await _context.UserProfiles
 .Where(up => sellerIds.Contains(up.UserId))
 .Select(up => new { up.UserId, up.FirstName, up.LastName })
 .ToListAsync();

 foreach (var seller in sellers)
 {
 var fullName = $"{seller.FirstName} {seller.LastName}".Trim();
 contacts.Add(new
 {
 UserId = seller.UserId,
 Name = string.IsNullOrEmpty(fullName) ? "Unknown Seller" : fullName,
 IsOnline = statusDict.GetValueOrDefault(seller.UserId, false)
 });
 }

 // Remove duplicates (in case user is both buyer and seller with same person)
 contacts = contacts
 .GroupBy(c => c.UserId)
 .Select(g => g.First())
 .ToList();

 await Clients.Caller.SendAsync("LoadContacts", contacts);
 }
 }
}
