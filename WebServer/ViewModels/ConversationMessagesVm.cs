using WebServer.Dtos;

namespace WebServer.ViewModels
{
    namespace WebServer.ViewModels
    {
        public class ConversationMessagesVm
        {
            public int ConversationId { get; set; }
            public int MeAccountId { get; set; }
            public List<ConversationMessageDto> Messages { get; set; } = new();
        }
    }
}
