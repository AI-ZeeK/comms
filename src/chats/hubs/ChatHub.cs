using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Comms.Data;
using Comms.Models;
using Comms.Services;
using Microsoft.EntityFrameworkCore;

namespace Comms.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly CommunicationsDbContext _context;
        private readonly ILogger<ChatHub> _logger;
        private readonly IRabbitMQService _rabbitMQService;

        private readonly IHubContext<ChatListHub> _chatListHub;

        public ChatHub(CommunicationsDbContext context, ILogger<ChatHub> logger, IRabbitMQService rabbitMQService, IHubContext<ChatListHub> chatListHub)
        {
            _context = context;
            _logger = logger;
            _rabbitMQService = rabbitMQService;
            _chatListHub = chatListHub;
        }

            // Join a specific chat room
        public async Task JoinChat(string chatId)
        {
            var userId = GetUserId();
            if (userId == null) return;

            // Verify user is participant in this chat
            var isParticipant = await _context.ChatParticipants
                .AnyAsync(cp => cp.ChatId == Guid.Parse(chatId) && cp.UserId == userId && cp.IsActive);

            if (isParticipant)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
                _logger.LogInformation($"User {userId} joined chat {chatId}");
            }
        }

        // Leave a specific chat room
        public async Task LeaveChat(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Chat_{chatId}");
            _logger.LogInformation($"User left chat {chatId}");
        }

        // Send a message to a chat
        public async Task SendMessage(string chatId, string content, string messageType = "TEXT")
        {
            var userId = GetUserId();
            if (userId == null) return;

            try
            {
                // Verify user is participant in this chat
                var isParticipant = await _context.ChatParticipants
                    .AnyAsync(cp => cp.ChatId == Guid.Parse(chatId) && cp.UserId == userId && cp.IsActive);

                if (!isParticipant) return;

                // Create and save message
                var message = new Message
                {
                    ChatId = Guid.Parse(chatId),
                    SenderId = userId.Value,
                    Content = content,
                    Type = Enum.Parse<MessageType>(messageType),
                    Status = MessageStatus.SENT,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // inside SendMessage after saving the message
var participants = await _context.ChatParticipants
    .Where(cp => cp.ChatId == message.ChatId && cp.IsActive)
    .Select(cp => cp.UserId)
    .ToListAsync();

foreach (var participantId in participants)
{
    // await _chatListHub.Clients.Group($"User_{participantId}").SendAsync("ChatListUpdated", new
    // {
    //     chatId = message.ChatId,
    //     preview = message.Content,
    //     unreadCount = await _context.Messages
    //         .CountAsync(m => m.ChatId == message.ChatId && !m.MessageReads.Any(r => r.UserId == participantId))
    // });
}

                // Publish message event to other microservices
                await _rabbitMQService.PublishChatEventAsync("message.sent", new
                {
                    messageId = message.MessageId,
                    chatId = message.ChatId,
                    senderId = message.SenderId,
                    content = message.Content,
                    type = message.Type.ToString(),
                    createdAt = message.CreatedAt
                });

                // Broadcast message to all participants in the chat
                await Clients.Group($"Chat_{chatId}").SendAsync("ReceiveMessage", new
                {
                    messageId = message.MessageId,
                    chatId = message.ChatId,
                    senderId = message.SenderId,
                    content = message.Content,
                    type = message.Type.ToString(),
                    status = message.Status.ToString(),
                    createdAt = message.CreatedAt
                });

                _logger.LogInformation($"Message sent by user {userId} to chat {chatId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to chat {chatId}");
            }
        }

        // Mark message as read
        public async Task MarkMessageAsRead(string messageId)
        {
            var userId = GetUserId();
            if (userId == null) return;

            try
            {
                var messageRead = new MessageRead
                {
                    MessageId = Guid.Parse(messageId),
                    UserId = userId.Value,
                    ReadAt = DateTime.UtcNow
                };

                _context.MessageReads.Add(messageRead);
                await _context.SaveChangesAsync();

                // Get the chat ID for this message
                var message = await _context.Messages
                    .FirstOrDefaultAsync(m => m.MessageId == Guid.Parse(messageId));

                if (message != null)
                {
                    // Notify other participants that message was read
                    await Clients.Group($"Chat_{message.ChatId}").SendAsync("MessageRead", new
                    {
                        messageId = messageId,
                        userId = userId,
                        readAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking message {messageId} as read");
            }
        }

        // User typing indicator
        public async Task UserTyping(string chatId, bool isTyping)
        {
            var userId = GetUserId();
            if (userId == null) return;

            await Clients.Group($"Chat_{chatId}").SendAsync("UserTyping", new
            {
                userId = userId,
                isTyping = isTyping
            });
        }

        // Connection management
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId != null)
            {
                // Join user to their personal notification group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation($"User {userId} connected to SignalR");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                _logger.LogInformation($"User {userId} disconnected from SignalR");
            }
            await base.OnDisconnectedAsync(exception);
        }

        private Guid? GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst("sub") ?? Context.User?.FindFirst("user_id");
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
            return null;
        }
    }
} 