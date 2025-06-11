# Communications Microservice

A real-time chat microservice built with ASP.NET Core, Entity Framework, SignalR, and PostgreSQL.

## Features

- ✅ Real-time messaging with SignalR
- ✅ JWT Authentication
- ✅ PostgreSQL database with Entity Framework
- ✅ **RabbitMQ integration for microservice communication**
- ✅ **Event-driven architecture with message queues**
- ✅ CORS configuration for multiple frontends
- ✅ Message read receipts
- ✅ Typing indicators
- ✅ Multiple message types (text, image, audio, video, file)
- ✅ Push notification subscriptions

## Setup Instructions

### 1. Environment Variables

Copy `appsettings.example.json` to `appsettings.json` and configure your environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=djengo_communications;Username=postgres;Password=your_password;Schema=communications"
  },
  "Microservices": {
    "ProfileService": "http://localhost:5001",
    "EventsService": "http://localhost:5002",
    "GatewayService": "http://localhost:5003",
    "FilesService": "http://localhost:5004"
  },
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:3000",
      "http://localhost:3001",
      "http://localhost:3002"
    ]
  },
  "JWT": {
    "SecretKey": "your_jwt_secret_key_here_should_be_at_least_32_characters_long",
    "Issuer": "https://your-domain.com",
    "Audience": "https://your-domain.com"
  }
}
```

### 2. Database Setup

Make sure PostgreSQL is running and create the database:

```sql
CREATE DATABASE djengo_communications;
CREATE SCHEMA communications;
```

### 3. RabbitMQ Setup

Make sure RabbitMQ is running. The service will automatically create the necessary exchanges and queues:

- **Exchanges**: `user.exchange`, `chat.exchange`, `notification.exchange`, `file.exchange`
- **Queues**: `user.events`, `chat.events`, `notification.events`, `file.events`

Set these environment variables:

```bash
RABBITMQ_HOST=localhost
RABBITMQ_PORT=5672
RABBITMQ_USERNAME=guest
RABBITMQ_PASSWORD=guest
RABBITMQ_VHOST=/
```

### 4. Install Dependencies

```bash
dotnet restore
```

### 5. Run Database Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 6. Run the Service

```bash
dotnet run
```

The service will be available at `http://localhost:5000`.

## API Endpoints

### REST API

- `GET /api/chat/user/{userId}` - Get user's chats
- `GET /api/chat/{chatId}/messages` - Get chat messages (paginated)
- `POST /api/chat` - Create new chat
- `GET /health` - Health check

### SignalR Hub

Connect to `/chathub` for real-time functionality:

#### Client Methods (call from frontend):

- `JoinChat(chatId)` - Join a chat room
- `LeaveChat(chatId)` - Leave a chat room
- `SendMessage(chatId, content, messageType)` - Send a message
- `MarkMessageAsRead(messageId)` - Mark message as read
- `UserTyping(chatId, isTyping)` - Send typing indicator

#### Server Methods (listen on frontend):

- `ReceiveMessage` - Receive new messages
- `MessageRead` - Message read receipt
- `UserTyping` - Typing indicator from other users

## Frontend Integration

### JavaScript/TypeScript Example

```javascript
import * as signalR from "@microsoft/signalr";

const connection = new signalR.HubConnectionBuilder()
  .withUrl("/chathub", {
    accessTokenFactory: () => yourJwtToken,
  })
  .build();

// Listen for messages
connection.on("ReceiveMessage", (message) => {
  console.log("New message:", message);
});

// Send a message
connection.invoke("SendMessage", chatId, "Hello!", "TEXT");

// Join a chat
connection.invoke("JoinChat", chatId);
```

### React Hook Example

```typescript
import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

export const useChat = (token: string) => {
  const [connection, setConnection] = useState<signalR.HubConnection | null>(
    null
  );

  useEffect(() => {
    const newConnection = new signalR.HubConnectionBuilder()
      .withUrl("/chathub", {
        accessTokenFactory: () => token,
      })
      .build();

    setConnection(newConnection);

    newConnection.start().then(() => {
      console.log("Connected to chat hub");
    });

    return () => {
      newConnection.stop();
    };
  }, [token]);

  const sendMessage = (chatId: string, content: string) => {
    if (connection) {
      connection.invoke("SendMessage", chatId, content, "TEXT");
    }
  };

  return { connection, sendMessage };
};
```

## Microservice Communication

The service communicates with other NestJS microservices via **RabbitMQ message queues**:

### Published Events (Outgoing)

- `chat.message.sent` - When a new message is sent
- `chat.created` - When a new chat is created
- `notification.push_required` - When push notifications should be sent

### Consumed Events (Incoming)

- `user.created` - User account created
- `user.updated` - User profile updated
- `user.deleted` - User account deleted (triggers cleanup)
- `file.uploaded` - File upload completed (updates message with file URL)
- `file.deleted` - File deleted (removes file references)
- `notification.push_subscription` - Push subscription from other services

### Message Format

```json
{
  "eventType": "chat.message.sent",
  "data": {
    "messageId": "uuid",
    "chatId": "uuid",
    "senderId": "uuid",
    "content": "Hello!",
    "type": "TEXT",
    "createdAt": "2024-01-01T00:00:00Z"
  },
  "timestamp": "2024-01-01T00:00:00Z",
  "source": "communications-service"
}
```

### Traditional HTTP Communication

The service also supports HTTP clients for direct API calls:

- **Profile Service**: User information and authentication
- **Events Service**: Chat events and notifications
- **Files Service**: File uploads and media handling
- **Gateway Service**: API gateway routing

## Database Schema

The service uses the `communications` schema with these tables:

- `chats` - Chat rooms/conversations
- `chat_participants` - Users in chats
- `messages` - Chat messages
- `message_reads` - Read receipts
- `unread_message_counts` - Unread message counters
- `push_subscriptions` - Push notification subscriptions

## Docker Support

To run with Docker, create a `Dockerfile`:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["comms.csproj", "."]
RUN dotnet restore "comms.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "comms.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "comms.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "comms.dll"]
```

## Monitoring & Logging

The service includes comprehensive logging for:

- SignalR connections/disconnections
- Message sending/receiving
- Database operations
- Authentication events
- Error tracking

Logs are structured and compatible with standard .NET logging providers.
