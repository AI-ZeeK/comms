// DTO for message with sender info

using Microsoft.CodeAnalysis;

namespace Comms.Models.DTOs
{
    public class CreateGroupChatDto
    {
        public string CreatorId { get; set; } = string.Empty;
        public string[] ParticipantIds { get; set; } = Array.Empty<string>();

        public string ChatName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;
        // Add other chat fields as needed
    }

    public class CreateDirectChatDto
    {
        public string CreatorId { get; set; } = string.Empty;
        public string ParticipantId { get; set; } = string.Empty;
        // Add other chat fields as needed
    }

    public class SendMessageDto
    {
        public string SenderId { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public MessageType Type { get; set; } = MessageType.TEXT;
        public string[] MediaUrls { get; set; } = Array.Empty<string>();

        public int? Duration { get; set; } // For audio messages

        // Add other message fields as needed
    }

    public class CreateChatDto
    {
        public string CreatorId { get; set; } = string.Empty;
        public string[] ParticiantsIds { get; set; } = Array.Empty<string>();
        public ChatType ChatType { get; set; } = ChatType.DIRECT;

        public string ChatName { get; set; } = string.Empty;
        public string AvatarUrl { get; set; } = string.Empty;

        // Add other chat fields as needed
    }

    public class ChatHistoryDto
    {
        public Guid ChatId { get; set; }
        public string? Name { get; set; }
        public string? AvatarUrl { get; set; }
        public ChatType ChatType { get; set; } = ChatType.DIRECT;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<MessageWithSenderDto> Messages { get; set; } = new();
        public List<ParticipantWithUserDto> Participants { get; set; } = new();
        // Add other chat fields as needed
    }

    public class ParticipantWithUserDto
    {
        public Guid ChatId { get; set; }
        public Guid UserId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public bool IsAdmin { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public DateTime? LeftAt { get; set; }
        public int UnreadCount { get; set; } = 0;
        public virtual Chat Chat { get; set; } = null!;
    }

    public class MessageWithSenderDto
    {
        public Guid MessageId { get; set; }
        public Guid ChatId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string[] MediaUrls { get; set; } = Array.Empty<string>();
        public Guid? SenderId { get; set; }
        public object? Sender { get; set; }
        public MessageType Type { get; set; } = MessageType.TEXT;
        public MessageStatus Status { get; set; } = MessageStatus.SENT;
        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DeletedAt { get; set; }

        // For audio messages
        public int? Duration { get; set; }

        // For file/image messages
        public string? FileUrl { get; set; }
        public int? FileSize { get; set; }
        public string? FileType { get; set; }

        // Add other message fields as needed
    }
}
