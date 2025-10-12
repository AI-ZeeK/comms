using System.Security.Claims;
using Comms.Data;
using Comms.Models;
using Comms.Models.DTOs;
using Comms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Comms.Hubs
{
    [Authorize]
    public class ChatRoomHub : Hub
    {
        private readonly CommunicationsDbContext _context;
        private readonly ILogger<ChatRoomHub> _logger;

        // Removed RabbitMQService, not needed for in-app delivery
        private readonly IHubContext<ChatListHub> _chatListHub;

        private readonly PushService _pushService;

        private readonly ChatService _chatService;

        // In-memory active chat users: chatId -> set of userIds
        // Use Redis for active chat tracking
        private readonly IConnectionTracker _connectionTracker;

        public ChatRoomHub(
            CommunicationsDbContext context,
            ILogger<ChatRoomHub> logger,
            IHubContext<ChatListHub> chatListHub,
            PushService pushService,
            ChatService chatService,
            IConnectionTracker connectionTracker
        )
        {
            _context = context;
            _logger = logger;
            _chatListHub = chatListHub;
            _pushService = pushService;
            _chatService = chatService;
            _connectionTracker = connectionTracker;
        }

        [HubMethodName("join_chat")]
        public async Task JoinChat(string chatId)
        {
            var (user_id, sender_name, sender_avatar) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated in JoinChat");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }

            if (!Guid.TryParse(chatId, out var chatGuid))
            {
                await Clients.Caller.SendAsync("error", "Invalid chat ID");
                return;
            }

            // Verify user is participant in this chat
            var isParticipant = await _context.ChatParticipant.AnyAsync(cp =>
                cp.ChatId == chatGuid && cp.UserId == user_id && cp.IsActive
            );
            if (!isParticipant)
            {
                _logger.LogWarning($"User {user_id} is not a participant of chat {chatId}");
                await Clients.Caller.SendAsync("error", "You are not a participant of this chat");
                return;
            }

            var roomName = $"Chat_{chatId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);
            await _connectionTracker.AddUserToRoomAsync(user_id.Value.ToString(), roomName);
            _logger.LogInformation($"User {user_id} joined chat {chatId}");

            // Send chat messages
            var messages = await _context
                .Messages.Where(m => m.ChatId == chatGuid)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
            await Clients.Caller.SendAsync("chat_messages", new { chat_id = chatId, messages });

            // Mark all SENT messages as DELIVERED for this user
            var deliveredMessages = await _context
                .Messages.Where(m => m.ChatId == chatGuid && m.Status == MessageStatus.SENT)
                .ToListAsync();
            foreach (var msg in deliveredMessages)
            {
                msg.Status = MessageStatus.DELIVERED;
                // Optionally: track delivered per user
                await Clients
                    .Group($"Chat_{chatId}")
                    .SendAsync(
                        "message_delivered",
                        new
                        {
                            message_id = msg.MessageId,
                            delivered_to = user_id,
                            status = "DELIVERED",
                        }
                    );
            }
            await _context.SaveChangesAsync();

            // Mark all messages as read for this user
            var unreadMessages = await _context
                .Messages.Where(m => m.ChatId == chatGuid)
                .Select(m => m.MessageId)
                .ToListAsync();
            foreach (var mid in unreadMessages)
            {
                if (
                    !await _context.MessageReads.AnyAsync(r =>
                        r.MessageId == mid && r.UserId == user_id
                    )
                )
                {
                    _context.MessageReads.Add(
                        new MessageRead
                        {
                            MessageId = mid,
                            UserId = user_id.Value,
                            ReadAt = DateTime.UtcNow,
                        }
                    );
                }
            }
            await _context.SaveChangesAsync();
            await Clients
                .Group($"Chat_{chatId}")
                .SendAsync(
                    "messages_read",
                    new
                    {
                        chat_id = chatId,
                        user_id = user_id,
                        message_ids = unreadMessages,
                        read_at = DateTime.UtcNow,
                    }
                );
        }

        // Leave a specific chat room
        [HubMethodName("leave_chat")]
        public async Task LeaveChat(string chatId)
        {
            var (user_id, sender_name, sender_avatar) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated in LeaveChat");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }
            if (!Guid.TryParse(chatId, out var chatGuid))
            {
                await Clients.Caller.SendAsync("error", "Invalid chat ID");
                return;
            }
            var roomName = $"Chat_{chatId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            await _connectionTracker.RemoveUserFromRoomAsync(user_id.Value.ToString(), roomName);
            _logger.LogInformation($"User {user_id} left chat {chatId}");
        }

        // Send a message to a chat
        [HubMethodName("send_message")]
        public async Task SendMessage(string chatId, string content, string messageType = "TEXT")
        {
            var (user_id, sender_name, sender_avatar) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated in SendMessage");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }
            if (!Guid.TryParse(chatId, out var chatGuid))
            {
                await Clients.Caller.SendAsync("error", "Invalid chat ID");
                return;
            }
            try
            {
                // Verify user is participant in this chat
                var isParticipant = await _context.ChatParticipant.AnyAsync(cp =>
                    cp.ChatId == chatGuid && cp.UserId == user_id && cp.IsActive
                );
                if (!isParticipant)
                {
                    await Clients.Caller.SendAsync(
                        "error",
                        "You are not a participant of this chat"
                    );
                    return;
                }

                // Create and save message
                var message = new Message
                {
                    ChatId = chatGuid,
                    SenderId = user_id.Value,
                    Content = content,
                    Type = Enum.TryParse<MessageType>(messageType, out var mt)
                        ? mt
                        : MessageType.TEXT,
                    Status = MessageStatus.SENT,
                    CreatedAt = DateTime.UtcNow,
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Get all participants
                var participants = await _context
                    .ChatParticipant.Where(cp => cp.ChatId == chatGuid && cp.IsActive)
                    .Select(cp => cp.UserId)
                    .ToListAsync();

                // Broadcast message to all participants in the chat
                await Clients.Group($"Chat_{chatId}").SendAsync("new_message", message);

                // Mark as delivered for all active users except sender (using Redis)
                foreach (var participant in participants)
                {
                    if (participant != user_id)
                    {
                        var roomName = $"Chat_{chatId}";
                        var isActive = await _connectionTracker.IsUserInRoomAsync(
                            participant.ToString(),
                            roomName
                        );
                        if (isActive)
                        {
                            await Clients.Caller.SendAsync(
                                "message_delivered",
                                new
                                {
                                    message_id = message.MessageId,
                                    delivered_to = participant,
                                    status = "DELIVERED",
                                }
                            );
                        }
                    }
                }

                // Update chat lists for all participants
                foreach (var participant in participants)
                {
                    await _chatListHub
                        .Clients.Group($"user_{participant}_chats")
                        .SendAsync("chats_list_updated");
                }

                // Update message status to SENT
                message.Status = MessageStatus.SENT;
                await _context.SaveChangesAsync();
                await Clients.Caller.SendAsync(
                    "message_sent",
                    new { message_id = message.MessageId, status = "SENT" }
                );

                // Send push notification to all except sender
                foreach (var participant in participants)
                {
                    if (participant != user_id)
                    {
                        try
                        {
                            await _pushService.SendNotificationToUserAsync(
                                participant.ToString(),
                                "New Message",
                                content,
                                new NotificationData
                                {
                                    EntityId = chatId,
                                    SenderId = user_id.Value.ToString(),
                                    SenderAvatar = sender_avatar ?? string.Empty,
                                    SenderName = sender_name ?? string.Empty,
                                    EntityType = Comms.Models.NotificationType.NEW_MESSAGE,
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                $"Failed to send notification to user {participant}"
                            );
                        }
                    }
                }

                _logger.LogInformation($"Message sent by user {user_id} to chat {chatId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending message to chat {chatId}");
                await Clients.Caller.SendAsync("error", $"Failed to send message: {ex.Message}");
            }
        }

        // Mark all messages as read in a chat
        public async Task MarkMessagesRead(string chatId)
        {
            var (user_id, sender_name, sender_avatar) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated in MarkMessagesRead");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }
            if (!Guid.TryParse(chatId, out var chatGuid))
            {
                await Clients.Caller.SendAsync("error", "Invalid chat ID");
                return;
            }
            try
            {
                var unreadMessages = await _context
                    .Messages.Where(m => m.ChatId == chatGuid)
                    .Select(m => m.MessageId)
                    .ToListAsync();
                foreach (var mid in unreadMessages)
                {
                    if (
                        !await _context.MessageReads.AnyAsync(r =>
                            r.MessageId == mid && r.UserId == user_id
                        )
                    )
                    {
                        _context.MessageReads.Add(
                            new MessageRead
                            {
                                MessageId = mid,
                                UserId = user_id.Value,
                                ReadAt = DateTime.UtcNow,
                            }
                        );
                    }
                }
                await _context.SaveChangesAsync();
                await Clients
                    .Group($"Chat_{chatId}")
                    .SendAsync(
                        "messages_read",
                        new
                        {
                            chat_id = chatId,
                            user_id = user_id,
                            message_ids = unreadMessages,
                            read_at = DateTime.UtcNow,
                        }
                    );
                _logger.LogInformation($"User {user_id} marked messages as read in chat {chatId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking messages as read in chat {chatId}");
                await Clients.Caller.SendAsync(
                    "error",
                    $"Failed to mark messages as read: {ex.Message}"
                );
            }
        }

        // User typing indicator
        [HubMethodName("user_typing")]
        public async Task UserTyping(string chatId, bool isTyping)
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id == null)
                return;
            await Clients
                .Group($"Chat_{chatId}")
                .SendAsync("user_typing", new { userId = user_id, isTyping });
        }

        // Connection management
        public override async Task OnConnectedAsync()
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{user_id}");
                _logger.LogInformation($"User {user_id} connected to ChatRoomHub");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id != null)
            {
                // Remove user from all chat rooms in Redis
                var userRooms = await _connectionTracker.GetUserRoomsAsync(
                    user_id.Value.ToString()
                );
                foreach (var room in userRooms)
                {
                    await _connectionTracker.RemoveUserFromRoomAsync(
                        user_id.Value.ToString(),
                        room
                    );
                }
                _logger.LogInformation($"User {user_id} disconnected from ChatRoomHub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        private (Guid? user_id, string? username, string? avatar_url) GetAuthUserDetails()
        {
            var user = Context.User;

            // Get user ID (sub or user_id)
            var userIdClaim = user?.FindFirst("sub") ?? user?.FindFirst("user_id");
            Guid? user_id = null;
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var parsedId))
            {
                user_id = parsedId;
            }

            // Get username
            var username =
                user?.FindFirst(ClaimTypes.Name)?.Value
                ?? user?.FindFirst("username")?.Value
                ?? "Unknown User";

            // Get avatar URL
            var avatar_url = user?.FindFirst("avatar_url")?.Value ?? string.Empty;

            if (user_id == null)
                _logger.LogWarning("User ID not found in claims");

            return (user_id, username, avatar_url);
        }
    }
}
