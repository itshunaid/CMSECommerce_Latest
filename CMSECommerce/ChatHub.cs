using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using CMSECommerce.Infrastructure;


namespace CMSECommerce // Best practice to put Hubs in their own folder
{
    public class ChatHub : Hub
    {
        private readonly DataContext _context;

        public ChatHub(DataContext context) => _context = context;

        public async Task SendPrivateMessage(string receiverId, string message, int? orderId = null)
        {
            var senderId = Context.UserIdentifier;

            if (string.IsNullOrEmpty(senderId)) return;

            // 1. Save to Database for persistence
            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = message,
                Timestamp = DateTime.UtcNow,
                OrderId = orderId, // Changed from ProductId to match your model
                IsRead = false
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            // 2. Push to the receiver in real-time
            // We pass the senderId, content, and the actual DB timestamp
            await Clients.User(receiverId).SendAsync("ReceiveMessage",
                senderId,
                message,
                chatMsg.Timestamp);

            // 3. Confirm to the sender it was sent
            await Clients.Caller.SendAsync("MessageSent", receiverId, message);
        }

        // Presence: Notify contacts when this user comes online
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.Others.SendAsync("UserStatusChanged", userId, true);
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                await Clients.Others.SendAsync("UserStatusChanged", userId, false);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendTypingNotification(string receiverId, bool isTyping)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId)) return;

            // Send only to the specific person being typed to
            await Clients.User(receiverId).SendAsync("UserTyping", senderId, isTyping);
        }

        public async Task MarkAsRead(string senderId)
        {
            var currentUserId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(currentUserId)) return;

            // 1. Update Database
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.SenderId == senderId && m.ReceiverId == currentUserId && !m.IsRead)
                .ToListAsync();

            if (unreadMessages.Any())
            {
                unreadMessages.ForEach(m => m.IsRead = true);
                await _context.SaveChangesAsync();

                // 2. Notify the original sender that their messages were seen
                await Clients.User(senderId).SendAsync("MessagesRead", currentUserId);
            }
        }

        public async Task DeleteMessage(int messageId)
        {
            var currentUserId = Context.UserIdentifier;
            var message = await _context.ChatMessages.FindAsync(messageId);

            if (message != null && message.SenderId == currentUserId)
            {
                _context.ChatMessages.Remove(message);
                await _context.SaveChangesAsync();

                // Notify both sender and receiver to remove the bubble from UI
                await Clients.Users(message.SenderId, message.ReceiverId)
                    .SendAsync("MessageDeleted", messageId);
            }
        }

        public async Task SendFileMessage(string receiverId, string fileUrl, string fileName, bool isImage)
        {
            var senderId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(senderId)) return;

            var chatMsg = new ChatMessage
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Content = fileUrl, // Store the path to the file
                Timestamp = DateTime.UtcNow,
                IsRead = false,
                IsFile = true,     // You'll need to add this property to your ChatMessage model
                FileName = fileName
            };

            _context.ChatMessages.Add(chatMsg);
            await _context.SaveChangesAsync();

            // Broadcast to receiver
            await Clients.User(receiverId).SendAsync("ReceiveMessage", senderId, fileUrl, chatMsg.Timestamp, true, fileName);
        }
    }
}