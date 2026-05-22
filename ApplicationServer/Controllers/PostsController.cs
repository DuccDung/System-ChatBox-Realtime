using System.Security.Claims;
using ApplicationServer.Dtos.Posts;
using ApplicationServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApplicationServer.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/posts")]
    public class PostsController : ControllerBase
    {
        private const string DefaultAvatarUrl = "/assets/images/avatar-default.png";
        private const string DeletedCommentPlaceholder = "Binh luan da bi xoa";

        private readonly SocialNetworkContext _context;

        public PostsController(SocialNetworkContext context)
        {
            _context = context;
        }

        [HttpGet("feed")]
        public async Task<ActionResult<List<PostFeedItemDto>>> GetFeed(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var posts = await LoadPagedPostsAsync(_context.Posts.Where(p => p.IsRemove != true), page, pageSize);
            var result = await BuildFeedItemsAsync(posts, currentUserId);
            return Ok(result);
        }

        [HttpGet("user/{accountId:int}")]
        public async Task<ActionResult<List<PostFeedItemDto>>> GetUserPosts(
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var ownerExists = await _context.Accounts.AnyAsync(a => a.AccountId == accountId);
            if (!ownerExists)
                return NotFound(new { message = "User not found." });

            var posts = await LoadPagedPostsAsync(
                _context.Posts.Where(p => p.IsRemove != true && p.AccountId == accountId),
                page,
                pageSize);

            var result = await BuildFeedItemsAsync(posts, currentUserId);
            return Ok(result);
        }

        [HttpGet("{postId:int}")]
        public async Task<ActionResult<PostDetailDto>> GetPostDetail(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var detail = await LoadPostDetailAsync(postId, currentUserId);
            if (detail == null)
                return NotFound(new { message = "Post not found." });

            return Ok(detail);
        }

        [HttpPost]
        public async Task<ActionResult<PostDetailDto>> CreatePost([FromBody] CreatePostRequest request)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (request == null)
                return BadRequest(new { message = "Body is required." });

            var content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim();
            var media = NormalizeMedia(request.Media);

            if (string.IsNullOrWhiteSpace(content) && media.Count == 0)
                return BadRequest(new { message = "Content or media is required." });

            var now = DateTime.UtcNow;
            var post = new Post
            {
                AccountId = currentUserId,
                Content = content,
                PostType = string.IsNullOrWhiteSpace(request.PostType)
                    ? (media.Count > 0 ? "media" : "text")
                    : request.PostType.Trim(),
                CreateAt = now,
                UpdateAt = now,
                IsRemove = false
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            if (media.Count > 0)
            {
                _context.PostMedia.AddRange(media.Select(item => new PostMedium
                {
                    PostId = post.PostId,
                    MediaUrl = item.MediaUrl,
                    MediaType = item.MediaType,
                    CreateAt = now
                }));

                await _context.SaveChangesAsync();
            }

            var detail = await LoadPostDetailAsync(post.PostId, currentUserId);
            return Ok(detail ?? new PostDetailDto { PostId = post.PostId, AccountId = currentUserId });
        }

        [HttpPut("{postId:int}")]
        public async Task<ActionResult<PostDetailDto>> UpdatePost(
            int postId,
            [FromBody] UpdatePostRequest request)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (request == null)
                return BadRequest(new { message = "Body is required." });

            var post = await _context.Posts
                .Include(p => p.Account)
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.PostId == postId && p.IsRemove != true);

            if (post == null)
                return NotFound(new { message = "Post not found." });

            if (post.AccountId != currentUserId)
                return Forbid();

            if (request.Content != null)
                post.Content = string.IsNullOrWhiteSpace(request.Content) ? null : request.Content.Trim();

            if (request.PostType != null)
                post.PostType = string.IsNullOrWhiteSpace(request.PostType) ? post.PostType : request.PostType.Trim();

            if (request.Media != null)
            {
                _context.PostMedia.RemoveRange(post.PostMedia);
                var mediaItems = NormalizeMedia(request.Media);
                foreach (var media in mediaItems)
                {
                    _context.PostMedia.Add(new PostMedium
                    {
                        PostId = post.PostId,
                        MediaUrl = media.MediaUrl,
                        MediaType = media.MediaType,
                        CreateAt = DateTime.UtcNow
                    });
                }
            }

            post.UpdateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var detail = await LoadPostDetailAsync(post.PostId, currentUserId);
            return Ok(detail ?? new PostDetailDto { PostId = post.PostId, AccountId = currentUserId });
        }

        [HttpDelete("{postId:int}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId && p.IsRemove != true);
            if (post == null)
                return NotFound(new { message = "Post not found." });

            if (post.AccountId != currentUserId)
                return Forbid();

            post.IsRemove = true;
            post.UpdateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, postId });
        }

        [HttpPost("{postId:int}/like")]
        public async Task<ActionResult<LikeResponseDto>> LikePost(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId && p.IsRemove != true);
            if (!postExists)
                return NotFound(new { message = "Post not found." });

            var existingLike = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);

            if (existingLike == null)
            {
                _context.PostLikes.Add(new PostLike
                {
                    PostId = postId,
                    UserId = currentUserId
                });
                await _context.SaveChangesAsync();
            }

            var likeCount = await CountLikesAsync(postId);
            return Ok(new LikeResponseDto
            {
                PostId = postId,
                IsLiked = true,
                LikeCount = likeCount
            });
        }

        [HttpDelete("{postId:int}/like")]
        public async Task<ActionResult<LikeResponseDto>> UnlikePost(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var like = await _context.PostLikes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);

            if (like != null)
            {
                _context.PostLikes.Remove(like);
                await _context.SaveChangesAsync();
            }

            var likeCount = await CountLikesAsync(postId);
            return Ok(new LikeResponseDto
            {
                PostId = postId,
                IsLiked = false,
                LikeCount = likeCount
            });
        }

        [HttpGet("{postId:int}/likes")]
        public async Task<ActionResult<List<PostLikeUserDto>>> GetLikes(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId && p.IsRemove != true);
            if (!postExists)
                return NotFound(new { message = "Post not found." });

            var result = await _context.PostLikes
                .AsNoTracking()
                .Where(l => l.PostId == postId)
                .Include(l => l.Account)
                .OrderByDescending(l => l.LikeId)
                .Select(l => new PostLikeUserDto
                {
                    AccountId = l.Account!.AccountId,
                    AccountName = l.Account.AccountName,
                    PhotoPath = string.IsNullOrWhiteSpace(l.Account.PhotoPath) ? DefaultAvatarUrl : l.Account.PhotoPath
                })
                .ToListAsync();

            return Ok(result);
        }

        [HttpGet("{postId:int}/comments")]
        public async Task<ActionResult<List<CommentDto>>> GetComments(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId && p.IsRemove != true);
            if (!postExists)
                return NotFound(new { message = "Post not found." });

            var comments = await LoadCommentTreeAsync(postId);
            return Ok(comments);
        }

        [HttpPost("{postId:int}/comments")]
        public async Task<ActionResult<CommentDto>> CreateComment(
            int postId,
            [FromBody] CreateCommentRequest request)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (request == null)
                return BadRequest(new { message = "Body is required." });

            var post = await _context.Posts.FirstOrDefaultAsync(p => p.PostId == postId && p.IsRemove != true);
            if (post == null)
                return NotFound(new { message = "Post not found." });

            var content = request.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest(new { message = "Content is required." });

            if (request.ParentCommentId.HasValue)
            {
                var parent = await _context.PostComments.FirstOrDefaultAsync(c =>
                    c.CommentId == request.ParentCommentId.Value &&
                    c.PostId == postId);

                if (parent == null)
                    return BadRequest(new { message = "Parent comment not found." });
            }

            var now = DateTime.UtcNow;
            var comment = new PostComment
            {
                PostId = postId,
                AccountId = currentUserId,
                Content = content,
                ParentCommentId = request.ParentCommentId,
                CreateAt = now,
                UpdateAt = now,
                IsRemove = false
            };

            _context.PostComments.Add(comment);
            await _context.SaveChangesAsync();

            var result = await LoadCommentByIdAsync(comment.CommentId);
            return Ok(result ?? MapCommentDto(comment, null));
        }

        [HttpPut("comments/{commentId:int}")]
        public async Task<ActionResult<CommentDto>> UpdateComment(
            int commentId,
            [FromBody] UpdateCommentRequest request)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (request == null)
                return BadRequest(new { message = "Body is required." });

            var comment = await _context.PostComments
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                return NotFound(new { message = "Comment not found." });

            if (comment.IsRemove == true)
                return BadRequest(new { message = "Comment was deleted." });

            if (comment.AccountId != currentUserId)
                return Forbid();

            var content = request.Content?.Trim();
            if (string.IsNullOrWhiteSpace(content))
                return BadRequest(new { message = "Content is required." });

            comment.Content = content;
            comment.UpdateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var result = await LoadCommentByIdAsync(comment.CommentId);
            return Ok(result ?? MapCommentDto(comment, comment.Account));
        }

        [HttpDelete("comments/{commentId:int}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var comment = await _context.PostComments.FirstOrDefaultAsync(c => c.CommentId == commentId);
            if (comment == null)
                return NotFound(new { message = "Comment not found." });

            if (comment.IsRemove == true)
                return Ok(new { ok = true, commentId, removed = true });

            if (comment.AccountId != currentUserId)
                return Forbid();

            comment.IsRemove = true;
            comment.UpdateAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { ok = true, commentId, removed = true });
        }

        [HttpPost("{postId:int}/share")]
        public async Task<ActionResult<ShareResponseDto>> SharePost(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId && p.IsRemove != true);
            if (!postExists)
                return NotFound(new { message = "Post not found." });

            var existingShare = await _context.PostShares
                .FirstOrDefaultAsync(s => s.PostId == postId && s.AccountId == currentUserId);

            if (existingShare == null)
            {
                _context.PostShares.Add(new PostShare
                {
                    PostId = postId,
                    AccountId = currentUserId,
                    CreateAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            var shareCount = await CountSharesAsync(postId);
            return Ok(new ShareResponseDto
            {
                PostId = postId,
                IsShared = true,
                ShareCount = shareCount
            });
        }

        [HttpDelete("{postId:int}/share")]
        public async Task<ActionResult<ShareResponseDto>> UnsharePost(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var share = await _context.PostShares
                .FirstOrDefaultAsync(s => s.PostId == postId && s.AccountId == currentUserId);

            if (share != null)
            {
                _context.PostShares.Remove(share);
                await _context.SaveChangesAsync();
            }

            var shareCount = await CountSharesAsync(postId);
            return Ok(new ShareResponseDto
            {
                PostId = postId,
                IsShared = false,
                ShareCount = shareCount
            });
        }

        [HttpGet("{postId:int}/shares")]
        public async Task<ActionResult<List<PostShareUserDto>>> GetShares(int postId)
        {
            if (!TryGetCurrentAccountId(out var currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            if (!await AccountExistsAsync(currentUserId))
                return Unauthorized(new { message = "Not logged in." });

            var postExists = await _context.Posts.AnyAsync(p => p.PostId == postId && p.IsRemove != true);
            if (!postExists)
                return NotFound(new { message = "Post not found." });

            var result = await _context.PostShares
                .AsNoTracking()
                .Where(s => s.PostId == postId)
                .Include(s => s.Account)
                .OrderByDescending(s => s.CreateAt)
                .ThenByDescending(s => s.PsId)
                .Select(s => new PostShareUserDto
                {
                    AccountId = s.Account != null ? s.Account.AccountId : 0,
                    AccountName = s.Account != null && !string.IsNullOrWhiteSpace(s.Account.AccountName)
                        ? s.Account.AccountName
                        : "Nguoi dung",
                    PhotoPath = s.Account != null && !string.IsNullOrWhiteSpace(s.Account.PhotoPath)
                        ? s.Account.PhotoPath
                        : DefaultAvatarUrl,
                    CreateAt = s.CreateAt
                })
                .ToListAsync();

            return Ok(result);
        }

        private async Task<List<Post>> LoadPagedPostsAsync(IQueryable<Post> query, int page, int pageSize)
        {
            var normalizedPage = Math.Max(page, 1);
            var normalizedPageSize = Math.Clamp(pageSize, 1, 100);

            return await query
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Account)
                .Include(p => p.PostMedia)
                .OrderByDescending(p => p.CreateAt)
                .ThenByDescending(p => p.PostId)
                .Skip((normalizedPage - 1) * normalizedPageSize)
                .Take(normalizedPageSize)
                .ToListAsync();
        }

        private async Task<List<PostFeedItemDto>> BuildFeedItemsAsync(List<Post> posts, int currentUserId)
        {
            if (posts.Count == 0)
                return new List<PostFeedItemDto>();

            var postIds = posts.Select(p => p.PostId).ToList();
            var counters = await LoadCountersAsync(postIds, currentUserId);

            return posts.Select(post => MapFeedItem(post, counters)).ToList();
        }

        private async Task<PostDetailDto?> LoadPostDetailAsync(int postId, int currentUserId)
        {
            var post = await _context.Posts
                .AsNoTracking()
                .AsSplitQuery()
                .Include(p => p.Account)
                .Include(p => p.PostMedia)
                .FirstOrDefaultAsync(p => p.PostId == postId && p.IsRemove != true);

            if (post == null)
                return null;

            var feedItem = (await BuildFeedItemsAsync(new List<Post> { post }, currentUserId)).FirstOrDefault();
            if (feedItem == null)
                return null;

            var comments = await LoadCommentTreeAsync(postId);

            return new PostDetailDto
            {
                PostId = feedItem.PostId,
                AccountId = feedItem.AccountId,
                Author = feedItem.Author,
                Content = feedItem.Content,
                PostType = feedItem.PostType,
                CreateAt = feedItem.CreateAt,
                UpdateAt = feedItem.UpdateAt,
                IsRemove = feedItem.IsRemove,
                Media = feedItem.Media,
                LikeCount = feedItem.LikeCount,
                CommentCount = feedItem.CommentCount,
                ShareCount = feedItem.ShareCount,
                IsLikedByMe = feedItem.IsLikedByMe,
                IsSharedByMe = feedItem.IsSharedByMe,
                Comments = comments
            };
        }

        private async Task<List<CommentDto>> LoadCommentTreeAsync(int postId)
        {
            var rows = await _context.PostComments
                .AsNoTracking()
                .Include(c => c.Account)
                .Where(c => c.PostId == postId)
                .OrderBy(c => c.CreateAt)
                .ThenBy(c => c.CommentId)
                .ToListAsync();

            if (rows.Count == 0)
                return new List<CommentDto>();

            var map = rows.ToDictionary(
                row => row.CommentId,
                row => MapCommentDto(row, row.Account));

            foreach (var row in rows)
            {
                if (row.ParentCommentId.HasValue && map.TryGetValue(row.ParentCommentId.Value, out var parent))
                {
                    parent.Children.Add(map[row.CommentId]);
                }
            }

            return rows
                .Where(row => !row.ParentCommentId.HasValue || !map.ContainsKey(row.ParentCommentId.Value))
                .Select(row => map[row.CommentId])
                .ToList();
        }

        private async Task<CommentDto?> LoadCommentByIdAsync(int commentId)
        {
            var comment = await _context.PostComments
                .AsNoTracking()
                .Include(c => c.Account)
                .FirstOrDefaultAsync(c => c.CommentId == commentId);

            if (comment == null)
                return null;

            return MapCommentDto(comment, comment.Account);
        }

        private static CommentDto MapCommentDto(PostComment comment, Account? account)
        {
            var isRemoved = comment.IsRemove == true;

            return new CommentDto
            {
                CommentId = comment.CommentId,
                PostId = comment.PostId,
                AccountId = comment.AccountId,
                Author = MapAuthor(account),
                Content = isRemoved ? DeletedCommentPlaceholder : (comment.Content ?? string.Empty),
                ParentCommentId = comment.ParentCommentId,
                CreateAt = comment.CreateAt,
                UpdateAt = comment.UpdateAt,
                IsRemove = comment.IsRemove,
                Children = new List<CommentDto>()
            };
        }

        private static PostFeedItemDto MapFeedItem(Post post, PostCounters counters)
        {
            return new PostFeedItemDto
            {
                PostId = post.PostId,
                AccountId = post.AccountId,
                Author = MapAuthor(post.Account),
                Content = post.Content,
                PostType = post.PostType,
                CreateAt = post.CreateAt,
                UpdateAt = post.UpdateAt,
                IsRemove = post.IsRemove,
                Media = post.PostMedia
                    .OrderBy(m => m.CreateAt)
                    .ThenBy(m => m.MediaId)
                    .Select(MapMedia)
                    .ToList(),
                LikeCount = counters.LikeCounts.GetValueOrDefault(post.PostId),
                CommentCount = counters.CommentCounts.GetValueOrDefault(post.PostId),
                ShareCount = counters.ShareCounts.GetValueOrDefault(post.PostId),
                IsLikedByMe = counters.LikedPostIds.Contains(post.PostId),
                IsSharedByMe = counters.SharedPostIds.Contains(post.PostId)
            };
        }

        private static PostMediaDto MapMedia(PostMedium media)
        {
            return new PostMediaDto
            {
                MediaId = media.MediaId,
                MediaUrl = media.MediaUrl,
                MediaType = media.MediaType,
                CreateAt = media.CreateAt
            };
        }

        private static PostAuthorDto MapAuthor(Account? account)
        {
            return new PostAuthorDto
            {
                AccountId = account?.AccountId ?? 0,
                AccountName = string.IsNullOrWhiteSpace(account?.AccountName) ? "Nguoi dung" : account!.AccountName,
                Email = account?.Email ?? string.Empty,
                PhotoPath = string.IsNullOrWhiteSpace(account?.PhotoPath) ? DefaultAvatarUrl : account!.PhotoPath
            };
        }

        private async Task<PostCounters> LoadCountersAsync(List<int> postIds, int currentUserId)
        {
            if (postIds.Count == 0)
                return new PostCounters();

            var likeCounts = await _context.PostLikes
                .AsNoTracking()
                .Where(l => l.PostId.HasValue && postIds.Contains(l.PostId.Value))
                .GroupBy(l => l.PostId!.Value)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);

            var commentCounts = await _context.PostComments
                .AsNoTracking()
                .Where(c => postIds.Contains(c.PostId) && c.IsRemove != true)
                .GroupBy(c => c.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);

            var shareCounts = await _context.PostShares
                .AsNoTracking()
                .Where(s => postIds.Contains(s.PostId))
                .GroupBy(s => s.PostId)
                .Select(g => new { PostId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PostId, x => x.Count);

            var likedPostIds = await _context.PostLikes
                .AsNoTracking()
                .Where(l => l.UserId == currentUserId && l.PostId.HasValue && postIds.Contains(l.PostId.Value))
                .Select(l => l.PostId!.Value)
                .Distinct()
                .ToListAsync();

            var sharedPostIds = await _context.PostShares
                .AsNoTracking()
                .Where(s => s.AccountId == currentUserId && postIds.Contains(s.PostId))
                .Select(s => s.PostId)
                .Distinct()
                .ToListAsync();

            return new PostCounters
            {
                LikeCounts = likeCounts,
                CommentCounts = commentCounts,
                ShareCounts = shareCounts,
                LikedPostIds = likedPostIds.ToHashSet(),
                SharedPostIds = sharedPostIds.ToHashSet()
            };
        }

        private async Task<int> CountLikesAsync(int postId)
        {
            return await _context.PostLikes.CountAsync(l => l.PostId == postId);
        }

        private async Task<int> CountSharesAsync(int postId)
        {
            return await _context.PostShares.CountAsync(s => s.PostId == postId);
        }

        private static List<CreatePostMediaRequest> NormalizeMedia(List<CreatePostMediaRequest> media)
        {
            return media
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.MediaUrl))
                .Select(item => new CreatePostMediaRequest
                {
                    MediaUrl = item.MediaUrl.Trim(),
                    MediaType = string.IsNullOrWhiteSpace(item.MediaType) ? "image" : item.MediaType.Trim()
                })
                .ToList();
        }

        private bool TryGetCurrentAccountId(out int accountId)
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out accountId) && accountId > 0;
        }

        private Task<bool> AccountExistsAsync(int accountId)
        {
            return _context.Accounts.AnyAsync(a => a.AccountId == accountId);
        }

        private sealed class PostCounters
        {
            public Dictionary<int, int> LikeCounts { get; init; } = new();
            public Dictionary<int, int> CommentCounts { get; init; } = new();
            public Dictionary<int, int> ShareCounts { get; init; } = new();
            public HashSet<int> LikedPostIds { get; init; } = new();
            public HashSet<int> SharedPostIds { get; init; } = new();
        }
    }
}
