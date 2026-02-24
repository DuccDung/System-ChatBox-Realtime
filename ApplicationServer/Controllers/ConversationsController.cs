using ApplicationServer.Dtos.Conversations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApplicationServer.Controllers
{
    [Route("api/conversations")]
    [ApiController]
    public class ConversationsController : ControllerBase
    {
        private readonly SocialNetworkContext _context;
        public ConversationsController(SocialNetworkContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrGetOneToOne([FromBody] CreateConversationRequest req)
        {
            if (req == null) return BadRequest("Body is required.");
            if (req.AccountId <= 0 || req.FriendId <= 0) return BadRequest("Invalid ids.");
            if (req.AccountId == req.FriendId) return BadRequest("AccountId and FriendId must be different.");

            // Check users exist
            var usersExist = await _context.Accounts
                .Where(a => a.AccountId == req.AccountId || a.AccountId == req.FriendId)
                .Select(a => a.AccountId)
                .ToListAsync();

            if (usersExist.Count != 2) return NotFound("One or both users not found.");

            // Try find existing 1-1 conversation
            // Condition: is_group == false and members contain both ids and total members == 2
            var existingConversationId = await _context.ConversationMembers
                .Where(cm => cm.AccountId == req.AccountId || cm.AccountId == req.FriendId)
                .GroupBy(cm => cm.ConversationId)
                .Where(g => g.Select(x => x.AccountId).Distinct().Count() == 2) // đủ 2 người
                .Select(g => g.Key)
                .FirstOrDefaultAsync();

            if (existingConversationId != 0)
            {
                var conv = await _context.Conversations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(c => c.ConversationId == existingConversationId && c.IsGroup == false);

                if (conv != null)
                {
                    return Ok(new ConversationDto
                    {
                        ConversationId = conv.ConversationId,
                        IsGroup = conv.IsGroup,
                        Title = conv.Title,
                        CreatedAt = conv.CreatedAt
                    });
                }
            }

            // Create new conversation + members in a transaction
            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var conversation = new Conversation
                {
                    IsGroup = false,
                    Title = null,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(); // để có ConversationId

                _context.ConversationMembers.AddRange(
                    new ConversationMember
                    {
                        ConversationId = conversation.ConversationId,
                        AccountId = req.AccountId,
                        JoinedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        Title = null
                    },
                    new ConversationMember
                    {
                        ConversationId = conversation.ConversationId,
                        AccountId = req.FriendId,
                        JoinedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        Title = null
                    }
                );

                await _context.SaveChangesAsync();
                await tx.CommitAsync();

                return Ok(new ConversationDto
                {
                    ConversationId = conversation.ConversationId,
                    IsGroup = conversation.IsGroup,
                    Title = conversation.Title,
                    CreatedAt = conversation.CreatedAt
                });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }
       
        // GET: /api/conversations/threads?accountId=1
        [HttpGet("threads")]
        public async Task<IActionResult> GetThreads([FromQuery] int accountId)
        {
            if (accountId <= 0) return BadRequest("accountId is required.");

            var exists = await _context.Accounts.AnyAsync(a => a.AccountId == accountId);
            if (!exists) return NotFound("User not found.");

            // Lấy tất cả conversation mà user là member
            var threads = await _context.ConversationMembers
                .Where(cm => cm.AccountId == accountId)
                .Select(cm => cm.Conversation)
                .Where(c => c != null)
                .Select(c => new
                {
                    Conversation = c!,
                    // lấy tin nhắn cuối cùng (theo CreatedAt)
                    LastMessage = c!.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => new { m.Content, m.MessageType, m.CreatedAt, m.SenderId })
                        .FirstOrDefault(),

                    // lấy "người còn lại" trong 1-1
                    OtherMember = c!.ConversationMembers
                        .Where(x => x.AccountId != accountId)
                        .Select(x => x.Account)
                        .FirstOrDefault()
                })
                .OrderByDescending(x => x.LastMessage!.CreatedAt) // thread mới lên đầu
                .ToListAsync();

            // map ra ThreadDto
            var result = threads.Select(x =>
            {
                var c = x.Conversation;

                // name + avatar cho 1-1 / group
                string name;
                string avatar;

                if (c.IsGroup)
                {
                    name = string.IsNullOrWhiteSpace(c.Title) ? "Nhóm chat" : c.Title!;
                    avatar = Url.Content("~/assets/images/group-default.png"); // bạn có thể đổi
                }
                else
                {
                    var other = x.OtherMember;
                    name = other?.AccountName ?? "Người dùng";
                    avatar = string.IsNullOrWhiteSpace(other?.PhotoPath)
                        ? Url.Content("~/assets/images/avatar-default.png")
                        : other!.PhotoPath!;
                }

                // snippet tin nhắn cuối
                string snippet = "Chưa có tin nhắn";
                DateTime? lastAt = null;

                if (x.LastMessage != null)
                {
                    lastAt = x.LastMessage.CreatedAt;

                    // tuỳ bạn quy ước MessageType, ở đây minh hoạ:
                    // nếu message_type là "file" / "image" / ... thì set text khác
                    if (!string.IsNullOrWhiteSpace(x.LastMessage.Content))
                        snippet = x.LastMessage.Content!;
                    else
                        snippet = "Đã gửi một tệp đính kèm.";
                }

                return new ThreadDto
                {
                    ConversationId = c.ConversationId,
                    Name = name,
                    AvatarUrl = avatar,
                    Snippet = snippet,
                    LastMessageAt = lastAt
                };
            }).ToList();

            return Ok(result);
        }
        // GET: /api/conversations/{id}/messages?me=1&limit=50&beforeMessageId=123
        [HttpGet("{conversationId:int}/messages")]
        public async Task<ActionResult<List<MessageDto>>> GetMessages(
            int conversationId,
            [FromQuery] int me,
            [FromQuery] int limit = 50,
            [FromQuery] int? beforeMessageId = null)
        {
            if (me <= 0) return BadRequest("me is required");
            limit = Math.Clamp(limit, 1, 200);

            // check member
            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.AccountId == me);
            if (!isMember) return Forbid();

            var query = _context.Messages
                .Where(m => m.ConversationId == conversationId && (m.IsRemove == null || m.IsRemove == false))
                .Include(m => m.Sender)
                .AsNoTracking()
                .OrderByDescending(m => m.MessageId); // pagination dễ bằng message_id

            if (beforeMessageId.HasValue)
                query = query.Where(m => m.MessageId < beforeMessageId.Value)
                             .OrderByDescending(m => m.MessageId);

            var messages = await query.Take(limit).ToListAsync();

            // đảo lại để hiển thị từ cũ -> mới
            messages.Reverse();

            var result = messages.Select(m => new MessageDto
            {
                MessageId = m.MessageId,
                ConversationId = m.ConversationId,
                Content = m.Content,
                MessageType = m.MessageType,
                CreatedAt = m.CreatedAt,
                IsRead = m.IsRead,
                IsRemove = m.IsRemove,
                ParentMessageId = m.ParentMessageId,
                Sender = new SenderDto
                {
                    AccountId = m.Sender.AccountId,
                    AccountName = m.Sender.AccountName,
                    Email = m.Sender.Email,
                    PhotoPath = m.Sender.PhotoPath
                }
            }).ToList();

            return Ok(result);
        }

        // OPTIONAL: mark read (placeholder)
        [HttpPost("{conversationId:int}/mark-read")]
        public async Task<IActionResult> MarkRead(int conversationId, [FromQuery] int me)
        {
            if (me <= 0) return BadRequest("me is required");

            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.AccountId == me);
            if (!isMember) return Forbid();

            // NOTE: hệ thống đọc thật sự cho group/private thường cần bảng MessageReadReceipt (message_id, account_id, read_at)
            // Tạm thời: nếu bạn đang dùng Message.IsRead (bool) global thì không đủ cho group.
            return Ok(new { ok = true });
        }

        // POST: /api/conversations/{conversationId}/messages
        [HttpPost("{conversationId:int}/messages")]
        public async Task<IActionResult> SendTextMessage(
            int conversationId,
            [FromBody] SendMessageRequest req)
        {
            if (req == null) return BadRequest("Body is required.");
            if (req.SenderId <= 0) return BadRequest("SenderId is required.");
            if (string.IsNullOrWhiteSpace(req.Content)) return BadRequest("Content is required.");

            // check conversation exists
            var convExists = await _context.Conversations
                .AnyAsync(c => c.ConversationId == conversationId);
            if (!convExists) return NotFound("Conversation not found.");

            // check sender exists
            var sender = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == req.SenderId);
            if (sender == null) return NotFound("Sender not found.");

            // check sender is a member of the conversation
            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.AccountId == req.SenderId);
            if (!isMember) return Forbid();

            // optional: validate parent message belongs to same conversation
            if (req.ParentMessageId.HasValue)
            {
                var parentOk = await _context.Messages.AnyAsync(m =>
                    m.MessageId == req.ParentMessageId.Value &&
                    m.ConversationId == conversationId);
                if (!parentOk) return BadRequest("ParentMessageId is invalid.");
            }

            var now = DateTime.UtcNow;

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = req.SenderId,
                Content = req.Content.Trim(),
                MessageType = "text",
                CreatedAt = now,
                ParentMessageId = req.ParentMessageId,
                IsRead = false,
                IsRemove = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // trả về data giống GetMessages đang map
            var result = new MessageDto
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                Content = message.Content,
                MessageType = message.MessageType,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                IsRemove = message.IsRemove,
                ParentMessageId = message.ParentMessageId,
                Sender = new SenderDto
                {
                    AccountId = sender.AccountId,
                    AccountName = sender.AccountName,
                    Email = sender.Email,
                    PhotoPath = sender.PhotoPath
                }
            };

            return Ok(result);
        }
        // POST: /api/conversations/{conversationId}/messages/image
        [HttpPost("{conversationId:int}/messages/image")]
        public async Task<IActionResult> SendImageMessage(
            int conversationId,
            [FromBody] SendImageMessageRequest req)
        {
            if (req == null) return BadRequest("Body is required.");
            if (req.SenderId <= 0) return BadRequest("SenderId is required.");
            if (string.IsNullOrWhiteSpace(req.ImageUrl))
                return BadRequest("ImageUrl is required.");

            // check conversation exists
            var convExists = await _context.Conversations
                .AnyAsync(c => c.ConversationId == conversationId);
            if (!convExists) return NotFound("Conversation not found.");

            // check sender exists
            var sender = await _context.Accounts
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountId == req.SenderId);
            if (sender == null) return NotFound("Sender not found.");

            // check sender is member
            var isMember = await _context.ConversationMembers
                .AnyAsync(cm => cm.ConversationId == conversationId && cm.AccountId == req.SenderId);
            if (!isMember) return Forbid();

            // validate parent message
            if (req.ParentMessageId.HasValue)
            {
                var parentOk = await _context.Messages.AnyAsync(m =>
                    m.MessageId == req.ParentMessageId.Value &&
                    m.ConversationId == conversationId);

                if (!parentOk)
                    return BadRequest("ParentMessageId is invalid.");
            }

            var now = DateTime.UtcNow;

            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = req.SenderId,
                Content = req.ImageUrl.Trim(), // lưu URL vào Content
                MessageType = "image",         // 🔥 quan trọng
                CreatedAt = now,
                ParentMessageId = req.ParentMessageId,
                IsRead = false,
                IsRemove = false
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            var result = new MessageDto
            {
                MessageId = message.MessageId,
                ConversationId = message.ConversationId,
                Content = message.Content,
                MessageType = message.MessageType,
                CreatedAt = message.CreatedAt,
                IsRead = message.IsRead,
                IsRemove = message.IsRemove,
                ParentMessageId = message.ParentMessageId,
                Sender = new SenderDto
                {
                    AccountId = sender.AccountId,
                    AccountName = sender.AccountName,
                    Email = sender.Email,
                    PhotoPath = sender.PhotoPath
                }
            };

            return Ok(result);
        }
    }
}