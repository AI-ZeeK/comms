using System.ComponentModel.DataAnnotations;

namespace Comms.Models.DTOs
{
    public class CreatePushSubscriptionDto
    {
        [Required]
        public string PublicKey { get; set; } = string.Empty; // "WEB", "ANDROID", "IOS"
    }

    public class PushSubscriptionDto
    {
        public Guid UserId { get; set; }

        public UserType UserType { get; set; } = UserType.USER;

        [Required]
        public SubscriptionDto Subscription { get; set; } = new();

        [Required]
        public string Platform { get; set; } = string.Empty; // "WEB", "ANDROID", "IOS"
    }

    public class SubscriptionDto
    {
        [Required]
        public string Endpoint { get; set; } = string.Empty;

        [Required]
        public SubscriptionKeysDto Keys { get; set; } = new();
    }

    public class NotificationData
    {
        public string EntityId { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string SenderAvatar { get; set; } = string.Empty;
        public NotificationType EntityType { get; set; } // optional, if you may add more later
    }
}
