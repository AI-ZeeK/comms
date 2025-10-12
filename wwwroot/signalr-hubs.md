# SignalR Hubs Documentation

## Real-Time Hubs

This API exposes two SignalR hubs for real-time communication:

### 1. Chat Room Hub

- **Endpoint:** `/chat-room-hub`
- **Description:** Real-time messaging and presence for chat rooms.
- **Common Methods:**
  - `SendMessage(roomId, message)`: Send a message to a chat room.
  - `JoinRoom(roomId)`: Join a chat room and receive messages.
  - `LeaveRoom(roomId)`: Leave a chat room.
  - `Typing(roomId, isTyping)`: Broadcast typing indicator.
  - `OnMessageReceived`: Client event for receiving new messages.
  - `OnUserJoined`: Client event for user join notifications.
  - `OnUserLeft`: Client event for user leave notifications.

### 2. Chat List Hub

- **Endpoint:** `/chat-list-hub`
- **Description:** Real-time updates for chat lists and user presence.
- **Common Methods:**
  - `SubscribeToList(userId)`: Subscribe to chat list updates.
  - `UnsubscribeFromList(userId)`: Unsubscribe from chat list updates.
  - `Typing(listId, isTyping)`: Broadcast typing indicator in chat list context.
  - `OnListUpdated`: Client event for chat list changes.
  - `OnTyping`: Client event for typing notifications.

## Usage

- Connect to the hub endpoint using a SignalR/WebSocket client.
- Authenticate using JWT Bearer token (see API docs for details).
- Invoke hub methods as shown above.

---

**Note:** These endpoints are not RESTful and do not appear in the Swagger/OpenAPI schema. This page documents their usage for client developers.
