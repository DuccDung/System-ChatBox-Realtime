using System.Text.Json.Serialization;

namespace WebServer.Dtos
{
    public class NotificationDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("content")]
        public string Content { get; set; } = "";

        [JsonPropertyName("senderId")]
        public int SenderId { get; set; }

        [JsonPropertyName("senderName")]
        public string SenderName { get; set; } = "";

        [JsonPropertyName("senderPhotoPath")]
        public string SenderPhotoPath { get; set; } = "";

        [JsonPropertyName("consumerId")]
        public int ConsumerId { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("isRead")]
        public bool IsRead { get; set; }

        [JsonPropertyName("conversationId")]
        public int? ConversationId { get; set; }
    }

    public class NotificationUnreadCountDto
    {
        [JsonPropertyName("unreadCount")]
        public int UnreadCount { get; set; }
    }

    public class CreateChatMessageNotificationsRequest
    {
        [JsonPropertyName("conversationId")]
        public int ConversationId { get; set; }

        [JsonPropertyName("senderId")]
        public int SenderId { get; set; }

        [JsonPropertyName("messageType")]
        public string? MessageType { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class CreateNotificationsResponse
    {
        [JsonPropertyName("notifications")]
        public List<NotificationDto> Notifications { get; set; } = new();
    }
}
