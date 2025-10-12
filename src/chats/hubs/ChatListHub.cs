using System.Security.Claims;
using Comms.Data;
using Comms.Models.DTOs;
using Comms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Comms.Hubs
{
    // [Authorize]
    public class ChatListHub : Hub
    {
        private readonly CommunicationsDbContext _context;
        private readonly ILogger<ChatListHub> _logger;
        private readonly IConnectionTracker _connectionTracker;
        private readonly PushService _pushService;
        private readonly IProfileGrpcService _profileService;
        private readonly IAdminGrpcService _adminGrpcService;

        private readonly ChatService _chatService;

        public ChatListHub(
            CommunicationsDbContext context,
            ILogger<ChatListHub> logger,
            PushService pushService,
            ChatService chatService,
            IConnectionTracker connectionTracker,
            IProfileGrpcService profileService,
            IAdminGrpcService adminGrpcService
        )
        {
            _context = context;
            _logger = logger;
            _pushService = pushService;
            _chatService = chatService;
            _connectionTracker = connectionTracker;
            _profileService = profileService;
            _adminGrpcService = adminGrpcService;
        }

        // Equivalent to @SubscribeMessage('leave_chats_list')
        [HubMethodName("leave_chat_list")]
        public async Task LeaveChatList()
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated during LeaveChatsList.");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }

            var chatsRoom = $"user_{user_id}_chats";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatsRoom);
            await _connectionTracker.RemoveUserFromRoomAsync(
                user_id.ToString() ?? string.Empty,
                chatsRoom
            );
            _logger.LogInformation($"User {user_id} left chat list room {chatsRoom}");
        }

        // Equivalent to @SubscribeMessage('get_user_chats')
        [HubMethodName("get_user_chats")]
        public async Task GetUserChats()
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated during GetUserChats.");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }
            try
            {
                var chats = await _chatService.GetAllChatsWithLastMessageAsync(
                    user_id.Value.ToString()
                );
                await Clients.Caller.SendAsync("user_chats", chats);
                _logger.LogInformation($"User {user_id} fetched their chats");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get chats for user {user_id}");
                await Clients.Caller.SendAsync("error", $"Failed to get chats: {ex.Message}");
            }
        }

        // Equivalent to @SubscribeMessage('create_chat')
        [HubMethodName("create_direct_chat")]
        public async Task CreateDirectChat(string participantId)
        {
            var (sender_id, sender_name, sender_avatar) = GetAuthUserDetails();
            if (sender_id == null)
            {
                _logger.LogWarning("User not authenticated during CreateChat.");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }

            try
            {
                // Call chat service to create chat and send message
                var chat = await _chatService.CreateDirectChatAsync(
                    sender_id.Value.ToString(),
                    participantId
                );

                // Check if users are in their chat list rooms
                var creatorInRoom = await _connectionTracker.IsUserInRoomAsync(
                    sender_id.Value.ToString(),
                    $"user_{sender_id}_chats"
                );
                var participantInRoom = await _connectionTracker.IsUserInRoomAsync(
                    participantId,
                    $"user_{participantId}_chats"
                );

                var usersToUpdate = new List<string>();
                if (creatorInRoom)
                    usersToUpdate.Add(sender_id.Value.ToString());
                if (participantInRoom)
                    usersToUpdate.Add(participantId);

                if (usersToUpdate.Count > 0)
                {
                    await UpdateChatListsForUsers(usersToUpdate);
                }

                if (!participantInRoom)
                {
                    var Title = $"New chat from {Context.User?.Identity?.Name ?? "Someone"}";
                    var Body = "New chat created";
                    try
                    {
                        await _pushService.SendNotificationToUserAsync(
                            participantId,
                            Title,
                            Body,
                            new NotificationData
                            {
                                EntityId = chat.ChatId.ToString(),
                                SenderId = sender_id.Value.ToString(),
                                SenderAvatar = sender_avatar ?? "",
                                SenderName = sender_name ?? "",
                                EntityType = Models.NotificationType.CHAT_CREATED,
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            $"Failed to send notification to user {participantId}"
                        );
                    }
                }

                await Clients.Caller.SendAsync(
                    "chat_created",
                    new
                    {
                        success = true,
                        message = "Chat created successfully",
                        data = chat,
                        room_status = new
                        {
                            creator_in_room = creatorInRoom,
                            participant_in_room = participantInRoom,
                        },
                    }
                );
                _logger.LogInformation($"User {sender_id} created chat with {participantId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create chat for user {sender_id}");
                await Clients.Caller.SendAsync(
                    "chat_created",
                    new { success = false, message = ex.Message }
                );
            }
        }

        [HubMethodName("create_group_chat")]
        public async Task CreateGroupChat(CreateGroupChatDto data)
        {
            var (sender_id, sender_name, sender_avatar) = GetAuthUserDetails();
            if (sender_id == null)
            {
                _logger.LogWarning("User not authenticated during CreateChat.");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }

            try
            {
                // Call chat service to create chat and send message
                var chat = await _chatService.CreateGroupChatAsync(data);

                // Check if users are in their chat list rooms (Redis)
                var creatorInRoom = await _connectionTracker.IsUserInRoomAsync(
                    sender_id.Value.ToString(),
                    $"user_{sender_id}_chats"
                );
                var participantsInRoom = new HashSet<string>();
                var participantsNotInRoom = new HashSet<string>();
                foreach (var participant_id in data.ParticipantIds)
                {
                    var inRoom = await _connectionTracker.IsUserInRoomAsync(
                        participant_id,
                        $"user_{participant_id}_chats"
                    );
                    if (inRoom)
                        participantsInRoom.Add(participant_id);
                    else
                        participantsNotInRoom.Add(participant_id);
                }

                var usersToUpdate = new List<string>();
                if (creatorInRoom)
                    usersToUpdate.Add(sender_id.Value.ToString());
                usersToUpdate.AddRange(participantsInRoom);

                if (usersToUpdate.Count > 0)
                {
                    await UpdateChatListsForUsers(usersToUpdate);
                }

                // Send notifications to all participants not in their chat list room
                if (participantsNotInRoom.Count > 0)
                {
                    var Title = $"New chat from {Context.User?.Identity?.Name ?? "Someone"}";
                    var Body = "New chat created";
                    foreach (var pid in participantsNotInRoom)
                    {
                        try
                        {
                            await _pushService.SendNotificationToUserAsync(
                                pid,
                                Title,
                                Body,
                                new NotificationData
                                {
                                    EntityId = chat.ChatId.ToString(),
                                    SenderId = sender_id.Value.ToString(),
                                    SenderAvatar = sender_avatar ?? "",
                                    SenderName = sender_name ?? "",
                                    EntityType = Models.NotificationType.CHAT_CREATED,
                                }
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send notification to user {pid}");
                        }
                    }
                }

                await Clients.Caller.SendAsync(
                    "chat_created",
                    new
                    {
                        success = true,
                        message = "Chat created successfully",
                        data = chat,
                        room_status = new
                        {
                            creator_in_room = creatorInRoom,
                            participants_in_room = participantsInRoom.ToList(),
                            participants_not_in_room = participantsNotInRoom.ToList(),
                        },
                    }
                );
                _logger.LogInformation(
                    $"User {sender_id} created group chat with participants: {string.Join(",", data.ParticipantIds)}"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to create group chat for user {sender_id}");
                await Clients.Caller.SendAsync(
                    "chat_created",
                    new { success = false, message = ex.Message }
                );
            }
        }

        // Update chat list for a single user
        public async Task UpdateChatListForUser(string userId)
        {
            try
            {
                var chats = await _chatService.GetAllChatsWithLastMessageAsync(userId);
                var chatsRoom = $"user_{userId}_chats";
                await Clients.Group(chatsRoom).SendAsync("chats_list_updated", chats);
                _logger.LogDebug($"Updated chat list for user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating chat list for user {userId}");
            }
        }

        // Update chat lists for multiple users
        public async Task UpdateChatListsForUsers(IEnumerable<string> userIds)
        {
            try
            {
                var updateTasks = userIds.Select(async userId =>
                {
                    var chats = await _chatService.GetAllChatsWithLastMessageAsync(userId);
                    var chatsRoom = $"user_{userId}_chats";
                    await Clients.Group(chatsRoom).SendAsync("chats_list_updated", chats);
                    _logger.LogDebug($"Updated chat list for user {userId}");
                });
                await Task.WhenAll(updateTasks);
                _logger.LogDebug($"Updated chat lists for {userIds.Count()} users");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating chat lists for users: {ex.Message}");
            }
        }

        // public async Task HandleClientEvent(string eventName)
        // {
        //     var userId = GetUserId();

        //     switch (eventName)
        //     {
        //         case "join_chat_list":
        //             await JoinChatList();
        //             break;
        //         case "leave_chat_list":
        //             await LeaveChatList(userId);
        //             break;
        //         default:
        //             await Clients.Caller.SendAsync("error", "Unknown event");
        //             break;
        //     }
        // }

        // Connection management
        public override async Task OnConnectedAsync()
        {
            System.Diagnostics.Debug.WriteLine("OnConnectedAsync called in ChatListHub");
            var httpContext = Context.GetHttpContext();
            var userRef = httpContext?.Request.Query["ref"].FirstOrDefault();
            _logger.LogInformation(
                "New connection to ChatListHub from {IP}, userRef={UserRef}",
                httpContext?.Connection.RemoteIpAddress,
                userRef
            );
            // Log the full httpContext as JSON (for debugging)
            try
            {
                var contextInfo = new
                {
                    RemoteIpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    LocalIpAddress = httpContext?.Connection.LocalIpAddress?.ToString(),
                    Headers = httpContext?.Request?.Headers?.ToDictionary(
                        h => h.Key,
                        h => h.Value.ToString()
                    ),
                    Path = httpContext?.Request?.Path.ToString(),
                    QueryString = httpContext?.Request?.QueryString.ToString(),
                    UserRef = userRef,
                };
                var contextJson = System.Text.Json.JsonSerializer.Serialize(contextInfo);
                _logger.LogInformation(
                    "New connection to ChatListHub context: {ContextJson}",
                    contextJson
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to serialize httpContext for logging");
            }
            var token = httpContext?.Request.Query["access_token"].FirstOrDefault();
            var role = userRef?.ToUpper() ?? "USER";
            _logger.LogInformation(
                "Token: {Token}, Role: {Role}",
                token != null ? token[..15] + "..." : "null",
                role
            );
            _logger.LogInformation(
                "Token: {Token}, Role: {Role}",
                token != null ? token[..15] + "..." : "null",
                role
            );
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(role))
            {
                _logger.LogWarning("Missing token or role during connection");
                Context.Abort();
                return;
            }

            _logger.LogInformation(
                "Incoming connection: Role={Role}, Token={Token}",
                role,
                token[..15] + "..."
            );

            if (role == "ADMIN")
            {
                var adminResp = await _adminGrpcService.ValidateAdminAccountAsync(token);
                if (adminResp == null || !adminResp.Success)
                {
                    _logger.LogWarning("❌ Invalid admin token");
                    Context.Abort();
                    return;
                }

                _logger.LogInformation("✅ Admin connected: {AdminId}", adminResp.User?.UserId);

                await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
            }
            else if (role == "USER")
            {
                var userResp = await _profileService.ValidateAccountAsync(token);
                if (userResp == null || !userResp.Success)
                {
                    _logger.LogWarning("❌ Invalid user token");
                    Context.Abort();
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, "users");
            }
            else
            {
                _logger.LogWarning("❌ Invalid role: {Role}", role);
                Context.Abort();
                return;
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id != null)
            {
                _logger.LogInformation($"User {user_id} disconnected from ChatListHub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // 🔹 Equivalent to @SubscribeMessage('join_chats_list')
        [HubMethodName("join_chat_list")]
        public async Task JoinChatList()
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id == null)
            {
                _logger.LogWarning("User not authenticated during JoinChatsList.");
                await Clients.Caller.SendAsync("error", "User not authenticated");
                return;
            }

            try
            {
                var chatsRoom = $"user_{user_id}_chats";
                await Groups.AddToGroupAsync(Context.ConnectionId, chatsRoom);
                //                 UsersInChatsRoom.Add(userId.Value);
                await _connectionTracker.AddUserToRoomAsync(user_id.ToString() ?? "", chatsRoom);

                _logger.LogInformation($"User {user_id} joined chat list room {chatsRoom}");

                // Load initial chat data
                var chats = await _chatService.GetAllChatsWithLastMessageAsync(
                    user_id.Value.ToString()
                );
                await Clients.Caller.SendAsync("chats_list_updated", chats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to join chats list for user {user_id}");
                await Clients.Caller.SendAsync("error", $"Failed to join chats list: {ex.Message}");
            }
        }

        // Method to push chat list updates
        public async Task NotifyNewMessage(Guid chatId, string preview, int unreadCount)
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id == null)
                return;

            // Broadcast only to this user's group
            await Clients
                .Group($"User_{user_id}")
                .SendAsync(
                    "ChatListUpdated",
                    new
                    {
                        chatId,
                        preview,
                        unreadCount,
                        timestamp = DateTime.UtcNow,
                    }
                );
        }

        // User typing indicator for chat list
        [HubMethodName("user_typing")]
        public async Task UserTyping(string chatId, bool isTyping)
        {
            var (user_id, _, _) = GetAuthUserDetails();
            if (user_id == null)
                return;
            // Broadcast to the user's chat list group (all their devices)
            var chatsRoom = $"user_{user_id}_chats";
            await Clients
                .Group(chatsRoom)
                .SendAsync(
                    "user_typing",
                    new
                    {
                        chatId,
                        userId = user_id,
                        isTyping,
                    }
                );
        }

        private (Guid? user_id, string? username, string? avatar_url) GetAuthUserDetails()
        {
            _logger.LogTrace("GetAuthUserDetails called112222", Context.User);
            _logger.LogTrace("GetAuthUserDetails called======", Context);
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
