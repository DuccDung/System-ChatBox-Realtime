using WebServer.Dtos;

namespace WebServer.Interfaces
{
    public interface IPostService
    {
        Task<List<PostFeedItemDto>> GetFeedAsync(int page = 1, int pageSize = 20);
        Task<List<PostFeedItemDto>> GetUserPostsAsync(int accountId, int page = 1, int pageSize = 20);
        Task<PostDetailDto> GetPostDetailAsync(int postId);
        Task<PostDetailDto> CreatePostAsync(CreatePostRequest request);
        Task<PostDetailDto> UpdatePostAsync(int postId, UpdatePostRequest request);
        Task DeletePostAsync(int postId);
        Task<LikeResponseDto> LikePostAsync(int postId);
        Task<LikeResponseDto> UnlikePostAsync(int postId);
        Task<List<PostLikeUserDto>> GetLikesAsync(int postId);
        Task<List<CommentDto>> GetCommentsAsync(int postId);
        Task<CommentDto> CreateCommentAsync(int postId, CreateCommentRequest request);
        Task<CommentDto> UpdateCommentAsync(int commentId, UpdateCommentRequest request);
        Task DeleteCommentAsync(int commentId);
        Task<ShareResponseDto> SharePostAsync(int postId);
        Task<ShareResponseDto> UnsharePostAsync(int postId);
        Task<List<PostShareUserDto>> GetSharesAsync(int postId);
    }
}
