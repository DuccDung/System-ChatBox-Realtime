namespace WebServer.Dtos
{
    public class CreatePostMediaRequest
    {
        public string MediaUrl { get; set; } = "";
        public string MediaType { get; set; } = "image";
    }

    public class CreatePostRequest
    {
        public string? Content { get; set; }
        public string? PostType { get; set; }
        public List<CreatePostMediaRequest> Media { get; set; } = new();
    }

    public class UpdatePostRequest
    {
        public string? Content { get; set; }
        public string? PostType { get; set; }
        public List<CreatePostMediaRequest>? Media { get; set; }
    }

    public class PostAuthorDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhotoPath { get; set; }
    }

    public class PostMediaDto
    {
        public int MediaId { get; set; }
        public string MediaUrl { get; set; } = "";
        public string MediaType { get; set; } = "";
        public DateTime? CreateAt { get; set; }
    }

    public class PostFeedItemDto
    {
        public int PostId { get; set; }
        public int AccountId { get; set; }
        public PostAuthorDto Author { get; set; } = new();
        public string? Content { get; set; }
        public string? PostType { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public bool? IsRemove { get; set; }
        public List<PostMediaDto> Media { get; set; } = new();
        public int LikeCount { get; set; }
        public int CommentCount { get; set; }
        public int ShareCount { get; set; }
        public bool IsLikedByMe { get; set; }
        public bool IsSharedByMe { get; set; }
    }

    public class PostDetailDto : PostFeedItemDto
    {
        public List<CommentDto> Comments { get; set; } = new();
    }

    public class CreateCommentRequest
    {
        public string Content { get; set; } = "";
        public int? ParentCommentId { get; set; }
    }

    public class UpdateCommentRequest
    {
        public string Content { get; set; } = "";
    }

    public class CommentDto
    {
        public int CommentId { get; set; }
        public int PostId { get; set; }
        public int AccountId { get; set; }
        public PostAuthorDto Author { get; set; } = new();
        public string Content { get; set; } = "";
        public int? ParentCommentId { get; set; }
        public DateTime? CreateAt { get; set; }
        public DateTime? UpdateAt { get; set; }
        public bool? IsRemove { get; set; }
        public List<CommentDto> Children { get; set; } = new();
    }

    public class LikeResponseDto
    {
        public int PostId { get; set; }
        public bool IsLiked { get; set; }
        public int LikeCount { get; set; }
    }

    public class ShareResponseDto
    {
        public int PostId { get; set; }
        public bool IsShared { get; set; }
        public int ShareCount { get; set; }
    }

    public class PostLikeUserDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public string? PhotoPath { get; set; }
    }

    public class PostShareUserDto
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; } = "";
        public string? PhotoPath { get; set; }
        public DateTime? CreateAt { get; set; }
    }
}
