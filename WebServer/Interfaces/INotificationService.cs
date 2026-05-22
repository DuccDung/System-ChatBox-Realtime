using WebServer.Dtos;

namespace WebServer.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetNotificationsAsync(int limit = 30, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync();
        Task<NotificationDto?> MarkReadAsync(int notificationId);
        Task<int> MarkAllReadAsync();
        Task<List<NotificationDto>> CreateChatMessageNotificationsAsync(int conversationId, int senderId, string? messageType, string? content);
    }
}
