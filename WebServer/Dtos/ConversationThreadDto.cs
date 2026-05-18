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
        [JsonPropertyName("isGroup")]
        public bool IsGroup { get; set; }
        [JsonPropertyName("memberCount")]
        public int MemberCount { get; set; }
        [JsonPropertyName("currentUserRole")]
        public string CurrentUserRole { get; set; } = "member";
        [JsonPropertyName("canManageMembers")]
        public bool CanManageMembers { get; set; }
    }
    public class ConversationMessageDto
    {
        public int MessageId { get; set; }
        public int ConversationId { get; set; }
        public string Content { get; set; } = "";
        public string MessageType { get; set; } = "text";
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public bool IsRemove { get; set; }
        public int? ParentMessageId { get; set; }
        public SenderDto Sender { get; set; } = new SenderDto();
    }

    public class SenderDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhotoPath { get; set; }
    }
    public class SendMessageRequest
    {
        public int ConversationId { get; set; }
        public string? Content { get; set; }
        public int? ParentMessageId { get; set; }
    }
    public class SendImageUploadRequest
    {
        public int ConversationId { get; set; }
        public IFormFile File { get; set; } = default!;
        public int? ParentMessageId { get; set; }
    }
    public class SendAudioUploadRequest
    {
        public int ConversationId { get; set; }
        public IFormFile File { get; set; } = default!;
        public int? ParentMessageId { get; set; }
    }
    public class PeerDto
    {
        public int AccountId { get; set; }
        public string? AccountName { get; set; }
        public string? Email { get; set; }
        public string? PhotoPath { get; set; }
    }
    public class ConversationPeerResponseDto
    {
        public PeerDto Me { get; set; } = default!;
        public PeerDto Peer { get; set; } = default!;
    }

    public class CreateGroupConversationRequest
    {
        public string? Title { get; set; }
        public List<int> MemberIds { get; set; } = new();
    }

    public class ConversationDto
    {
        public int ConversationId { get; set; }
        public bool IsGroup { get; set; }
        public string? Title { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? LeaderAccountId { get; set; }
        public int MemberCount { get; set; }
    }

    public class ConversationMemberDto
    {
        public int ConversationMemberId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhotoPath { get; set; }
        public DateTime? JoinedAt { get; set; }
        public string Role { get; set; } = "member";
        public bool IsLeader { get; set; }
    }

    public class ConversationMembersResponseDto
    {
        public int ConversationId { get; set; }
        public bool IsGroup { get; set; }
        public string? Title { get; set; }
        public int LeaderAccountId { get; set; }
        public string CurrentUserRole { get; set; } = "member";
        public bool CanManageMembers { get; set; }
        public List<ConversationMemberDto> Members { get; set; } = new();
    }
}
