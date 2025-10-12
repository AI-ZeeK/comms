using Comms.Constants;
using Comms.Data;
using Comms.Models;
using Comms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comms.Controllers
{
    [ApiController]
    [Route(ApiConstants.API_VERSION + "/chats")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly ChatService _chatService;
        private readonly ILogger<ChatController> _logger;
        private readonly IRabbitMQService _rabbitMQService;

        public ChatController(
            ChatService chatService,
            ILogger<ChatController> logger,
            IRabbitMQService rabbitMQService
        )
        {
            _chatService = chatService;
            _logger = logger;
            _rabbitMQService = rabbitMQService;
        }

        // GET: api/chat/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserChats(Guid userId)
        {
            try
            {
                var chats = await _chatService.GetAllChatsWithLastMessageAsync(userId.ToString());

                return Ok(chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting chats for user {userId}");
                return StatusCode(500, "Internal server error");
            }
        }

        // // GET: api/chat/{chatId}/messages
        // [HttpGet("{chatId}/messages")]
        // public async Task<ActionResult<IEnumerable<object>>> GetChatMessages(
        //     Guid chatId,
        //     [FromQuery] int page = 1,
        //     [FromQuery] int pageSize = 50
        // )
        // {
        //     try
        //     {
        //         // Verify user is participant
        //         var userId = GetUserId();
        //         if (userId == null)
        //             return Unauthorized();

        //         var isParticipant = await _context.ChatParticipant.AnyAsync(cp =>
        //             cp.ChatId == chatId && cp.UserId == userId && cp.IsActive
        //         );

        //         if (!isParticipant)
        //             return Forbid();

        //         var messages = await _context
        //             .Messages.Where(m => m.ChatId == chatId && m.DeletedAt == null)
        //             .OrderByDescending(m => m.CreatedAt)
        //             .Skip((page - 1) * pageSize)
        //             .Take(pageSize)
        //             .Select(m => new
        //             {
        //                 messageId = m.MessageId,
        //                 senderId = m.SenderId,
        //                 content = m.Content,
        //                 type = m.Type.ToString(),
        //                 status = m.Status.ToString(),
        //                 mediaUrls = m.MediaUrls,
        //                 fileUrl = m.FileUrl,
        //                 fileSize = m.FileSize,
        //                 fileType = m.FileType,
        //                 duration = m.Duration,
        //                 createdAt = m.CreatedAt,
        //                 readReceipts = m.ReadReceipts.Select(rr => new
        //                 {
        //                     userId = rr.UserId,
        //                     readAt = rr.ReadAt,
        //                 }),
        //             })
        //             .ToListAsync();

        //         return Ok(messages.OrderBy(m => m.createdAt)); // Show oldest first
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, $"Error getting messages for chat {chatId}");
        //         return StatusCode(500, "Internal server error");
        //     }
        // }

        // // POST: api/chat
        // [HttpPost]
        // public async Task<ActionResult<object>> CreateChat([FromBody] CreateChatRequest request)
        // {
        //     try
        //     {
        //         var userId = GetUserId();
        //         if (userId == null)
        //             return Unauthorized();

        //         var chat = new Chat
        //         {
        //             Name = request.Name,
        //             ChatType = Enum.Parse<ChatType>(request.ChatType),
        //             CreatedAt = DateTime.UtcNow,
        //             UpdatedAt = DateTime.UtcNow,
        //         };

        //         _context.Chats.Add(chat);

        //         // Add creator as participant
        //         var creatorParticipant = new ChatParticipant
        //         {
        //             ChatId = chat.ChatId,
        //             UserId = userId.Value,
        //             IsAdmin = true,
        //             JoinedAt = DateTime.UtcNow,
        //         };

        //         _context.ChatParticipant.Add(creatorParticipant);

        //         // Add other participants
        //         foreach (var participantId in request.ParticipantIds)
        //         {
        //             if (participantId != userId.Value)
        //             {
        //                 var participant = new ChatParticipant
        //                 {
        //                     ChatId = chat.ChatId,
        //                     UserId = participantId,
        //                     JoinedAt = DateTime.UtcNow,
        //                 };
        //                 _context.ChatParticipant.Add(participant);
        //             }
        //         }

        //         await _context.SaveChangesAsync();

        //         // Publish chat creation event to other microservices
        //         await _rabbitMQService.PublishChatEventAsync(
        //             "chat.created",
        //             new
        //             {
        //                 chatId = chat.ChatId,
        //                 name = chat.Name,
        //                 chatType = chat.ChatType.ToString(),
        //                 creatorId = userId.Value,
        //                 participantIds = request.ParticipantIds,
        //                 createdAt = chat.CreatedAt,
        //             }
        //         );

        //         return CreatedAtAction(
        //             nameof(GetUserChats),
        //             new { userId = userId.Value },
        //             new
        //             {
        //                 chatId = chat.ChatId,
        //                 name = chat.Name,
        //                 chatType = chat.ChatType.ToString(),
        //                 createdAt = chat.CreatedAt,
        //             }
        //         );
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Error creating chat");
        //         return StatusCode(500, "Internal server error");
        //     }
        // }

        private Guid? GetUserId()
        {
            var userIdClaim = User.FindFirst("sub") ?? User.FindFirst("user_id");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }

    public class CreateChatRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ChatType { get; set; } = "DIRECT";
        public List<Guid> ParticipantIds { get; set; } = new();
    }
}
