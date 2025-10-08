namespace Comms.Models
{
    public enum ChatStatus
    {
        READ,
        DELIVERED,
        PENDING,
        SENT,
    }

    public enum ChatType
    {
        DIRECT,
        GROUP,
        CHANNEL,
    }

    public enum MessageType
    {
        TEXT,
        IMAGE,
        AUDIO,
        VIDEO,
        FILE,
        LOCATION,
        SYSTEM,
    }

    public enum MessageStatus
    {
        PENDING,
        SENT,
        DELIVERED,
        READ,
    }

    public enum UserType
    {
        ADMIN,
        USER,
    }

    public enum NotificationType
    {
        CHAT_CREATED,
        NEW_MESSAGE,
    }
}
