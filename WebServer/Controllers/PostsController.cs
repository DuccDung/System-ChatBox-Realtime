using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebServer.Dtos;
using WebServer.Interfaces;

namespace WebServer.Controllers
{
    [Authorize]
    [Route("Posts")]
    public class PostsController : Controller
    {
        private readonly IPostService _postService;
        private readonly IUserService _userService;
        private readonly ILogger<PostsController> _logger;

        public PostsController(
            IPostService postService,
            IUserService userService,
            ILogger<PostsController> logger)
        {
            _postService = postService;
            _userService = userService;
            _logger = logger;
        }

        [HttpGet]
        [HttpGet("Feed")]
        public async Task<IActionResult> Feed([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            var returnUrl = Url.Action(nameof(Feed), new { page, pageSize });
            ViewBag.User = user;
            ViewBag.CurrentUserId = user.AccountId;
            ViewBag.ReturnUrl = returnUrl;

            try
            {
                var posts = await _postService.GetFeedAsync(page, pageSize);
                return View(posts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading feed");
                TempData["PostError"] = "Khong tai duoc feed bai viet.";
                return View(new List<PostFeedItemDto>());
            }
        }

        [HttpGet("Detail/{postId:int}")]
        public async Task<IActionResult> Detail(int postId)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                var post = await _postService.GetPostDetailAsync(postId);
                ViewBag.User = user;
                ViewBag.CurrentUserId = user.AccountId;
                ViewBag.ReturnUrl = Url.Action(nameof(Detail), new { postId });
                return View(post);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading post detail {PostId}", postId);
                TempData["PostError"] = "Khong tai duoc bai viet.";
                return RedirectToAction(nameof(Feed));
            }
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] string? content, [FromForm] string? postType, [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.CreatePostAsync(new CreatePostRequest
                {
                    Content = content,
                    PostType = postType
                });

                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating post");
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
        }

        [HttpPost("Edit/{postId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int postId,
            [FromForm] string? content,
            [FromForm] string? postType,
            [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.UpdatePostAsync(postId, new UpdatePostRequest
                {
                    Content = content,
                    PostType = postType
                });

                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
        }

        [HttpPost("Delete/{postId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int postId, [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.DeletePostAsync(postId);
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
        }

        [HttpPost("{postId:int}/Like")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Like(int postId, [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.LikePostAsync(postId);
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error liking post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
        }

        [HttpPost("{postId:int}/Unlike")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unlike(int postId, [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.UnlikePostAsync(postId);
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unliking post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
        }

        [HttpPost("{postId:int}/Share")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Share(int postId, [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.SharePostAsync(postId);
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sharing post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
        }

        [HttpPost("{postId:int}/Unshare")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Unshare(int postId, [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.UnsharePostAsync(postId);
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsharing post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Feed))!));
            }
        }

        [HttpPost("{postId:int}/Comments")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComment(
            int postId,
            [FromForm] string? content,
            [FromForm] int? parentCommentId,
            [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.CreateCommentAsync(postId, new CreateCommentRequest
                {
                    Content = content ?? string.Empty,
                    ParentCommentId = parentCommentId
                });

                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating comment for post {PostId}", postId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
        }

        [HttpPost("{postId:int}/Comments/{commentId:int}/Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateComment(
            int postId,
            int commentId,
            [FromForm] string? content,
            [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.UpdateCommentAsync(commentId, new UpdateCommentRequest
                {
                    Content = content ?? string.Empty
                });

                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating comment {CommentId}", commentId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
        }

        [HttpPost("{postId:int}/Comments/{commentId:int}/Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(
            int postId,
            int commentId,
            [FromForm] string? returnUrl)
        {
            var user = await LoadCurrentUserAsync();
            if (user == null)
                return RedirectToAction("Login", "Auth");

            try
            {
                await _postService.DeleteCommentAsync(commentId);
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                TempData["PostError"] = ex.Message;
                return Redirect(SafeReturnUrl(returnUrl, Url.Action(nameof(Detail), new { postId })!));
            }
        }

        private async Task<UserDto?> LoadCurrentUserAsync()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out var userId))
                return null;

            try
            {
                var user = await _userService.GetUserByIdAsync(userId);
                ViewBag.User = user;
                return user;
            }
            catch
            {
                return null;
            }
        }

        private string SafeReturnUrl(string? returnUrl, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return returnUrl;

            return fallback;
        }
    }
}
