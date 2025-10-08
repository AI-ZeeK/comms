using Comms.Data;
using Comms.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Comms.Hubs
{
    [Authorize]
    public class ChatListHub : Hub
    {
        private readonly CommunicationsDbContext _context;
        private readonly ILogger<ChatListHub> _logger;

        public ChatListHub(CommunicationsDbContext context, ILogger<ChatListHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Connection management
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            if (userId != null)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");
                _logger.LogInformation($"User {userId} connected to ChatListHub");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            if (userId != null)
            {
                _logger.LogInformation($"User {userId} disconnected from ChatListHub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Method to push chat list updates
        public async Task NotifyNewMessage(Guid chatId, string preview, int unreadCount)
        {
            var userId = GetUserId();
            if (userId == null)
                return;

            // Broadcast only to this user's group
            await Clients
                .Group($"User_{userId}")
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
