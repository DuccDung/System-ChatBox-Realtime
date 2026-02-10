using WebServer.Dtos;

namespace WebServer.Interfaces
{
    public interface IConversationService
    {
        Task<List<ConversationThreadDto>> GetThreadsAsync(int accountId);
    }
}
