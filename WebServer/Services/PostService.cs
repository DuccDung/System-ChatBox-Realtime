using System.Net.Http.Json;
using WebServer.Dtos;
using WebServer.Interfaces;

namespace WebServer.Services
{
    public class PostService : IPostService
    {
        private readonly HttpClient _http;

        public PostService(HttpClient http)
        {
            _http = http;
        }

        public async Task<List<PostFeedItemDto>> GetFeedAsync(int page = 1, int pageSize = 20)
        {
            var res = await _http.GetAsync($"api/posts/feed?page={page}&pageSize={pageSize}");
            return await ReadListAsync<PostFeedItemDto>(res, "Get feed failed");
        }

        public async Task<List<PostFeedItemDto>> GetUserPostsAsync(int accountId, int page = 1, int pageSize = 20)
        {
            var res = await _http.GetAsync($"api/posts/user/{accountId}?page={page}&pageSize={pageSize}");
            return await ReadListAsync<PostFeedItemDto>(res, "Get user posts failed");
        }

        public async Task<PostDetailDto> GetPostDetailAsync(int postId)
        {
            var res = await _http.GetAsync($"api/posts/{postId}");
            return await ReadSingleAsync<PostDetailDto>(res, "Get post detail failed");
        }

        public async Task<PostDetailDto> CreatePostAsync(CreatePostRequest request)
        {
            var res = await _http.PostAsJsonAsync("api/posts", request);
            return await ReadSingleAsync<PostDetailDto>(res, "Create post failed");
        }

        public async Task<PostDetailDto> UpdatePostAsync(int postId, UpdatePostRequest request)
        {
            var res = await _http.PutAsJsonAsync($"api/posts/{postId}", request);
            return await ReadSingleAsync<PostDetailDto>(res, "Update post failed");
        }

        public async Task DeletePostAsync(int postId)
        {
            var res = await _http.DeleteAsync($"api/posts/{postId}");
            await EnsureSuccessAsync(res, "Delete post failed");
        }

        public async Task<LikeResponseDto> LikePostAsync(int postId)
        {
            var res = await _http.PostAsync($"api/posts/{postId}/like", null);
            return await ReadSingleAsync<LikeResponseDto>(res, "Like post failed");
        }

        public async Task<LikeResponseDto> UnlikePostAsync(int postId)
        {
            var res = await _http.DeleteAsync($"api/posts/{postId}/like");
            return await ReadSingleAsync<LikeResponseDto>(res, "Unlike post failed");
        }

        public async Task<List<PostLikeUserDto>> GetLikesAsync(int postId)
        {
            var res = await _http.GetAsync($"api/posts/{postId}/likes");
            return await ReadListAsync<PostLikeUserDto>(res, "Get likes failed");
        }

        public async Task<List<CommentDto>> GetCommentsAsync(int postId)
        {
            var res = await _http.GetAsync($"api/posts/{postId}/comments");
            return await ReadListAsync<CommentDto>(res, "Get comments failed");
        }

        public async Task<CommentDto> CreateCommentAsync(int postId, CreateCommentRequest request)
        {
            var res = await _http.PostAsJsonAsync($"api/posts/{postId}/comments", request);
            return await ReadSingleAsync<CommentDto>(res, "Create comment failed");
        }

        public async Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentRequest request)
        {
            var res = await _http.PutAsJsonAsync($"api/posts/comments/{commentId}", request);
            return await ReadSingleAsync<CommentDto>(res, "Update comment failed");
        }

        public async Task DeleteCommentAsync(int commentId)
        {
            var res = await _http.DeleteAsync($"api/posts/comments/{commentId}");
            await EnsureSuccessAsync(res, "Delete comment failed");
        }

        public async Task<ShareResponseDto> SharePostAsync(int postId)
        {
            var res = await _http.PostAsync($"api/posts/{postId}/share", null);
            return await ReadSingleAsync<ShareResponseDto>(res, "Share post failed");
        }

        public async Task<ShareResponseDto> UnsharePostAsync(int postId)
        {
            var res = await _http.DeleteAsync($"api/posts/{postId}/share");
            return await ReadSingleAsync<ShareResponseDto>(res, "Unshare post failed");
        }

        public async Task<List<PostShareUserDto>> GetSharesAsync(int postId)
        {
            var res = await _http.GetAsync($"api/posts/{postId}/shares");
            return await ReadListAsync<PostShareUserDto>(res, "Get shares failed");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, string prefix)
        {
            if (response.IsSuccessStatusCode)
                return;

            var err = await response.Content.ReadAsStringAsync();
            throw new Exception($"{prefix}. Status: {response.StatusCode}. Body: {err}");
        }

        private static async Task<T> ReadSingleAsync<T>(HttpResponseMessage response, string prefix)
        {
            await EnsureSuccessAsync(response, prefix);
            var data = await response.Content.ReadFromJsonAsync<T>();
            if (data == null)
                throw new Exception($"{prefix}. Response body is empty.");
            return data;
        }

        private static async Task<List<T>> ReadListAsync<T>(HttpResponseMessage response, string prefix)
        {
            await EnsureSuccessAsync(response, prefix);
            return await response.Content.ReadFromJsonAsync<List<T>>() ?? new List<T>();
        }
    }
}
