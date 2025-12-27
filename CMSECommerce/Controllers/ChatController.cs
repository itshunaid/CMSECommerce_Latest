using CMSECommerce.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


namespace CMSECommerce.Controllers
{
    public class ChatController : Controller
    {
        private readonly DataContext _context;

        public ChatController(DataContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(currentUserId)) return Challenge();

            // 1. Bridge Buyers and Sellers via OrderDetails
            var sellerIds = await _context.OrderDetails
                .Where(od => od.Order.UserId == currentUserId && od.ProductOwner != null)
                .Select(od => od.ProductOwner).Distinct().ToListAsync();

            var buyerIds = await _context.OrderDetails
                .Where(od => od.ProductOwner == currentUserId && od.Order.UserId != null)
                .Select(od => od.Order.UserId).Distinct().ToListAsync();

            var contactIdList = sellerIds.Union(buyerIds).Where(id => id != currentUserId).ToList();

            // 2. Fetch User Profiles
            var rawProfiles = await _context.UserProfiles
                .Where(up => contactIdList.Contains(up.UserId))
                .Select(up => new { up.UserId, up.FirstName, up.LastName })
                .ToListAsync();

            // 3. Get Unread Counts
            var rawUnreadData = await _context.ChatMessages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead && contactIdList.Contains(m.SenderId))
                .Select(m => m.SenderId).ToListAsync();

            var unreadMap = rawUnreadData.GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());

            // 4. Get Last Message Previews
            var lastMessages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && contactIdList.Contains(m.ReceiverId)) ||
                            (m.ReceiverId == currentUserId && contactIdList.Contains(m.SenderId)))
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // 5. Build ViewModel dynamically
            var contactList = rawProfiles.Select(up => {
                var lastMsg = lastMessages.FirstOrDefault(m => m.SenderId == up.UserId || m.ReceiverId == up.UserId);
                return new
                {
                    up.UserId,
                    FullName = $"{up.FirstName} {up.LastName}".Trim(),
                    UnreadCount = unreadMap.GetValueOrDefault(up.UserId, 0),
                    LastMessage = lastMsg?.Content ?? "No messages yet",
                    LastMessageTime = lastMsg?.Timestamp
                };
            }).OrderByDescending(x => x.LastMessageTime).ToList();

            return View(contactList);
        }

        [HttpGet]
        public async Task<IActionResult> GetChatHistory(string id)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _context.ChatMessages
                .Where(m => (m.SenderId == currentUserId && m.ReceiverId == id) ||
                            (m.SenderId == id && m.ReceiverId == currentUserId))
                .OrderBy(m => m.Timestamp)
                .Select(m => new {
                    id = m.Id,
                    content = m.Content,
                    timestamp = m.Timestamp,
                    type = m.SenderId == currentUserId ? "sent" : "received",
                    isRead = m.IsRead,
                    isFile = m.IsFile,      // Ensure this matches your DB Model
                    fileName = m.FileName   // Ensure this matches your DB Model
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest();

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Json(new { url = "/uploads/" + fileName, name = file.FileName });
        }
    }
}