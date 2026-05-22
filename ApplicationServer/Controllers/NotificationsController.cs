using System.Security.Claims;
using ApplicationServer.Dtos.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApplicationServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/notifications")]
    public class NotificationsController : ControllerBase
    {
        private const string ChatMessageType = "chat.message";
        private readonly SocialNetworkContext _context;

        public NotificationsController(SocialNetworkContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationDto>>> GetMyNotifications(
            [FromQuery] int limit = 30,
            [FromQuery] bool unreadOnly = false)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Not logged in." });

            limit = Math.Clamp(limit, 1, 100);

            var query = _context.Notifications
                .AsNoTracking()
                .Where(n => n.ConsumerId == userId);

            if (unreadOnly)
                query = query.Where(n => n.IsRead != true);

            var notifications = await (
                from n in query
                join sender in _context.Accounts.AsNoTracking()
                    on n.SenderId equals sender.AccountId into senderJoin
                from sender in senderJoin.DefaultIfEmpty()
                orderby n.Date descending, n.Id descending
                select new
                {
                    Notification = n,
                    SenderName = sender == null ? null : sender.AccountName,
                    SenderPhotoPath = sender == null ? null : sender.PhotoPath
                })
                .Take(limit)
                .ToListAsync();

            return Ok(notifications.Select(x => ToDto(x.Notification, x.SenderName, x.SenderPhotoPath)).ToList());
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Not logged in." });

            var count = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.ConsumerId == userId && n.IsRead != true);

            return Ok(new { unreadCount = count });
        }

        [HttpPost("{id:int}/read")]
        public async Task<ActionResult<NotificationDto>> MarkRead(int id)
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Not logged in." });

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.ConsumerId == userId);

            if (notification == null)
                return NotFound(new { message = "Notification not found." });

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            var sender = await _context.Accounts
                .AsNoTracking()
                .Where(a => a.AccountId == notification.SenderId)
                .Select(a => new { a.AccountName, a.PhotoPath })
                .FirstOrDefaultAsync();

            return Ok(ToDto(notification, sender?.AccountName, sender?.PhotoPath));
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            if (!TryGetUserId(out var userId))
                return Unauthorized(new { message = "Not logged in." });

            var unread = await _context.Notifications
                .Where(n => n.ConsumerId == userId && n.IsRead != true)
                .ToListAsync();

            foreach (var notification in unread)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Ok(new { ok = true, markedCount = unread.Count });
        }

        [HttpPost("chat-message")]
        public async Task<ActionResult<CreateNotificationsResponse>> CreateChatMessageNotifications(
            [FromBody] CreateChatMessageNotificationsRequest request)
        {
            if (!TryGetUserId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (request == null)
                return BadRequest(new { message = "Body is required." });

            if (request.ConversationId <= 0 || request.SenderId <= 0)
                return BadRequest(new { message = "Invalid conversation or sender." });

            if (currentUserId != request.SenderId)
                return Forbid();

            var sender = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == request.SenderId);

            if (sender == null)
                return NotFound(new { message = "Sender not found." });

            var recipients = await _context.ConversationMembers
                .AsNoTracking()
                .Where(cm => cm.ConversationId == request.ConversationId && cm.AccountId != request.SenderId)
                .Select(cm => cm.AccountId)
                .Distinct()
                .ToListAsync();

            if (recipients.Count == 0)
                return Ok(new CreateNotificationsResponse());

            var now = DateTime.UtcNow;
            var notificationType = $"{ChatMessageType}:{request.ConversationId}";
            var content = BuildChatMessageContent(sender.AccountName, request.MessageType, request.Content);

            var notifications = recipients.Select(consumerId => new ApplicationServer.Notification
            {
                Type = notificationType,
                Content = content,
                SenderId = request.SenderId,
                ConsumerId = consumerId,
                Date = now,
                IsRead = false
            }).ToList();

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();

            return Ok(new CreateNotificationsResponse
            {
                Notifications = notifications
                    .Select(n => ToDto(n, sender.AccountName, sender.PhotoPath))
                    .ToList()
            });
        }

        private bool TryGetUserId(out int userId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out userId) && userId > 0;
        }

        private static NotificationDto ToDto(ApplicationServer.Notification notification, string? senderName, string? senderPhoto)
        {
            return new NotificationDto
            {
                Id = notification.Id,
                Type = NormalizeType(notification.Type),
                Content = notification.Content,
                SenderId = notification.SenderId,
                SenderName = string.IsNullOrWhiteSpace(senderName) ? "Người dùng" : senderName!,
                SenderPhotoPath = string.IsNullOrWhiteSpace(senderPhoto) ? "/assets/images/avatar-default.png" : senderPhoto!,
                ConsumerId = notification.ConsumerId,
                Date = notification.Date,
                IsRead = notification.IsRead == true,
                ConversationId = ParseConversationId(notification.Type)
            };
        }

        private static string NormalizeType(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return "";
            var separatorIndex = type.IndexOf(':');
            return separatorIndex > 0 ? type[..separatorIndex] : type;
        }

        private static int? ParseConversationId(string? type)
        {
            if (string.IsNullOrWhiteSpace(type)) return null;
            const string prefix = $"{ChatMessageType}:";
            if (!type.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;
            return int.TryParse(type[prefix.Length..], out var conversationId) && conversationId > 0
                ? conversationId
                : null;
        }

        private static string BuildChatMessageContent(string? senderName, string? messageType, string? content)
        {
            var name = string.IsNullOrWhiteSpace(senderName) ? "Người dùng" : senderName.Trim();
            var type = (messageType ?? "text").Trim().ToLowerInvariant();

            return type switch
            {
                "image" => $"{name} đã gửi một hình ảnh.",
                "audio" => $"{name} đã gửi một tin nhắn thoại.",
                _ => $"{name}: {TrimSnippet(content)}"
            };
        }

        private static string TrimSnippet(string? value)
        {
            var text = string.IsNullOrWhiteSpace(value) ? "Tin nhắn mới" : value.Trim();
            return text.Length <= 120 ? text : $"{text[..117]}...";
        }
    }
}
