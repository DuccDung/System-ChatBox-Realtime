using System.Net.Http.Json;
using WebServer.Dtos;
using WebServer.Interfaces;

namespace WebServer.Services
{
    public class ConversationService : IConversationService
    {
        private readonly HttpClient _http;
        public ConversationService(HttpClient http)
        {
            _http = http;
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
    }
}