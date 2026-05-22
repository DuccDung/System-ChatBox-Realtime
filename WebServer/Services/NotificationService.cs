using System.Net.Http.Json;
using WebServer.Dtos;
using WebServer.Interfaces;

namespace WebServer.Services
{
    public class NotificationService : INotificationService
    {
        private readonly HttpClient _http;

        public NotificationService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<NotificationDto>> GetNotificationsAsync(int limit = 30, bool unreadOnly = false)
        {
            limit = Math.Clamp(limit, 1, 100);
            var res = await _http.GetAsync($"api/notifications?limit={limit}&unreadOnly={unreadOnly.ToString().ToLowerInvariant()}");

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Get notifications failed. Status: {res.StatusCode}. Body: {err}");
            }

            return await res.Content.ReadFromJsonAsync<List<NotificationDto>>() ?? new();
        }

        public async Task<int> GetUnreadCountAsync()
        {
            var res = await _http.GetAsync("api/notifications/unread-count");

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Get notification unread count failed. Status: {res.StatusCode}. Body: {err}");
            }

            var payload = await res.Content.ReadFromJsonAsync<NotificationUnreadCountDto>();
            return payload?.UnreadCount ?? 0;
        }

        public async Task<NotificationDto?> MarkReadAsync(int notificationId)
        {
            if (notificationId <= 0) return null;

            var res = await _http.PostAsync($"api/notifications/{notificationId}/read", null);

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Mark notification read failed. Status: {res.StatusCode}. Body: {err}");
            }

            return await res.Content.ReadFromJsonAsync<NotificationDto>();
        }

        public async Task<int> MarkAllReadAsync()
        {
            var res = await _http.PostAsync("api/notifications/read-all", null);

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Mark all notifications read failed. Status: {res.StatusCode}. Body: {err}");
            }

            var payload = await res.Content.ReadFromJsonAsync<MarkAllReadResponse>();
            return payload?.MarkedCount ?? 0;
        }

        public async Task<List<NotificationDto>> CreateChatMessageNotificationsAsync(
            int conversationId,
            int senderId,
            string? messageType,
            string? content)
        {
            if (conversationId <= 0 || senderId <= 0) return new();

            var res = await _http.PostAsJsonAsync("api/notifications/chat-message", new CreateChatMessageNotificationsRequest
            {
                ConversationId = conversationId,
                SenderId = senderId,
                MessageType = messageType,
                Content = content
            });

            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Create chat notifications failed. Status: {res.StatusCode}. Body: {err}");
            }

            var payload = await res.Content.ReadFromJsonAsync<CreateNotificationsResponse>();
            return payload?.Notifications ?? new();
        }

        private class MarkAllReadResponse
        {
            public bool Ok { get; set; }
            public int MarkedCount { get; set; }
        }
    }
}
