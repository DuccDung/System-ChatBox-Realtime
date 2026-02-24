using System.Net.Http.Json;
using WebServer.Dtos;
using WebServer.Interfaces;
using static System.Net.Mime.MediaTypeNames;

namespace WebServer.Services
{
    public class ConversationService : IConversationService
    {
        private readonly HttpClient _http;
        public ConversationService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<ConversationMessageDto>> GetMessagesAsync(int conversationId, int meAccountId, int limit = 50)
        {
            if (conversationId <= 0 || meAccountId <= 0) return new();

            // gọi AppServer: /api/conversations/{id}/messages?me=1&limit=50
            var res = await _http.GetAsync($"api/conversations/{conversationId}/messages?me={meAccountId}&limit={limit}");
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Get messages failed. Status: {res.StatusCode}. Body: {err}");
            }

            var data = await res.Content.ReadFromJsonAsync<List<ConversationMessageDto>>();
            return data ?? new();
        }

        public async Task<List<ConversationThreadDto>> GetThreadsAsync(int accountId)
        {
            if (accountId <= 0) return new List<ConversationThreadDto>();

            // gọi ApplicationServer
            ///api/conversations/threads?accountId=1
            var res = await _http.GetAsync($"api/conversations/threads?accountId={accountId}");
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Get threads failed. Status: {res.StatusCode}. Body: {err}");
            }

            var data = await res.Content.ReadFromJsonAsync<List<ConversationThreadDto>>();
            return data ?? new List<ConversationThreadDto>();
        }

        /// NEW: POST gửi text lên AppServer
        public async Task<ConversationMessageDto> SendTextMessageAsync(
            int conversationId,
            int senderId,
            string content,
            int? parentMessageId = null)
        {
            if (conversationId <= 0) throw new ArgumentException("conversationId is required.");
            if (senderId <= 0) throw new ArgumentException("senderId is required.");
            if (string.IsNullOrWhiteSpace(content)) throw new ArgumentException("content is required.");

            // body đúng như bạn yêu cầu:
            // POST https://localhost:7231/api/conversations/3/messages
            // { "senderId": 1, "content": "hello", "parentMessageId": null }
            var body = new
            {
                senderId,
                content,
                parentMessageId
            };

            var res = await _http.PostAsJsonAsync($"api/conversations/{conversationId}/messages", body);
            if (!res.IsSuccessStatusCode)
            {
                var err = await res.Content.ReadAsStringAsync();
                throw new Exception($"Send message failed. Status: {res.StatusCode}. Body: {err}");
            }

            var created = await res.Content.ReadFromJsonAsync<ConversationMessageDto>();
            if (created == null) throw new Exception("Send message succeeded but response body is empty.");
            return created;
        }
    }
}