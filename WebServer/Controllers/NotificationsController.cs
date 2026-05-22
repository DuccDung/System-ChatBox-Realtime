using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Interfaces;

namespace WebServer.Controllers
{
    [Authorize]
    [Route("notifications")]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<IActionResult> GetNotifications([FromQuery] int limit = 30, [FromQuery] bool unreadOnly = false)
        {
            try
            {
                var notifications = await _notificationService.GetNotificationsAsync(limit, unreadOnly);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var unreadCount = await _notificationService.GetUnreadCountAsync();
                return Ok(new { unreadCount });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("{id:int}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            try
            {
                var notification = await _notificationService.MarkReadAsync(id);
                return notification == null
                    ? NotFound(new { message = "Notification not found." })
                    : Ok(notification);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            try
            {
                var markedCount = await _notificationService.MarkAllReadAsync();
                return Ok(new { ok = true, markedCount });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
