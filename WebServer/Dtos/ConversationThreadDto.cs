using System.Text.Json.Serialization;

namespace WebServer.Dtos
{
    public class ConversationThreadDto
    {
        [JsonPropertyName("conversationId")]
        public int ConversationId { get; set; }
        [JsonPropertyName("name")]

        public string Name { get; set; } = "";
        [JsonPropertyName("avatarUrl")]
        public string AvatarUrl { get; set; } = "";
        [JsonPropertyName("snippet")]

        public string Snippet { get; set; } = "";
        [JsonPropertyName("lastMessageAt")]
        public DateTime? LastMessageAt { get; set; }
    }
}
