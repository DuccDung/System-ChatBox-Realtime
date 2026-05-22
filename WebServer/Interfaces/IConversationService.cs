using WebServer.Dtos;

namespace WebServer.Interfaces
{
    public interface IConversationService
    {
        Task<List<ConversationThreadDto>> GetThreadsAsync(int accountId);
        Task<List<ConversationMessageDto>> GetMessagesAsync(int conversationId, int meAccountId, int limit = 50);

        Task<ConversationMessageDto> SendTextMessageAsync(int conversationId, int senderId, string content, int? parentMessageId = null);
        Task<ConversationMessageDto> SendImageMessageAsync(int conversationId, int senderId, IFormFile file, int? parentMessageId);
        Task<ConversationMessageDto> SendAudioMessageAsync(int conversationId, int senderId, IFormFile file, int? parentMessageId);
        Task<ConversationPeerResponseDto?> GetPeerAsync(int conversationId, int meAccountId);
        Task<ConversationDto> CreateDirectAsync(int accountId, int friendId);
        Task<ConversationDto> CreateGroupAsync(int ownerId, string? title, List<int> memberIds);
        Task<ConversationMembersResponseDto> GetMembersAsync(int conversationId, int meAccountId);
        Task RemoveMemberAsync(int conversationId, int actorId, int memberId);
        Task<ReadReceiptDto> MarkReadAsync(int conversationId, int meAccountId);
    }
}
