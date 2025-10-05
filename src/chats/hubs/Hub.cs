using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace Comms.Hubs
{
    public class ChatsHub : Hub
    {
        // Track which users are in which rooms
        private static readonly ConcurrentDictionary<string, HashSet<string>> RoomUsers = new();

        // Called when a client connects
        public override Task OnConnectedAsync()
        {
            Console.WriteLine($"User connected: {Context.ConnectionId}");
            return base.OnConnectedAsync();
        }

        // Called when a client disconnects
        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // Remove from all rooms
            foreach (var room in RoomUsers.Keys)
            {
                if (RoomUsers.TryGetValue(room, out var users))
                {
                    if (users.Remove(Context.ConnectionId))
                    {
                        // Notify others in the room
                        Clients.Group(room).SendAsync("UserLeft", Context.ConnectionId, room);
                    }
                }
            }

            Console.WriteLine($"User disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }

        // Join a specific chat room
        public async Task JoinRoom(string roomName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomName);

            var users = RoomUsers.GetOrAdd(roomName, _ => new HashSet<string>());
            users.Add(Context.ConnectionId);

            await Clients.Group(roomName).SendAsync("UserJoined", Context.ConnectionId, roomName);
            Console.WriteLine($"User {Context.ConnectionId} joined room {roomName}");
        }

        // Leave a specific chat room
        public async Task LeaveRoom(string roomName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);

            if (RoomUsers.TryGetValue(roomName, out var users))
            {
                users.Remove(Context.ConnectionId);
            }

            await Clients.Group(roomName).SendAsync("UserLeft", Context.ConnectionId, roomName);
            Console.WriteLine($"User {Context.ConnectionId} left room {roomName}");
        }

        // Send a message to a specific room
        public async Task SendMessageToRoom(string roomName, string message)
        {
            var sender = Context.ConnectionId;
            Console.WriteLine($"Message from {sender} to {roomName}: {message}");

            await Clients.Group(roomName).SendAsync("ReceiveMessage", new
            {
                Sender = sender,
                Message = message,
                Room = roomName,
                Timestamp = DateTime.UtcNow
            });
        }

        // Optional: Send a private message to a connection
        public async Task SendPrivateMessage(string connectionId, string message)
        {
            await Clients.Client(connectionId).SendAsync("ReceivePrivateMessage", new
            {
                Sender = Context.ConnectionId,
                Message = message,
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
